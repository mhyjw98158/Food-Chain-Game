using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// fishB 그물: 잡고 던질 방향으로 휘둘러 놓으면 날아간다.
/// - 방향 = 놓기 직전 손의 이동 방향, 거리 = 놓기 직전 손의 속도
/// - 놓기 직전 손이 살짝 느려져도 약해지지 않게 마지막 짧은 구간의 가장 빠른 속도를 쓴다
/// - 휘두르는 동안 지금 놓으면 떨어질 위치를 미리 보여 준다
/// - 멈춘 채로 놓으면(너무 느리면) 던지지 않고 취소, 우클릭도 취소
/// </summary>
public class FishNetB : FishNetBBase
{
    [Header("Refs")]
    [SerializeField] private RectTransform netRect;
    [Tooltip("착지 미리보기. 비워두면 그물 모양을 반투명하게 복제해 자동 생성")]
    [SerializeField] private RectTransform aimMarker;

    [Header("Grab")]
    [Tooltip("잡지 않았을 때 그물이 마우스를 따라다님")]
    [SerializeField] private bool followMouseWhenIdle = true;
    [SerializeField] private float followSmooth = 20f;

    [Header("Flick (속도 → 거리)")]
    [Tooltip("속도를 재는 구간(초). 짧을수록 손의 마지막 움직임에 민감")]
    [SerializeField] private float velocityWindow = 0.08f;
    [Tooltip("놓기 직전 이 시간 안의 가장 빠른 속도를 쓴다(놓을 때 감속 보정)")]
    [SerializeField] private float releaseGrace = 0.1f;
    [Tooltip("이보다 느리게 놓으면 취소(Control Area 단위/초)")]
    [SerializeField] private float minThrowSpeed = 450f;
    [Tooltip("이 속도 이상이면 최대 거리")]
    [SerializeField] private float fullPowerSpeed = 2600f;
    [SerializeField] private float minThrowRange = 60f;
    [Tooltip("켜면 최대 거리를 그물 위치에서 호수 위쪽 모서리까지로 자동 계산(호수 크기를 바꿔도 맞음)")]
    [SerializeField] private bool autoMaxRange = true;
    [Tooltip("autoMaxRange가 꺼져 있을 때의 최대 비행 거리(Fishing Area 단위)")]
    [SerializeField] private float maxThrowRange = 620f;
    [Tooltip("1보다 크면 약하게 던질 때 더 세밀하게 조절됨")]
    [SerializeField] private float powerExponent = 1.0f;

    [Header("Aim Visual")]
    [SerializeField] private bool showPreview = true;
    [SerializeField] private int trajectoryDotCount = 10;
    [SerializeField] private float trajectoryDotSize = 10f;
    [SerializeField] private Color trajectoryColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color markerColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float markerCenterDotSize = 14f;
    [Tooltip("미리보기 위치가 따라가는 부드러움(클수록 즉각)")]
    [SerializeField] private float previewSmooth = 25f;

    [Header("Throw Motion")]
    [SerializeField] private float flightTime = 0.45f;
    [SerializeField] private float flightScaleBoost = 0.35f;

    [Header("Retry")]
    [SerializeField] private float retryDelay = 1.5f;

    [Header("Catch")]
    [SerializeField] private float schoolHitPadding = 10f;

    private struct Sample
    {
        public float t;
        public Vector2 pos;
    }

    private struct VelocitySample
    {
        public float t;
        public Vector2 v;
    }

    private FishB _mission;
    private RectTransform _controlArea;
    private RectTransform _fishingArea;

    private bool _grabbing;
    private bool _throwing;
    private bool _cooldown;

    private Vector2 _homePos;
    private bool _homeCached;

    private readonly List<Sample> _samples = new List<Sample>(64);
    private readonly List<VelocitySample> _velocities = new List<VelocitySample>(64);

    private bool _previewShown;
    private Vector2 _previewPos;

    private Image _markerCenterDot;
    private readonly List<RectTransform> _dots = new List<RectTransform>();

