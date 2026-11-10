using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CharacterData;

public class GamePlayUI : MonoBehaviour
{
    public static GamePlayUI Instance;

    [Header("UI")]
    public Canvas canvas;
    public GameObject chatUI;
    public GameObject resultUI;
    [Tooltip("결과 화면 본판(승리자·패배자와 모두의 직업)")]
    [SerializeField] private ResultBoardUI resultBoard;
    public GameObject disguiseUI;
    public GameObject predictUI;
    public GameObject checkUI;
    public GameObject predictCheckUI;
    public GameObject missionListUI;
    public GameObject playerSlotUI;
    public GameObject block;
    public GameObject skyBlock;
    public Slider scanPanel;
    public Slider invProgressPanel;
    public GameObject scanResultUI;
    public GameObject investigationUI;
    public GameObject invCheckBtnGroup;

    [Header("Text")]
    public GameObject characterPanel;
    public TextMeshProUGUI characterText;
    public TextMeshProUGUI territoryText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerDisplay;
    public TextMeshProUGUI checkText;
    public TextMeshProUGUI predictCheckText;
    public TextMeshProUGUI scanKillerType;
    public TextMeshProUGUI scanCorpseType;
    public TextMeshProUGUI scanDeathTime;
    public TextMeshProUGUI investigationDescript;
    public TextMeshProUGUI investigationResult;
    public TextMeshProUGUI investigationType;

    [Header("Scan Result Labels")]
    [Tooltip("결과 창의 제목. 시체 조사 기록과 플레이어 탐색 기록이 같은 창을 쓰므로 기록 종류에 맞게 바꿔 표시한다")]
    [SerializeField] private TextMeshProUGUI scanResultTitle;
    [SerializeField] private string corpseResultTitleText = "부검 결과";
    [SerializeField] private string playerResultTitleText = "탐색 결과";
    [Tooltip("결과 창의 항목 이름. 시체 조사 기록과 플레이어 탐색 기록이 같은 창을 쓰므로 기록 종류에 맞게 바꿔 표시한다")]
    [SerializeField] private TextMeshProUGUI scanTimeLabel;
    [SerializeField] private TextMeshProUGUI scanCorpseLabel;
    [SerializeField] private TextMeshProUGUI scanKillerLabel;
    [Tooltip("게임 도중 나간 플레이어의 시체를 조사했을 때 공격자 칸에 표시할 문구(임시)")]
    [SerializeField] private string leftDeathText = "퇴장";

    [Header("Timing")]
    [Tooltip("캐릭터 소개 연출 동안 조작을 막는 시간(초). 시작 패널 애니메이션 길이(7초: 3~5초 직업 글자 이동, 5~7초 화면이 밝아짐)와 맞춘다")]
    [SerializeField] private float characterIntroSeconds = 7f;
    [SerializeField] private float characterIntroUnlockDelay = 0.1f;

    [Header("Character Intro")]
    [Tooltip("시작 연출의 능력 설명 글자(승리 조건 글자와 같은 애니메이션)")]
    [SerializeField] private TextMeshProUGUI abilityText;
    [Tooltip("시작 연출의 직업 일러스트 묶음(Animator 트리거 Illustration으로 나타났다 사라진다)")]
    [SerializeField] private GameObject roleIllustrationRoot;
    [SerializeField] private Image roleIllustrationImage;
    [Tooltip("동물별 일러스트. 없는 동물은 내 캐릭터 모습(색 + 의상)을 대신 보여 준다")]
    [SerializeField] private RoleIllustration[] roleIllustrations = new RoleIllustration[0];
    [SerializeField] private Image fallbackBodyImage;
    [SerializeField] private Image fallbackOverlayImage;
    [Tooltip("의상에 몸 그림이 없을 때 쓰는 기본 몸 그림(GamePlayer 프리팹의 몸 그림과 같게)")]
    [SerializeField] private Sprite defaultBodySprite;

    [Serializable]
    private struct RoleIllustration
    {
        public AnimalType type;
        public Sprite sprite;
    }

    [Header("Loading")]
    [Tooltip("시작 연출 전까지 검은 패널(characterPanel) 위에 띄우는 로딩 안내. 패널 애니메이터의 기본 상태는 정지(검은 화면)라, 서버가 모든 플레이어를 기다린 뒤 시작 연출을 보낼 때까지 그대로 유지된다")]
    [SerializeField] private GameObject loadingIndicator;

    [Header("Death")]
    [Tooltip("사망 연출(죽으면 열린 창을 모두 닫은 뒤 켰다가 deathEffectSeconds 뒤에 끈다). 비워 두면 창만 닫는다")]
    [SerializeField] private GameObject deathEffectUI;
    [Tooltip("사망 연출을 보여 주는 동안 조작을 막는 시간(초)")]
    [SerializeField] private float deathEffectSeconds = 2f;

    [Header("Round Text Warning")]
    [Tooltip("라운드 표시의 평소 색(남은 시간 글자)")]
    [SerializeField] private Color normalRoundTextColor = Color.white;
    [Tooltip("라운드 표시의 '밤'과 마지막 날의 '○일차', 낮/밤이 곧 바뀔 때의 남은 시간 글자에 쓰는 색")]
    [SerializeField] private Color urgentRoundTextColor = Color.red;

