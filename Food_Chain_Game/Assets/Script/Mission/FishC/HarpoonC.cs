using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// fishC 작살: 누른 채 던질 방향의 반대로 당기면(새총식) 당긴 길이만큼 멀리 날아간다.
/// 조준 중에는 작살이 던질 방향을 가리키고, 조준점(십자)과 궤적 점이 보인다. 놓으면 작살 끝이 조준점에 꽂힌다.
/// 작살 끝이 큰 물고기에 닿으면 성공. 조금만 당기고 놓거나 우클릭하면 취소.
/// </summary>
public class HarpoonC : FishNetBBase
{
    [Header("Refs")]
    [Tooltip("작살 이미지(세로로 긴 모양, 위쪽이 끝)")]
    [SerializeField] private RectTransform harpoonRect;

    [Header("Aim (새총식)")]
    [Tooltip("이 길이(Control Area 단위)만큼 당기면 최대 거리")]
    [SerializeField] private float pullForMaxRange = 240f;
    [Tooltip("켜면 최대 거리를 작살 위치에서 호수 위쪽 모서리까지로 자동 계산")]
    [SerializeField] private bool autoMaxRange = true;
    [Tooltip("autoMaxRange가 꺼져 있을 때의 최대 거리(Fishing Area 단위)")]
    [SerializeField] private float maxThrowRange = 620f;
    [Tooltip("1보다 크면 약하게 던질 때 더 세밀하게 조절됨")]
    [SerializeField] private float powerExponent = 1.0f;
    [Tooltip("이보다 짧게 당기고 놓으면 취소")]
    [SerializeField] private float minPullToThrow = 25f;
    [Tooltip("당길 때 작살이 뒤로 끌려오는 최대 거리(연출)")]
    [SerializeField] private float pullVisual = 40f;

    [Header("Aim Visual")]
    [SerializeField] private int trajectoryDotCount = 14;
    [SerializeField] private float trajectoryDotSize = 8f;
    [SerializeField] private Color trajectoryColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color reticleColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private float reticleSize = 44f;
    [SerializeField] private float reticleThickness = 5f;

    [Header("Throw Motion")]
    [SerializeField] private float flightTime = 0.3f;

    [Header("Retry")]
    [Tooltip("빗나간 뒤 작살이 돌아오기까지(초)")]
    [SerializeField] private float retryDelay = 1.0f;

    [Header("Catch")]
    [Tooltip("물고기 크기에 더하는 판정 여유(작을수록 정확히 맞혀야 함)")]
    [SerializeField] private float fishHitPadding = 6f;

    private FishB _mission;
    private RectTransform _controlArea;
    private RectTransform _fishingArea;

    private bool _aiming;
    private bool _throwing;
    private bool _cooldown;

    private Vector2 _homePos;
    private bool _homeCached;
    private Vector2 _pressLocal;
    private Vector2 _target;      // 작살 끝이 꽂힐 위치(Fishing Area)
    private Vector2 _aimDir = Vector2.up;
    private bool _targetValid;

    private RectTransform _reticle;
    private readonly List<RectTransform> _dots = new List<RectTransform>();

    private float HalfLength => harpoonRect != null ? harpoonRect.rect.height * 0.5f : 0f;

    public override void Init(FishB mission, RectTransform controlArea, RectTransform fishingArea)
    {
        _mission = mission;
        _controlArea = controlArea;
        _fishingArea = fishingArea;

        if (harpoonRect == null)
            harpoonRect = GetComponent<RectTransform>();

        ForceToControlArea();
        if (!_homeCached)
        {
            _homePos = harpoonRect.anchoredPosition;
            _homeCached = true;
        }

        if (autoMaxRange)
            maxThrowRange = ComputeAutoRange();

        EnsureAimVisuals();
        Reset();
    }

    private void OnDisable()
    {
        _aiming = false;
        SetAimVisible(false);
    }