    public override void Init(FishB mission, RectTransform controlArea, RectTransform fishingArea)
    {
        _mission = mission;
        _controlArea = controlArea;
        _fishingArea = fishingArea;

        if (netRect == null)
            netRect = GetComponent<RectTransform>();

        ForceToControlArea();
        if (!_homeCached)
        {
            _homePos = ClampInside(_controlArea, netRect, netRect.anchoredPosition);
            _homeCached = true;
        }

        if (autoMaxRange)
            maxThrowRange = ComputeAutoRange();

        EnsureAimVisuals();
        Reset();
    }

    private void OnDisable()
    {
        _grabbing = false;
        SetAimVisible(false);
    }

    private void Update()
    {
        if (_mission == null || _mission.IsMissionFinished || netRect == null) return;
        if (_controlArea == null || _fishingArea == null) return;
        if (_throwing || _cooldown) return;

        TryScreenToLocal(_controlArea, Input.mousePosition, out Vector2 mouse);
        Vector2 clamped = ClampInside(_controlArea, netRect, mouse);

        if (!_grabbing)
        {
            if (Input.GetMouseButtonDown(0) && IsPointerInPlayArea(Input.mousePosition))
            {
                _grabbing = true;
                _samples.Clear();
                _velocities.Clear();
                _previewShown = false;
                netRect.anchoredPosition = clamped;
                AddSample(mouse);
                return;
            }

            if (followMouseWhenIdle && IsPointerInPlayArea(Input.mousePosition))
            {
                netRect.anchoredPosition = Vector2.Lerp(netRect.anchoredPosition, clamped,
                    Time.deltaTime * followSmooth);
            }
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelGrab();
            return;
        }

        // 그물은 마우스를 바로 따라감(영역 안으로 제한). 속도는 영역 밖으로 나가도 끊기지 않게 실제 마우스 위치로 잰다
        netRect.anchoredPosition = clamped;
        AddSample(mouse);
        UpdatePreview();

        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
        {
            _grabbing = false;
            SetAimVisible(false);

            Vector2 v = GetReleaseVelocity();
            if (v.magnitude < minThrowSpeed)
                return; // 너무 느리게 놓음 = 취소, 그물은 그 자리에서 다시 따라다님

            StartCoroutine(ThrowRoutine(ComputeLanding(v)));
        }
    }

    // ---------- Velocity ----------

    private void AddSample(Vector2 pos)
    {
        float now = Time.unscaledTime;
        _samples.Add(new Sample { t = now, pos = pos });

        float keep = Mathf.Max(velocityWindow, 0.01f) + 0.05f;
        while (_samples.Count > 2 && _samples[0].t < now - keep)
            _samples.RemoveAt(0);

        // velocityWindow 전의 위치와 비교한 평균 속도
        Sample oldest = _samples[0];
        for (int i = _samples.Count - 1; i >= 0; i--)
        {
            if (now - _samples[i].t >= velocityWindow) { oldest = _samples[i]; break; }
        }

        float dt = now - oldest.t;
        Vector2 v = dt > 0.0001f ? (pos - oldest.pos) / dt : Vector2.zero;
        _velocities.Add(new VelocitySample { t = now, v = v });

        while (_velocities.Count > 1 && _velocities[0].t < now - Mathf.Max(releaseGrace, 0.01f))
            _velocities.RemoveAt(0);
    }

    // 놓기 직전 releaseGrace 안에서 가장 빠른 속도(방향 포함)
    private Vector2 GetReleaseVelocity()
    {
        Vector2 best = Vector2.zero;
        float now = Time.unscaledTime;
        for (int i = 0; i < _velocities.Count; i++)
        {
            if (now - _velocities[i].t > releaseGrace) continue;
            if (_velocities[i].v.sqrMagnitude > best.sqrMagnitude)
                best = _velocities[i].v;
        }
        return best;
    }

    private Vector2 ComputeLanding(Vector2 controlVelocity)
    {
        Vector3 worldDir = _controlArea.TransformVector(new Vector3(controlVelocity.x, controlVelocity.y, 0f));
        Vector3 fishingDir3 = _fishingArea.InverseTransformVector(worldDir);
        Vector2 dir = new Vector2(fishingDir3.x, fishingDir3.y);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        float power = Mathf.InverseLerp(minThrowSpeed, Mathf.Max(minThrowSpeed + 1f, fullPowerSpeed), controlVelocity.magnitude);
        power = Mathf.Pow(Mathf.Clamp01(power), Mathf.Max(0.1f, powerExponent));
        float range = Mathf.Lerp(minThrowRange, maxThrowRange, power);

        Vector2 from = ConvertAnchored(_controlArea, _fishingArea, netRect.anchoredPosition);
        return ClampInside(_fishingArea, netRect, from + dir * range);
    }