    [Header("Survival Warning")]
    [Tooltip("화면 위쪽 가운데의 큰 경고 글자(이대로 밤이 끝나면 굶어 죽을 때 뜬다). 미션 창에 가리지 않게 Mission Canvas에 둔다")]
    [SerializeField] private TextMeshProUGUI survivalWarningText;
    [SerializeField] private string huntWarningFormat = "{0}분 안에 사냥하지 않을시 사망합니다.";
    [SerializeField] private string foodWarningFormat = "{0}분 내로 식량을 구하지 않을시 사망합니다";
    [Tooltip("경고를 또렷하게 보여 주는 시간(초)")]
    [SerializeField] private float survivalWarningHoldSeconds = 2f;
    [Tooltip("경고가 서서히 사라지는 시간(초)")]
    [SerializeField] private float survivalWarningFadeSeconds = 1f;

    [Header("Zone Panels")]
    public Transform fieldPanel;
    public Transform forestPanel;
    public Transform riverPanel;
    public Transform skyPanel;

    [Header("Prefab")]
    public GameObject uiButtonPrefab;
    public Transform disguiseButtonGroup;
    public Transform predictButtonGroup;
    public Transform investigationButtonGroup;
    public GameObject playerIconPrefab;

    public GameObject investigationObj;

    [Header("Disguise / Predict")]
    [Tooltip("위장 창 제목. 이미 위장했다면 위장한 동물 안내로 바뀐다")]
    [SerializeField] private TextMeshProUGUI disguiseTitleText;
    [SerializeField] private string disguiseSelectTitle = "위장할 캐릭터를\n선택해주세요.";
    [SerializeField] private string disguiseDoneFormat = "이미 {0}(으)로\n위장했습니다.";
    [Tooltip("승리 예측 창 제목. 이미 예측했다면 예측한 플레이어 안내로 바뀐다")]
    [SerializeField] private TextMeshProUGUI predictTitleText;
    [SerializeField] private string predictSelectTitle = "승리할 플레이어를\n예측해주세요.";
    [SerializeField] private string predictDoneFormat = "이미 {0}님을\n승리자로 예측했습니다.";

    Animator textAni;
    Animator panelAni;
    Animator territoryAni;
    Animator descriptionAni;
    Animator abilityAni;
    Animator illustrationAni;

    private Coroutine scanCo;
    private Coroutine invCo;
    private bool isInvestigating;
    private bool isCorpseScanning;
    private Coroutine deathCo;
    private Coroutine survivalWarningCo;
    // 사망 연출 중 ESC를 막는 키(UIManager 닫기 스택)
    private static readonly object DeathEffectEscapeKey = new();

    private Dictionary<GamePlayer, GameObject> iconMap = new();
    public TMP_InputField chatInputField;
    private bool _predictOpen;
    private bool _disguiseOpen;
    // 위장/예측을 서버에 보낸 직후 SyncVar가 도착하기 전에도 다시 고르지 못하게 본인 화면에서 기억해 둔다
    private AnimalType _localDisguise = AnimalType.None;
    private uint _predictTargetNetId;
    private string _predictTargetNickname;
    private uint _localPredictNetId;
    private string _localPredictNickname;
    private GamePlayer localPlayer;
    private AnimalType selectType;
    
    private uint InvestigationTargetNetId;
    private string InvestigationTargetNickname;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        textAni = characterText.GetComponent<Animator>();
        panelAni = characterPanel.GetComponent<Animator>();
        territoryAni = territoryText.GetComponent<Animator>();
        descriptionAni = descriptionText.GetComponent<Animator>();
        if (abilityText != null) abilityAni = abilityText.GetComponent<Animator>();
        if (roleIllustrationRoot != null) illustrationAni = roleIllustrationRoot.GetComponent<Animator>();