    private void Update()
    {
        if (_mission == null || _mission.IsMissionFinished || harpoonRect == null) return;
        if (_controlArea == null || _fishingArea == null) return;
        if (_throwing || _cooldown) return;

        if (!_aiming)
        {
            if (Input.GetMouseButtonDown(0) && IsPointerInPlayArea(Input.mousePosition)
                && TryScreenToLocal(_controlArea, Input.mousePosition, out Vector2 press))
            {
                _aiming = true;
                _pressLocal = press;
            }
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelAim();
            return;
        }

        if (!TryScreenToLocal(_controlArea, Input.mousePosition, out Vector2 cur))
            cur = _pressLocal;

        UpdateAim(cur - _pressLocal);

        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0))
        {
            _aiming = false;
            SetAimVisible(false);

            if (_targetValid)
                StartCoroutine(ThrowRoutine(_target, _aimDir));
            else
                ResetPose();
        }
    }

    private void UpdateAim(Vector2 pull)
    {
        float pullLen = pull.magnitude;

        Vector2 visualPull = pullLen > 0.001f ? pull / pullLen * Mathf.Min(pullLen, pullVisual) : Vector2.zero;
        harpoonRect.anchoredPosition = _homePos + visualPull;

        _targetValid = pullLen >= minPullToThrow;
        if (!_targetValid)
        {
            SetRotation(Vector2.up);
            SetAimVisible(false);
            return;
        }

        Vector3 worldDir = _controlArea.TransformVector(new Vector3(-pull.x, -pull.y, 0f));
        Vector3 fishingDir3 = _fishingArea.InverseTransformVector(worldDir);
        Vector2 dir = new Vector2(fishingDir3.x, fishingDir3.y);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        float power = Mathf.Clamp01((pullLen - minPullToThrow) / Mathf.Max(1f, pullForMaxRange - minPullToThrow));
        power = Mathf.Pow(power, Mathf.Max(0.1f, powerExponent));

        Vector2 tipFrom = HomeTipInFishing(dir);
        _target = ClampPointInside(_fishingArea, tipFrom + dir * (power * maxThrowRange));

        // 가장자리에 막혀 꺾였을 수 있으니 실제로 날아갈 방향으로 다시 계산
        Vector2 flyDir = _target - tipFrom;
        _aimDir = flyDir.sqrMagnitude > 0.01f ? flyDir.normalized : dir;
        SetRotation(_aimDir);

        SetAimVisible(true);
        if (_reticle != null)
        {
            _reticle.SetAsLastSibling();
            _reticle.anchoredPosition = _target;
        }
        for (int i = 0; i < _dots.Count; i++)
        {
            float t = (i + 1f) / (_dots.Count + 1f);
            _dots[i].anchoredPosition = Vector2.Lerp(tipFrom, _target, t);
        }
    }

    private void CancelAim()
    {
        _aiming = false;
        _targetValid = false;
        SetAimVisible(false);
        ResetPose();
    }

    private IEnumerator ThrowRoutine(Vector2 target, Vector2 dir)
    {
        _throwing = true;
        _cooldown = true;

        // 작살 가운데 위치 기준으로 날림(끝이 target에 닿도록)
        Vector2 fromCenter = ConvertAnchored(_controlArea, _fishingArea, harpoonRect.anchoredPosition);
        Vector2 toCenter = target - dir * HalfLength;

        harpoonRect.SetParent(_fishingArea, worldPositionStays: false);
        harpoonRect.SetAsLastSibling();
        harpoonRect.anchoredPosition = fromCenter;
        SetRotation(dir);

        float t = 0f;
        while (t < 1f && _mission != null && !_mission.IsMissionFinished)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, flightTime);
            float u = Mathf.Clamp01(t);
            float eased = 1f - (1f - u) * (1f - u);
            harpoonRect.anchoredPosition = Vector2.Lerp(fromCenter, toCenter, eased);
            yield return null;
        }

        harpoonRect.anchoredPosition = toCenter;

        if (_mission == null) yield break;

        if (CheckHitFish(target))
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

        _aiming = false;
        _throwing = false;
        _cooldown = false;
        _targetValid = false;

        if (harpoonRect == null) return;

        ForceToControlArea();
        ResetPose();
        SetAimVisible(false);
    }

    private void ResetPose()
    {
        harpoonRect.anchoredPosition = _homePos;
        SetRotation(Vector2.up);
    }

    private void SetRotation(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        harpoonRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 작살이 제자리에서 dir 방향을 가리킬 때 끝의 위치(Fishing Area)
    private Vector2 HomeTipInFishing(Vector2 dir)
    {
        Vector2 center = ConvertAnchored(_controlArea, _fishingArea, _homePos);
        return center + dir * HalfLength;
    }

    private float ComputeAutoRange()
    {
        Vector2 tip = HomeTipInFishing(Vector2.up);
        Rect r = _fishingArea.rect;
        float a = Vector2.Distance(tip, new Vector2(r.xMin, r.yMax));
        float b = Vector2.Distance(tip, new Vector2(r.xMax, r.yMax));
        return Mathf.Max(a, b);
    }

    // ---------- Aim visuals ----------

    private void EnsureAimVisuals()
    {
        if (_fishingArea == null) return;

        if (_reticle == null)
        {
            var go = new GameObject("AimReticle", typeof(RectTransform));
            _reticle = go.GetComponent<RectTransform>();
            _reticle.SetParent(_fishingArea, false);
            _reticle.sizeDelta = new Vector2(reticleSize, reticleSize);
            CreateBar(_reticle, new Vector2(reticleSize, reticleThickness));
            CreateBar(_reticle, new Vector2(reticleThickness, reticleSize));
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

    private void CreateBar(RectTransform parent, Vector2 size)
    {
        var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = reticleColor;
        img.raycastTarget = false;
    }

    private void SetAimVisible(bool visible)
    {
        if (_reticle != null && _reticle.gameObject.activeSelf != visible)
            _reticle.gameObject.SetActive(visible);

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
        if (_controlArea == null || harpoonRect == null) return;

        RectTransform currentParent = harpoonRect.parent as RectTransform;
        if (currentParent == _controlArea) return;

        Vector2 localInControl = harpoonRect.anchoredPosition;
        if (currentParent != null)
            localInControl = ConvertAnchored(currentParent, _controlArea, harpoonRect.anchoredPosition);

        harpoonRect.SetParent(_controlArea, worldPositionStays: false);
        harpoonRect.anchoredPosition = localInControl;
    }

    private bool CheckHitFish(Vector2 tipInFishingArea)
    {
        if (_mission == null) return false;

        FishSchoolB fish = _mission.GetComponentInChildren<FishSchoolB>();
        if (fish == null) return false;

        Rect r = fish.GetSchoolRectInFishingArea();
        r.xMin -= fishHitPadding;
        r.xMax += fishHitPadding;
        r.yMin -= fishHitPadding;
        r.yMax += fishHitPadding;

        return r.Contains(tipInFishingArea);
    }

    private static bool TryScreenToLocal(RectTransform area, Vector2 screen, out Vector2 local)
    {
        local = Vector2.zero;
        if (area == null) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out local);
    }

    private static Vector2 ClampPointInside(RectTransform area, Vector2 p)
    {
        Rect r = area.rect;
        p.x = Mathf.Clamp(p.x, r.xMin, r.xMax);
        p.y = Mathf.Clamp(p.y, r.yMin, r.yMax);
        return p;
    }

    private static Vector2 ConvertAnchored(RectTransform fromArea, RectTransform toArea, Vector2 fromLocal)
    {
        Vector3 world = fromArea.TransformPoint(fromLocal);
        Vector3 toLocal3 = toArea.InverseTransformPoint(world);
        return new Vector2(toLocal3.x, toLocal3.y);
    }
}