    // 그물 시작 위치에서 호수 위쪽 두 모서리(그물이 들어갈 수 있는 범위) 중 먼 쪽까지의 거리
    private float ComputeAutoRange()
    {
        Vector2 from = ConvertAnchored(_controlArea, _fishingArea, _homePos);
        Rect r = _fishingArea.rect;
        Vector2 half = netRect.rect.size * 0.5f;
        Vector2 left = new Vector2(r.xMin + half.x, r.yMax - half.y);
        Vector2 right = new Vector2(r.xMax - half.x, r.yMax - half.y);
        return Mathf.Max(Vector2.Distance(from, left), Vector2.Distance(from, right));
    }

    // ---------- Preview ----------

    private void UpdatePreview()
    {
        if (!showPreview)
        {
            SetAimVisible(false);
            return;
        }

        Vector2 v = GetReleaseVelocity();
        if (v.magnitude < minThrowSpeed)
        {
            SetAimVisible(false);
            _previewShown = false;
            return;
        }

        Vector2 landing = ComputeLanding(v);
        _previewPos = _previewShown
            ? Vector2.Lerp(_previewPos, landing, Time.deltaTime * previewSmooth)
            : landing;
        _previewShown = true;

        SetAimVisible(true);
        Vector2 from = ConvertAnchored(_controlArea, _fishingArea, netRect.anchoredPosition);
        if (aimMarker != null)
        {
            aimMarker.SetAsLastSibling();
            aimMarker.anchoredPosition = _previewPos;
        }
        for (int i = 0; i < _dots.Count; i++)
        {
            float t = (i + 1f) / (_dots.Count + 1f);
            _dots[i].anchoredPosition = Vector2.Lerp(from, _previewPos, t);
        }
    }

    private void CancelGrab()
    {
        _grabbing = false;
        _previewShown = false;
        SetAimVisible(false);
    }

    // ---------- Throw ----------

    private IEnumerator ThrowRoutine(Vector2 landing)
    {
        _throwing = true;
        _cooldown = true;

        Vector2 from = ConvertAnchored(_controlArea, _fishingArea, netRect.anchoredPosition);
        netRect.SetParent(_fishingArea, worldPositionStays: false);
        netRect.SetAsLastSibling();
        netRect.anchoredPosition = from;

        float t = 0f;
        while (t < 1f && _mission != null && !_mission.IsMissionFinished)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, flightTime);
            float u = Mathf.Clamp01(t);
            float eased = 1f - (1f - u) * (1f - u);

            netRect.anchoredPosition = Vector2.Lerp(from, landing, eased);
            float s = 1f + flightScaleBoost * Mathf.Sin(u * Mathf.PI);
            netRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        netRect.anchoredPosition = landing;
        netRect.localScale = Vector3.one;

        if (_mission == null) yield break;

        if (CheckHitSchool(landing))
        {
            _throwing = false;
            _cooldown = false;
            _mission.OnNetSuccess();
            yield break;
        }

        _throwing = false;

        yield return new WaitForSeconds(retryDelay);

        if (_mission == null || _mission.IsMissionFinished) yield break;