        // 캐릭터 소개 글자는 연출이 끝나도 투명한 채 켜져 있어서, 클릭을 받으면 그 아래 창의 버튼
        // (탐색 결과 창의 확인 버튼 등)이 눌리지 않는다. 보여 주기만 하는 글자이므로 클릭을 받지 않게 한다
        characterText.raycastTarget = false;
        territoryText.raycastTarget = false;
        descriptionText.raycastTarget = false;
    }
    public void InitLocalPlayer(GamePlayer lgp)
    {
        localPlayer = lgp;
    }

    // 시작 연출 없이 로딩 화면만 걷는다(캐릭터 정보가 없을 때). 검은 패널이 남아 화면을 가리지 않게 한다
    public void SkipIntro()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (characterPanel != null) characterPanel.SetActive(false);
    }

    public IEnumerator ShowCharacter(AnimalType type)
    {
        if (!CharacterConfig.Characters.TryGetValue(type, out CharacterInfoData info))
        {
            Debug.LogWarning($"[GamePlayUI] 캐릭터 정보가 없어 소개 연출을 건너뜁니다: {type}");
            SkipIntro();
            yield break;
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        UIManager.Instance.Push(UIPriority.Modal);

        characterText.text = $"{info.DisplayName}";
        territoryText.text = $"거주지: {TranslateTerritory(info.HomeTerritory)}";
        descriptionText.text = info.WinConditionDescription;
        if (abilityText != null) abilityText.text = info.AbilityDescription;
        SetRoleIllustration(type);

        textAni.SetTrigger("ShowText");
        panelAni.SetTrigger("ShowPanel");
        territoryAni.SetTrigger("Territory");
        descriptionAni.SetTrigger("Description");
        if (abilityAni != null) abilityAni.SetTrigger("Description");
        if (illustrationAni != null) illustrationAni.SetTrigger("Illustration");
 
        yield return new WaitForSeconds(characterIntroSeconds);

        characterPanel.SetActive(false);
        if (roleIllustrationRoot != null) roleIllustrationRoot.SetActive(false);

        yield return new WaitForSeconds(characterIntroUnlockDelay);

        UIManager.Instance.Pop(UIPriority.Modal);
    }

    // 동물 일러스트가 있으면 그것을, 없으면 내 캐릭터 모습(플레이어 색 + 의상)을 보여 준다
    private void SetRoleIllustration(AnimalType type)
    {
        if (roleIllustrationRoot == null) return;
        roleIllustrationRoot.SetActive(true);

        Sprite illustration = null;
        foreach (var entry in roleIllustrations)
        {
            if (entry.type == type) { illustration = entry.sprite; break; }
        }

        if (roleIllustrationImage != null)
        {
            roleIllustrationImage.sprite = illustration;
            roleIllustrationImage.enabled = illustration != null;
        }

        bool useFallback = illustration == null;
        // 시작 연출이 InitLocalPlayer보다 먼저 올 수 있어 Mirror의 로컬 플레이어도 확인한다
        GamePlayer me = localPlayer;
        if (me == null && NetworkClient.localPlayer != null) me = NetworkClient.localPlayer.GetComponent<GamePlayer>();
        CostumeData costume = useFallback && me != null ? CostumeCatalog.Get(me.costumeIndex) : null;
        Color bodyColor = me != null ? me.gpColor : Color.white;
        bodyColor.a = 1f;

        if (fallbackBodyImage != null)
        {
            Sprite body = costume != null && costume.bodySprite != null ? costume.bodySprite : defaultBodySprite;
            fallbackBodyImage.sprite = body;
            fallbackBodyImage.color = bodyColor;
            fallbackBodyImage.enabled = useFallback && body != null;
        }

        if (fallbackOverlayImage != null)
        {
            Sprite overlay = costume != null ? costume.overlaySprite : null;
            fallbackOverlayImage.sprite = overlay;
            fallbackOverlayImage.color = costume != null && costume.tintOverlay ? bodyColor : Color.white;
            fallbackOverlayImage.enabled = useFallback && overlay != null;
        }
    }

    public Color GetTimerColor(bool isPhaseEnding)
    {
        return isPhaseEnding ? urgentRoundTextColor : normalRoundTextColor;
    }

    public void UpdateRoundText(int round, RoundTime time, bool isLastDay, bool isPhaseEnding)
    {
        if (time == RoundTime.End)
        {
            roundText.text = "게임 종료";
            timerDisplay.text = "";
            HideSurvivalWarning();
        }
        else
        {
            string dayName = $"{round}일차";
            string timeName = GetTimeName(time);
            if (isLastDay) dayName = ColorUrgent(dayName);
            if (time == RoundTime.Night) timeName = ColorUrgent(timeName);

            roundText.text = $"{dayName} {timeName}";
            timerDisplay.text = $"{GetTimeName(time)} 종료까지";
            timerDisplay.color = GetTimerColor(isPhaseEnding);
        }
            
    }
    private string ColorUrgent(string text)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(urgentRoundTextColor)}>{text}</color>";
    }

    public void ShowSurvivalWarning(bool isPredator, int minutes)
    {
        if (survivalWarningText == null) return;

        survivalWarningText.text = string.Format(isPredator ? huntWarningFormat : foodWarningFormat, minutes);

        if (survivalWarningCo != null) StopCoroutine(survivalWarningCo);
        survivalWarningCo = StartCoroutine(CoSurvivalWarning());
    }

    private void HideSurvivalWarning()
    {
        if (survivalWarningCo != null) StopCoroutine(survivalWarningCo);
        survivalWarningCo = null;

        if (survivalWarningText != null)
            survivalWarningText.gameObject.SetActive(false);
    }

    private IEnumerator CoSurvivalWarning()
    {
        Color color = survivalWarningText.color;
        color.a = 1f;
        survivalWarningText.color = color;
        survivalWarningText.gameObject.SetActive(true);

        yield return new WaitForSeconds(survivalWarningHoldSeconds);

        for (float elapsed = 0f; elapsed < survivalWarningFadeSeconds; elapsed += Time.deltaTime)
        {
            color.a = 1f - elapsed / survivalWarningFadeSeconds;
            survivalWarningText.color = color;
            yield return null;
        }

        survivalWarningText.gameObject.SetActive(false);
        survivalWarningCo = null;
    }

    // 위장 창(위장 오브젝트에서 연다). 이동 잠금(Modal)은 창을 열 때 한 번 걸고, 닫을 때(CloseDisguiseUI) 한 번만 푼다.
    // 위장은 한 번만 할 수 있어서, 이미 위장했다면 목록 대신 위장한 동물을 안내한다
    public void ShowDisguiseUI()
    {
        if (localPlayer == null || !localPlayer.CanDisguise) return;
        if (_disguiseOpen) return;

        _disguiseOpen = true;
        selectType = AnimalType.None;

        AnimalType done = localPlayer.HasDisguised ? localPlayer.disguisedAs : _localDisguise;
        bool isDone = done != AnimalType.None;
        disguiseButtonGroup.gameObject.SetActive(!isDone);
        if (disguiseTitleText != null)
            disguiseTitleText.text = isDone ? string.Format(disguiseDoneFormat, GetAnimalName(done)) : disguiseSelectTitle;

        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(disguiseUI, EscapeDisguiseUI);
        checkUI.SetActive(false);
        disguiseUI.SetActive(true);
        disguiseUI.transform.SetAsLastSibling();
    }

    // ESC: 확인창이 떠 있으면 확인창만 닫고("아니요"), 아니면 위장 창을 닫는다
    private void EscapeDisguiseUI()
    {
        if (checkUI.activeSelf) DeActiveCheckUI();
        else CloseDisguiseUI();
    }

    // 선택하지 않고 닫을 때도 이 메서드를 쓴다(닫기 버튼/위장 오브젝트에서 벗어남 등)
    public void CloseDisguiseUI()
    {
        checkUI.SetActive(false);
        disguiseUI.SetActive(false);
        selectType = AnimalType.None;

        if (!_disguiseOpen) return;
        _disguiseOpen = false;
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(disguiseUI);
    }

    public void OnDisguiseSelected(AnimalType type)
    {
        if (!_disguiseOpen || localPlayer == null) return;
        if (type == AnimalType.None || !AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string animalName)) return;
        // 이미 위장했다면 바꿀 수 없다(서버도 다시 확인)
        if (localPlayer.HasDisguised || _localDisguise != AnimalType.None)
        {
            CloseDisguiseUI();
            return;
        }

        localPlayer.CmdSetDisguise(type);
        _localDisguise = type;

        PlayerSlot slot = PlayerSlotUI.Instance != null ? PlayerSlotUI.Instance.GetSlotByPlayer(localPlayer) : null;
        if (slot != null) slot.UpdateNicknameWithAnimal($"카멜레온 > {animalName}");

        CloseDisguiseUI();
    }
    // 승리 예측 창(예측 오브젝트에서 연다): 나를 뺀 플레이어 목록 → 확인 → 확정.
    // 이동 잠금(Modal)은 창을 열 때 한 번 걸고, 닫을 때(ClosePredictUI) 한 번만 푼다.
    // 예측은 한 번만 할 수 있어서, 이미 예측했다면 목록 대신 예측한 플레이어를 안내한다
    public void ShowPredictUI()
    {
        if (localPlayer == null || !localPlayer.CanPredict) return;
        if (_predictOpen) return;

        _predictOpen = true;
        _predictTargetNetId = 0;

        string predictedNickname = GetPredictedNickname();
        bool isDone = predictedNickname != null;
        if (isDone)
        {
            foreach (Transform child in predictButtonGroup)
                Destroy(child.gameObject);
        }
        else
        {
            BuildPredictButtons();
        }
        predictButtonGroup.gameObject.SetActive(!isDone);
        if (predictTitleText != null)
            predictTitleText.text = isDone ? string.Format(predictDoneFormat, predictedNickname) : predictSelectTitle;

        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(predictUI, EscapePredictUI);
        predictCheckUI.SetActive(false);
        predictUI.SetActive(true);
        predictUI.transform.SetAsLastSibling();
    }

    // ESC: 확인창이 떠 있으면 확인창만 닫고("아니요"), 아니면 예측 창을 닫는다
    private void EscapePredictUI()
    {
        if (predictCheckUI.activeSelf) DeActivePredictCheckUI();
        else ClosePredictUI();
    }

    // 선택하지 않고 닫을 때도 이 메서드를 쓴다(닫기 버튼/예측 오브젝트에서 벗어남 등)
    public void ClosePredictUI()
    {
        predictCheckUI.SetActive(false);
        predictUI.SetActive(false);
        _predictTargetNetId = 0;

        if (!_predictOpen) return;
        _predictOpen = false;
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(predictUI);
    }

    // 서버가 조사 시작을 승인했을 때 호출됨(GamePlayer.TargetStartCorpseScan)
    public void BeginCorpseScan(Corpse corpse)
    {
        if (isCorpseScanning || corpse == null || localPlayer == null) return;

        isCorpseScanning = true;
        UIManager.Instance.Push(UIPriority.Scan);
        // 부검 진행 중에는 ESC를 막는다(창을 닫지도, 설정 창을 열지도 않음)
        UIManager.RegisterEscape(scanPanel, null);
        scanCo = StartCoroutine(CoScanCorpse(corpse, localPlayer.CorpseScanDuration, localPlayer.CorpseScanRange));
    }

    // 진행 바는 연출용. 실제 판정과 사망 정보 전송은 서버(GamePlayer.ServerCoScanCorpse)가 한다
    private IEnumerator CoScanCorpse(Corpse corpse, float duration, float range)
    {
        scanPanel.gameObject.SetActive(true);
        scanPanel.transform.SetAsLastSibling();
        scanPanel.value = 0f;
        SetScanPanelPositionOnce(localPlayer.transform);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            scanPanel.value = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

            bool outOfRange = corpse == null
                || ((Vector2)localPlayer.transform.position - (Vector2)corpse.transform.position).sqrMagnitude > range * range;

            if (outOfRange)
            {
                localPlayer.CmdCancelCorpseScan();
                CancelScanUI();
                yield break;
            }
            yield return null;
        }

        // 바가 다 찬 뒤에는 서버 결과(OnCorpseScanResult / OnCorpseScanCanceled)를 기다린다
        scanCo = null;
    }

    public void OnCorpseScanResult(uint corpseNetId, AnimalType deadType, AnimalType killerType, DeathReason deathReason, int day, bool wasNight, float elapsedSec)
    {
        if (!isCorpseScanning) return;
        CancelScanUI();

        int sec = Mathf.Max(0, Mathf.FloorToInt(elapsedSec));
        AnimalNameMap.AnimalTypeToName.TryGetValue(deadType, out string deadName);

        var rec = new InvestigationRecord
        {
            type = InvestigationType.Corpse,
            recordId = $"{corpseNetId}",
            corpseAnimalType = deadType,
            targetAnimalType = killerType,
            deathReason = deathReason,

            Day = day,
            WasNight = wasNight,
            ElapsedSec = elapsedSec,

            title = deadName ?? "???",
            subtitle = $"{day}일차 {(wasNight ? "밤" : "낮")} {sec}초 경과",
            createdAtLocal = Time.time
        };

        if (InvestigationLog.Instance != null)
            InvestigationLog.Instance.Add(rec);

        ActiveScanResult(rec);
    }

    public void OnCorpseScanCanceled()
    {
        if (!isCorpseScanning) return;
        CancelScanUI();
    }
    private void SetScanPanelPositionOnce(Transform transform)
    {
        var rt = scanPanel.GetComponent<RectTransform>();
        Vector3 worldPos = transform.position + new Vector3(0, 1.2f, 0);

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rt.position = screenPos;
            return;
        }

        Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransform parent = rt.parent as RectTransform;
        if (parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var localPoint))
        {
            rt.anchoredPosition = localPoint;
        }
    }
    private void CancelScanUI()
    {
        // Push는 BeginCorpseScan에서 한 번만 하므로 조사 중일 때만 한 번 Pop
        if (!isCorpseScanning) return;
        isCorpseScanning = false;

        if (scanCo != null)
        {
            StopCoroutine(scanCo);
            scanCo = null;
        }

        scanPanel.gameObject.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
        UIManager.UnregisterEscape(scanPanel);
        scanPanel.value = 0f;
    }
    // 시체 조사 기록: 사망 시각 / 사망자 / 공격자
    public void ActiveScanResult(InvestigationRecord record)
    {
        SetScanResultTitle(corpseResultTitleText);
        SetScanResultLabels("사망 시각 :", "사망자 :", "공격자 :");

        scanDeathTime.text = FormatRecordTime(record);
        scanCorpseType.text = GetAnimalName(record.corpseAnimalType);
        // 게임 도중 나간 플레이어의 시체는 공격자 대신 "퇴장"으로 표시(임시)
        scanKillerType.text = record.deathReason == DeathReason.Left ? leftDeathText : GetAnimalName(record.targetAnimalType);

        OpenScanResultUI();
    }
    public void DeActiveScanResult()
    {
        if (!scanResultUI.activeSelf) return;

        scanResultUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Scan);
        UIManager.UnregisterEscape(scanResultUI);
    }

    // 플레이어 탐색 기록: 탐색 시각 / 대상(닉네임) / 정체.
    // 시체 조사와 같은 창을 쓰므로 항목 이름을 바꿔 준다(그대로 두면 "사망자: 늑대"처럼 엉뚱하게 표시된다)
    public void ActivePlayerScanResult(InvestigationRecord record)
    {
        SetScanResultTitle(playerResultTitleText);
        SetScanResultLabels("탐색 시각 :", "대상 :", "정체 :");

        scanDeathTime.text = FormatRecordTime(record);
        scanCorpseType.text = record.title;
        scanKillerType.text = GetAnimalName(record.targetAnimalType);

        OpenScanResultUI();
    }

    // 결과 창이 이미 열려 있을 때 다른 기록을 누르면 내용만 바뀐다.
    // Push는 창을 열 때 한 번만 해야 닫을 때의 Pop 한 번과 짝이 맞는다(중복 Push는 상호작용 잠금이 풀리지 않는 원인)
    private void OpenScanResultUI()
    {
        scanResultUI.transform.SetAsLastSibling();
        UIManager.RegisterEscape(scanResultUI, DeActiveScanResult);
        if (scanResultUI.activeSelf) return;

        scanResultUI.SetActive(true);
        UIManager.Instance.Push(UIPriority.Scan);
    }

    private void SetScanResultTitle(string title)
    {
        if (scanResultTitle != null) scanResultTitle.text = title;
    }

    private void SetScanResultLabels(string time, string corpse, string killer)
    {
        if (scanTimeLabel != null) scanTimeLabel.text = time;
        if (scanCorpseLabel != null) scanCorpseLabel.text = corpse;
        if (scanKillerLabel != null) scanKillerLabel.text = killer;
    }

    private static string FormatRecordTime(InvestigationRecord record)
    {
        int sec = Mathf.Max(0, Mathf.FloorToInt(record.ElapsedSec));
        return $"{record.Day}일차 {(record.WasNight ? "밤" : "낮")} {sec}초 경과";
    }

    private static string GetAnimalName(AnimalType type)
    {
        return AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string animalName) ? animalName : "???";
    }

    public void Startinvestigation()
    {
        if (investigationUI.activeSelf) return;

        ActiveInvestigationSelectUI();
    }

    void ActiveInvestigationSelectUI()
    {
        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(investigationUI, EscapeInvestigationUI);

        investigationUI.SetActive(true);
        investigationUI.transform.SetAsLastSibling();

        DestroyBtn();

        investigationDescript.text = GetRemainingInvestigationText();
        investigationResult.text = string.Empty;
        investigationType.text = string.Empty;
        invCheckBtnGroup.SetActive(false);
        HideInvestigationProgress();
        InitBtnGroup();
    }

    // ESC: 탐색 진행 중이면 무시, "탐색하시겠습니까?" 확인 중이면 목록으로 돌아가고, 아니면 창을 닫는다
    private void EscapeInvestigationUI()
    {
        if (isInvestigating) return;

        if (invCheckBtnGroup.activeSelf) CancleInvestigation();
        else DeActiveInvestigationSelectUI();
    }

    // 횟수 판정은 서버(GamePlayer.CmdBeginInvestigation)가 한다. 여기서는 안내만 한다
    private string GetRemainingInvestigationText()
    {
        if (localPlayer == null) return string.Empty;

        int remaining = localPlayer.RemainingInvestigationCount;
        return remaining > 0 ? $"남은 탐색 횟수 : {remaining}" : "탐색 횟수를 모두 사용했습니다.";
    }
    // Modal은 창을 열 때 한 번 Push, 닫을 때 한 번만 Pop
    public void DeActiveInvestigationSelectUI()
    {
        if (!investigationUI.activeSelf) return;

        if (isInvestigating)
        {
            isInvestigating = false;
            if (localPlayer != null) localPlayer.CmdCancelInvestigation();
        }
        HideInvestigationProgress();

        investigationUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(investigationUI);
    }
    // 로컬 플레이어가 죽었을 때 호출됨(GamePlayer.OnAliveChanged. 진행 중이던 미션은 그쪽에서 먼저 취소한다).
    // 준비·진행 중이던 행동(탐색 확인/진행, 부검 진행)을 취소하고 열린 창을 모두 닫은 뒤 사망 연출을 보여 준다.
    // 결과나 실패 안내는 남기지 않는다(서버도 GamePlayer.Die에서 탐색·부검을 즉시 중단한다)
    public void BeginLocalDeath()
    {
        CloseAllWindows();

        if (deathCo != null) StopCoroutine(deathCo);
        deathCo = StartCoroutine(CoPlayDeathEffect());
    }

    // 준비·진행 중이던 행동(탐색 확인/진행, 부검 진행)을 취소하고 열린 창을 모두 닫는다(사망, 게임 종료 때)
    private void CloseAllWindows()
    {
        DeActiveInvestigationSelectUI();
        OnCorpseScanCanceled();

        CloseDisguiseUI();
        ClosePredictUI();
        DeActiveScanResult();
        CloseMissionListUI();
        if (playerSlotUI.activeSelf) OnClickProfileSlotUI();

        // 여기서 직접 닫지 않은 창(탐색 노트, 우클릭 메뉴, 미니맵 전체화면 등)
        if (UIManager.Instance != null)
            UIManager.Instance.CloseAllEscapable();
    }

    // 결과 화면을 띄울 때 재생 중이던 사망 연출을 끝낸다(마지막 공격으로 죽으며 게임이 끝난 경우)
    private void StopDeathEffect()
    {
        if (deathCo != null)
        {
            StopCoroutine(deathCo);
            deathCo = null;
        }

        // 연출 오브젝트가 켜져 있을 때만 Push가 된 상태다(연결 안 된 경우 코루틴은 Push 없이 바로 끝난다)
        if (deathEffectUI == null || !deathEffectUI.activeSelf) return;

        deathEffectUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(DeathEffectEscapeKey);
    }

    private IEnumerator CoPlayDeathEffect()
    {
        if (deathEffectUI == null)
        {
            deathCo = null;
            yield break;
        }

        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(DeathEffectEscapeKey, null);
        deathEffectUI.SetActive(true);
        deathEffectUI.transform.SetAsLastSibling();

        yield return new WaitForSeconds(deathEffectSeconds);

        deathEffectUI.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(DeathEffectEscapeKey);
        deathCo = null;
    }

    private void OnSelectInvestigationTarget(uint targetNetId, string targetNickname)
    {
        InvestigationTargetNetId = targetNetId;
        InvestigationTargetNickname = targetNickname;

        DestroyBtn();
        investigationResult.text = $"{targetNickname}를(을) 탐색하시겠습니까?";
        invCheckBtnGroup.SetActive(true);
    }

    public void ProgressInvestigation()
    {
        if (isInvestigating || localPlayer == null) return;

        invCheckBtnGroup.SetActive(false);
        isInvestigating = true;
        localPlayer.CmdBeginInvestigation(InvestigationTargetNetId);

        if (invCo != null) StopCoroutine(invCo);
        invCo = StartCoroutine(CoInvestigationProgress(localPlayer.InvestigationDuration));
    }
    public void CancleInvestigation()
    {
        InitBtnGroup();
        investigationDescript.text = GetRemainingInvestigationText();
        investigationResult.text = string.Empty;
        invCheckBtnGroup.SetActive(false);
    }
    void InitBtnGroup()
    {
        if (GameMamager.Instance == null) return;
        // 횟수를 다 쓰면 대상 목록을 띄우지 않는다
        if (localPlayer == null || localPlayer.RemainingInvestigationCount <= 0) return;

        foreach (var p in GameMamager.Instance.players)
        {
            // 나간 플레이어는 목록에 null로 남을 수 있다
            if (p == null || p == localPlayer) continue;
            AddInvestigationTargetButton(p.netId, p.nickname);
        }

        // 게임 도중 나간 플레이어도 계속 탐색할 수 있다(서버가 나간 순간의 정보로 결과를 준다)
        foreach (var left in GameMamager.Instance.leftPlayers)
            AddInvestigationTargetButton(left.netId, left.nickname);
    }
    void AddInvestigationTargetButton(uint targetNetId, string targetNickname)
    {
        var btnObj = Instantiate(uiButtonPrefab, investigationButtonGroup);
        var label = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = targetNickname;

        var button = btnObj.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => OnSelectInvestigationTarget(targetNetId, targetNickname));
    }
    void DestroyBtn()
    {
        foreach (Transform child in investigationButtonGroup)
            Destroy(child.gameObject);
    }
    // 진행 바는 연출용. 실제 시간/조건 판정과 결과 전송은 서버(GamePlayer.ServerCoInvestigate)가 한다
    IEnumerator CoInvestigationProgress(float duration)
    {
        DestroyBtn();

        investigationDescript.text = "탐색 진행중";
        investigationResult.text = string.Empty;
        invProgressPanel.gameObject.SetActive(true);
        invProgressPanel.transform.SetAsLastSibling();
        invProgressPanel.value = 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            invProgressPanel.value = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;

            yield return null;
        }

        invCo = null;
    }
    private void HideInvestigationProgress()
    {
        if (invCo != null)
        {
            StopCoroutine(invCo);
            invCo = null;
        }

        invProgressPanel.gameObject.SetActive(false);
        invProgressPanel.value = 0f;
    }

    public void OnInvestigationCanceled()
    {
        if (!isInvestigating) return;
        isInvestigating = false;

        HideInvestigationProgress();

        investigationDescript.text = "탐색 실패";
        investigationResult.text = "탐색 장소를 벗어났거나 탐색할 수 없는 상태입니다.";
        investigationType.text = string.Empty;

        DestroyBtn();
        InitBtnGroup();
    }

    public void ActiveInvestigationResult(uint targetNetId, string targetNickname, AnimalType targetType, bool isPredator)
    {
        if (!isInvestigating) return;
        isInvestigating = false;

        HideInvestigationProgress();

        var gameMamager = GameMamager.Instance;

        // recordId는 비워 두어 InvestigationLog가 새 ID를 만든다(같은 대상을 다시 탐색해도 기록이 남도록)
        var rec = new InvestigationRecord
        {
            type = InvestigationType.PlayerIdentity,
            title = targetNickname,
            subtitle = $"탐색 결과: {GetAnimalName(targetType)}",
            Day = gameMamager.currentRound,
            WasNight = gameMamager.IsNightPhase,
            ElapsedSec = gameMamager.PhaseElapsedSeconds,
            targetAnimalType = targetType,
            createdAtLocal = Time.time,
        };

        if (InvestigationLog.Instance != null)
            InvestigationLog.Instance.Add(rec);

        investigationDescript.text = "탐색 완료";
        investigationResult.text = "탐색 결과";
        investigationType.text = GetAnimalName(targetType);
        investigationType.color = isPredator ? Color.red : Color.green;
    }
    public void DeActiveInvestigationResult()
    {
        DeActiveInvestigationSelectUI();
    }
    private string TranslateTerritory(TerritoryType type)
    {
        return type switch
        {
            TerritoryType.Sky => "하늘",
            TerritoryType.River => "강",
            TerritoryType.Field => "들",
            TerritoryType.Forest => "숲",
            _ => "???"
        };
    }
    private string GetTimeName(RoundTime time)
    {
        return time == RoundTime.Day ? "낮" : "밤";
    }
    public void ActiveSkyBlock()
    {
        skyBlock.SetActive(true);       
    }

    public void DeActiveChatUI()
    {
        chatUI.SetActive(false);
    }
    public void DeActivePlayerSlotUI()
    {
        playerSlotUI.SetActive(false);
    }
    public void ActiveBlock()
    {
        block.SetActive(true);
    }
    public void DeActiveBlock()
    {
        block.SetActive(false);
    }
  
    // 진행 중인 미션은 GamePlayer.TargetShowGameResult에서 먼저 취소한다. 여기서는 나머지 창을 닫고 결과 화면만 남긴다
    public void ShowResult(GameResultEntry[] entries)
    {
        StopDeathEffect();
        CloseAllWindows();

        if (resultBoard != null)
            resultBoard.Show(entries, localPlayer != null ? localPlayer.netId : 0);

        chatUI.SetActive(false);
        // 다른 창보다 위에 그린다
        resultUI.transform.SetAsLastSibling();
        resultUI.SetActive(true);
    }

    public void SetDisguiseOptions()
    {
        // 기존 버튼 제거
        foreach (Transform child in disguiseButtonGroup)
            Destroy(child.gameObject);

        // 위장 후보는 카멜레온을 뺀 모든 동물(서버도 GamePlayer.CmdSetDisguise에서 같은 조건으로 거른다)
        foreach (AnimalType type in Enum.GetValues(typeof(AnimalType)))
        {
            if (type == AnimalType.None || type == AnimalType.Chameleon) continue;
            if (!AnimalNameMap.AnimalTypeToName.TryGetValue(type, out string label)) continue;

            AnimalType candidate = type;
            GameObject btn = Instantiate(uiButtonPrefab, disguiseButtonGroup);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectType = candidate;
                checkText.text = $"정말로 {label}(으)로 \n위장하시겠습니까??\n<size=24>(다시 바꿀 수 없습니다)</size>";
                checkUI.SetActive(true);
                checkUI.transform.SetAsLastSibling();
            });
        }
        disguiseUI.SetActive(false);
    }

    // 예측 후보는 플레이어 목록이라 창을 열 때마다 새로 만든다(BuildPredictButtons). 게임 시작 시에는 비워 두기만 한다
    public void SetPredictOptions()
    {
        foreach (Transform child in predictButtonGroup)
            Destroy(child.gameObject);

        predictUI.SetActive(false);
    }

    private void BuildPredictButtons()
    {
        foreach (Transform child in predictButtonGroup)
            Destroy(child.gameObject);

        if (GameMamager.Instance == null) return;

        foreach (var p in GameMamager.Instance.players)
        {
            // 나간 플레이어는 목록에 null로 남을 수 있다
            if (p == null || p == localPlayer) continue;

            GamePlayer candidate = p;
            GameObject btn = Instantiate(uiButtonPrefab, predictButtonGroup);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = candidate.nickname;
            btn.GetComponent<Button>().onClick.AddListener(() => OnSelectPredictTarget(candidate));
        }
    }

    private void OnSelectPredictTarget(GamePlayer target)
    {
        if (!_predictOpen || target == null) return;

        _predictTargetNetId = target.netId;
        _predictTargetNickname = target.nickname;

        predictCheckText.text = $"{target.nickname}님을 승리자로\n예측하시겠습니까?\n<size=24>(다시 바꿀 수 없습니다)</size>";
        predictCheckUI.SetActive(true);
        predictCheckUI.transform.SetAsLastSibling();
    }

    // 이미 예측했다면 예측한 플레이어 닉네임, 아니면 null
    private string GetPredictedNickname()
    {
        if (_localPredictNetId != 0) return _localPredictNickname;
        if (localPlayer == null || !localPlayer.HasPredicted) return null;

        uint id = localPlayer.predictedPlayerNetId;
        if (NetworkClient.spawned.TryGetValue(id, out var identity) && identity != null
            && identity.TryGetComponent(out GamePlayer predicted))
            return predicted.nickname;

        return "???";
    }
    public void OnClickChatBtn()
    {
        chatUI.SetActive(!chatUI.activeSelf);
    }
    // 위장 확인창 "아니요": 확인창만 닫고 선택 창으로 돌아간다(잠금은 그대로)
    public void DeActiveCheckUI()
    {
        checkUI.SetActive(false);
        selectType = AnimalType.None;
    }
    // 위장 확인창 "네": 선택을 확정하고 창을 닫는다
    public void DeActiveDisguiseUI()
    {
        checkUI.SetActive(false);
        OnDisguiseSelected(selectType);
    }

    // 예측 확인창 "아니요": 확인창만 닫고 선택 창으로 돌아간다(잠금은 그대로)
    public void DeActivePredictCheckUI()
    {
        predictCheckUI.SetActive(false);
        _predictTargetNetId = 0;
    }
    // 예측 확인창 "네": 선택을 확정하고 창을 닫는다. 한 번 정하면 바꿀 수 없다(서버도 다시 확인)
    public void DeActivePredictUI()
    {
        predictCheckUI.SetActive(false);

        if (!_predictOpen || localPlayer == null || _predictTargetNetId == 0) return;
        if (GetPredictedNickname() != null)
        {
            ClosePredictUI();
            return;
        }

        localPlayer.CmdSetPredict(_predictTargetNetId);
        _localPredictNetId = _predictTargetNetId;
        _localPredictNickname = _predictTargetNickname;

        ClosePredictUI();
    }

    public void OnClickMissionListUI()
    {
        if (missionListUI.activeSelf)
        {
            CloseMissionListUI();
            return;
        }

        missionListUI.SetActive(true);
        UIManager.RegisterEscape(missionListUI, CloseMissionListUI);
    }

    public void CloseMissionListUI()
    {
        missionListUI.SetActive(false);
        UIManager.UnregisterEscape(missionListUI);
    }
    public void OnClickProfileSlotUI()
    {
        if (playerSlotUI.activeSelf)
        {
            playerSlotUI.SetActive(false);
            UIManager.Instance.Pop(UIPriority.Modal);
        }
        else
        {
            playerSlotUI.SetActive(true);
            UIManager.Instance.Push(UIPriority.Modal);
        }
    }
    public void AddPlayer(GamePlayer player, ZoneType zone)
    {
        if (iconMap.ContainsKey(player)) return;

        var icon = Instantiate(playerIconPrefab, GetZonePanel(zone));
        icon.GetComponentInChildren<TMP_Text>().text = player.nickname;
        iconMap[player] = icon;
    }

    public void MovePlayerIcon(GamePlayer player, ZoneType newZone)
    {
        if (!iconMap.ContainsKey(player)) return;

        iconMap[player].transform.SetParent(GetZonePanel(newZone), false);
    }

    public void RemovePlayer(GamePlayer player)
    {
        if (iconMap.TryGetValue(player, out var icon))
        {
            Destroy(icon);
            iconMap.Remove(player);
        }
    }
    Transform GetZonePanel(ZoneType zone) => zone switch
    {
        ZoneType.Field => fieldPanel,
        ZoneType.Forest => forestPanel,
        ZoneType.River => riverPanel,
        ZoneType.Sky => skyPanel,
        _ => null
    };
}