        Reset();
    }

    public void Reset()
    {
        StopAllCoroutines();

        _grabbing = false;
        _throwing = false;
        _cooldown = false;
        _previewShown = false;
        _samples.Clear();
        _velocities.Clear();

        if (netRect == null) return;

        ForceToControlArea();
        netRect.localScale = Vector3.one;
        netRect.anchoredPosition = _homePos;

        SetAimVisible(false);
    }

    // ---------- Aim visuals ----------

    private void EnsureAimVisuals()
    {
        if (_fishingArea == null) return;

        if (aimMarker == null)
        {
            var go = new GameObject("AimMarker", typeof(RectTransform), typeof(Image));
            aimMarker = go.GetComponent<RectTransform>();
            aimMarker.SetParent(_fishingArea, false);
            aimMarker.sizeDelta = netRect.rect.size;

            Image img = go.GetComponent<Image>();
            Image netImage = netRect.GetComponent<Image>();
            if (netImage != null)
            {
                img.sprite = netImage.sprite;
                img.type = netImage.type;
                img.preserveAspect = netImage.preserveAspect;
            }
            img.color = markerColor;
            img.raycastTarget = false;
        }
        else if (aimMarker.parent != _fishingArea)
        {
            aimMarker.SetParent(_fishingArea, false);
        }

        if (_markerCenterDot == null && aimMarker != null)
        {
            var dotGo = new GameObject("Center", typeof(RectTransform), typeof(Image));
            var rt = dotGo.GetComponent<RectTransform>();
            rt.SetParent(aimMarker, false);
            rt.sizeDelta = new Vector2(markerCenterDotSize, markerCenterDotSize);
            rt.anchoredPosition = Vector2.zero;
            _markerCenterDot = dotGo.GetComponent<Image>();
            _markerCenterDot.color = trajectoryColor;
            _markerCenterDot.raycastTarget = false;
        }

        if (_dots.Count == 0)
        {
            for (int i = 0; i < trajectoryDotCount; i++)
            {
                var go = new GameObject("AimDot", typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(_fishingArea, false);
                rt.sizeDelta = new Vector2(trajectoryDotSize, trajectoryDotSize);
                Image img = go.GetComponent<Image>();
                img.color = trajectoryColor;
                img.raycastTarget = false;
                _dots.Add(rt);
            }
        }
    }

    private void SetAimVisible(bool visible)
    {
        if (aimMarker != null && aimMarker.gameObject.activeSelf != visible)
            aimMarker.gameObject.SetActive(visible);

        for (int i = 0; i < _dots.Count; i++)
        {
            if (_dots[i] != null && _dots[i].gameObject.activeSelf != visible)
                _dots[i].gameObject.SetActive(visible);
        }
    }

    // ---------- Helpers ----------

    private bool IsPointerInPlayArea(Vector2 screen)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_controlArea, screen, null)
            || RectTransformUtility.RectangleContainsScreenPoint(_fishingArea, screen, null);
    }

    private void ForceToControlArea()
    {
        if (_controlArea == null || netRect == null) return;

        RectTransform currentParent = netRect.parent as RectTransform;
        if (currentParent == _controlArea) return;

        Vector2 localInControl = netRect.anchoredPosition;
        if (currentParent != null)
            localInControl = ConvertAnchored(currentParent, _controlArea, netRect.anchoredPosition);

        netRect.SetParent(_controlArea, worldPositionStays: false);
        netRect.anchoredPosition = localInControl;
    }

    private bool CheckHitSchool(Vector2 landingInFishingArea)
    {
        if (_mission == null) return false;

        FishSchoolB school = _mission.GetComponentInChildren<FishSchoolB>();
        if (school == null) school = FindFirstObjectByType<FishSchoolB>();
        if (school == null) return false;

        Rect r = school.GetSchoolRectInFishingArea();
        r.xMin -= schoolHitPadding;
        r.xMax += schoolHitPadding;
        r.yMin -= schoolHitPadding;
        r.yMax += schoolHitPadding;

        return r.Contains(landingInFishingArea);
    }

    private static bool TryScreenToLocal(RectTransform area, Vector2 screen, out Vector2 local)
    {
        local = Vector2.zero;
        if (area == null) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out local);
    }

    private static Vector2 ClampInside(RectTransform area, RectTransform item, Vector2 p)
    {
        Rect r = area.rect;
        Vector2 half = item.rect.size * 0.5f;
        p.x = Mathf.Clamp(p.x, r.xMin + half.x, r.xMax - half.x);
        p.y = Mathf.Clamp(p.y, r.yMin + half.y, r.yMax - half.y);
        return p;
    }

    private static Vector2 ConvertAnchored(RectTransform fromArea, RectTransform toArea, Vector2 fromLocal)
    {
        Vector3 world = fromArea.TransformPoint(fromLocal);
        Vector3 toLocal3 = toArea.InverseTransformPoint(world);
        return new Vector2(toLocal3.x, toLocal3.y);
    }
}
