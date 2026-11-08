using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로비의 방장 게임 설정.
// - 방장: 우측 상단 버튼으로 설정 창을 열어 바꾼다(방 제목, 공개 여부, 맵, 라운드 수, 낮·밤 시간, 포식자·중립 수).
//   바꿀 때마다 서버로 보내고, 서버가 검증한 값이 모두에게 돌아온다
// - 모두: 현재 설정 요약을 항상 본다(서버가 보낸 값)
public class GameSettingsUI : MonoBehaviour
{
    [Header("방장 전용")]
    [SerializeField] private GameObject openButton;
    [SerializeField] private GameObject window;

    [Header("방 정보")]
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private Toggle publicToggle;
    [SerializeField] private TMP_Dropdown mapDropdown;

    [Header("슬라이더")]
    [SerializeField] private Slider roundsSlider;
    [SerializeField] private TextMeshProUGUI roundsValueText;
    [SerializeField] private Slider daySlider;
    [SerializeField] private TextMeshProUGUI dayValueText;
    [SerializeField] private Slider nightSlider;
    [SerializeField] private TextMeshProUGUI nightValueText;
    [Tooltip("맨 왼쪽 = 자동(인원별 구성표)")]
    [SerializeField] private Slider predatorsSlider;
    [SerializeField] private TextMeshProUGUI predatorsValueText;
    [Tooltip("맨 왼쪽 = 자동(인원별 구성표)")]
    [SerializeField] private Slider neutralsSlider;
    [SerializeField] private TextMeshProUGUI neutralsValueText;

    [Header("모두에게 보이는 요약")]
    [SerializeField] private TextMeshProUGUI summaryText;

    [Header("문구")]
    [Tooltip("{0}: 라운드 수")]
    [SerializeField] private string roundsFormat = "{0}일";
    [Tooltip("{0}: 시간 문구")]
    [SerializeField] private string secondsFormat = "{0}";
    [Tooltip("{0}: 방 제목, {1}: 공개 여부 문구, {2}: 맵 이름, {3}: 라운드 수, {4}: 낮 시간, {5}: 밤 시간, {6}: 포식자 수 문구, {7}: 중립 수 문구")]
    [SerializeField] private string summaryFormat = "{0}\n{1} / 맵: {2}\n{3}일 / 낮 {4} / 밤 {5}\n포식자 {6} / 중립 {7}";
    [Tooltip("역할 수를 직접 정했을 때. {0}: 수")]
    [SerializeField] private string roleCountFormat = "{0}명";
    [Tooltip("역할 수가 자동일 때. {0}: 지금 인원으로 정해지는 수")]
    [SerializeField] private string roleAutoFormat = "자동({0}명)";
    [SerializeField] private string publicText = "공개 방";
    [SerializeField] private string privateText = "비공개 방";
    [Tooltip("맵 선택 목록에 인원 범위를 붙이는 문구. {0}: 맵 이름, {1}: 인원 범위(MapCatalog의 문구)")]
    [SerializeField] private string mapOptionWithRangeFormat = "{0} ({1})";
    [SerializeField] private string summaryWaiting = "게임 설정 불러오는 중";

    private GameSettingsRules _rules;
    private GameSettingsData _shown;
    private bool _isHost;
    private GameSettingsData _lastSent;
    private bool _hasSent;
    // 맵 선택 목록의 순서대로 맵 id(무작위 포함)
    private readonly List<string> _mapIds = new List<string>();
    private int _shownPlayerCount = -1;

    private void Awake()
    {
        _rules = GameSettingsRules.Instance;

        SetupSlider(roundsSlider, _rules.rounds);
        SetupSlider(daySlider, _rules.daySeconds);
        SetupSlider(nightSlider, _rules.nightSeconds);
        SetupRoleSlider(predatorsSlider, _rules.predators);
        SetupRoleSlider(neutralsSlider, _rules.neutrals);
        SetupMapDropdown();

        if (roundsSlider != null) roundsSlider.onValueChanged.AddListener(_ => OnControlChanged());
        if (daySlider != null) daySlider.onValueChanged.AddListener(_ => OnControlChanged());
        if (nightSlider != null) nightSlider.onValueChanged.AddListener(_ => OnControlChanged());
        if (predatorsSlider != null) predatorsSlider.onValueChanged.AddListener(_ => OnControlChanged());
        if (neutralsSlider != null) neutralsSlider.onValueChanged.AddListener(_ => OnControlChanged());
        if (publicToggle != null) publicToggle.onValueChanged.AddListener(_ => OnControlChanged());
        if (mapDropdown != null) mapDropdown.onValueChanged.AddListener(_ => OnControlChanged());
        if (titleInput != null)
        {
            titleInput.characterLimit = Mathf.Max(1, _rules.titleMaxLength);
            // 제목은 입력을 마쳤을 때(Enter, 다른 곳 클릭) 보낸다
            titleInput.onEndEdit.AddListener(_ => OnControlChanged());
        }

        if (window != null) window.SetActive(false);
        if (openButton != null) openButton.SetActive(false);
    }

    private void OnEnable()
    {
        GameSettings.ClientChanged += OnSettingsChanged;
    }

    private void OnDisable()
    {
        GameSettings.ClientChanged -= OnSettingsChanged;
        if (window != null && window.activeSelf) Close();
    }

    private void Start()
    {
        if (GameSettings.ClientHasValue) OnSettingsChanged(GameSettings.ClientCurrent);
        else if (summaryText != null) summaryText.text = summaryWaiting;
    }

    private void Update()
    {
        // 자동 역할 수는 인원에 따라 달라지므로 인원이 바뀌면 다시 그린다
        int playerCount = CurrentPlayerCount();
        if (playerCount != _shownPlayerCount)
        {
            _shownPlayerCount = playerCount;
            if (GameSettings.ClientHasValue) OnSettingsChanged(_shown);
        }

        // 이 구조에서 방장은 항상 서버를 겸하는 호스트다(GameRoomUI와 같은 기준). 접속이 끝난 뒤에 켠다
        bool isHost = NetworkServer.active && NetworkClient.localPlayer != null;
        if (isHost == _isHost) return;

        _isHost = isHost;
        if (openButton != null) openButton.SetActive(isHost);
        if (!isHost && window != null && window.activeSelf) Close();
    }

    public void Open()
    {
        if (!_isHost || window == null || window.activeSelf) return;

        window.SetActive(true);
        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(window, Close);
        ShowOnControls(_shown);
    }

    public void Close()
    {
        if (window == null || !window.activeSelf) return;

        // 제목을 입력하던 중이면 그 값까지 보낸 뒤 닫는다
        if (titleInput != null && titleInput.isFocused) OnControlChanged();

        window.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(window);
    }

    // 서버가 검증해서 보낸 값. 요약과 (열려 있으면) 설정 창에 반영한다
    private void OnSettingsChanged(GameSettingsData data)
    {
        _shown = data;

        if (summaryText != null)
        {
            summaryText.text = string.Format(summaryFormat,
                data.roomTitle,
                data.isPublic ? publicText : privateText,
                MapCatalog.Instance.DisplayName(data.mapId),
                data.roundCount, TimeText(data.daySeconds), TimeText(data.nightSeconds),
                PredatorsText(data), NeutralsText(data));
        }

        if (window != null && window.activeSelf) ShowOnControls(data);
    }

    private void ShowOnControls(GameSettingsData data)
    {
        SetSlider(roundsSlider, _rules.rounds.ToIndex(data.roundCount));
        SetSlider(daySlider, _rules.daySeconds.ToIndex(data.daySeconds));
        SetSlider(nightSlider, _rules.nightSeconds.ToIndex(data.nightSeconds));
        SetSlider(predatorsSlider, GameSettingsRules.RoleToSlider(_rules.predators, data.predatorCount));
        SetSlider(neutralsSlider, GameSettingsRules.RoleToSlider(_rules.neutrals, data.neutralCount));

        if (publicToggle != null) publicToggle.SetIsOnWithoutNotify(data.isPublic);
        if (mapDropdown != null)
        {
            int index = _mapIds.IndexOf(data.mapId);
            if (index >= 0) mapDropdown.SetValueWithoutNotify(index);
        }
        // 입력 중인 제목은 덮어쓰지 않는다
        if (titleInput != null && !titleInput.isFocused) titleInput.SetTextWithoutNotify(data.roomTitle);

        ShowValues(data);
    }

    private void OnControlChanged()
    {
        var data = ReadControls();
        ShowValues(data);

        if (!_isHost) return;
        if (_hasSent && data.Equals(_lastSent)) return;

        var localPlayer = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<RoomPlayer>() : null;
        if (localPlayer == null) return;

        _lastSent = data;
        _hasSent = true;
        localPlayer.CmdChangeGameSettings(data);
    }

    private GameSettingsData ReadControls()
    {
        var data = _shown;
        if (roundsSlider != null) data.roundCount = _rules.rounds.FromIndex(Mathf.RoundToInt(roundsSlider.value));
        if (daySlider != null) data.daySeconds = _rules.daySeconds.FromIndex(Mathf.RoundToInt(daySlider.value));
        if (nightSlider != null) data.nightSeconds = _rules.nightSeconds.FromIndex(Mathf.RoundToInt(nightSlider.value));
        if (predatorsSlider != null) data.predatorCount = GameSettingsRules.RoleFromSlider(_rules.predators, Mathf.RoundToInt(predatorsSlider.value));
        if (neutralsSlider != null) data.neutralCount = GameSettingsRules.RoleFromSlider(_rules.neutrals, Mathf.RoundToInt(neutralsSlider.value));
        if (publicToggle != null) data.isPublic = publicToggle.isOn;
        if (mapDropdown != null && mapDropdown.value >= 0 && mapDropdown.value < _mapIds.Count) data.mapId = _mapIds[mapDropdown.value];
        if (titleInput != null) data.roomTitle = titleInput.text;
        return data;
    }

    private void ShowValues(GameSettingsData data)
    {
        if (roundsValueText != null) roundsValueText.text = string.Format(roundsFormat, data.roundCount);
        if (dayValueText != null) dayValueText.text = string.Format(secondsFormat, TimeText(data.daySeconds));
        if (nightValueText != null) nightValueText.text = string.Format(secondsFormat, TimeText(data.nightSeconds));
        if (predatorsValueText != null) predatorsValueText.text = PredatorsText(data);
        if (neutralsValueText != null) neutralsValueText.text = NeutralsText(data);
    }

    // 지금 로비 인원(자동 역할 수 표시용). 게임 시작 때 서버가 같은 규칙으로 다시 계산한다
    private static int CurrentPlayerCount()
    {
        return NetworkManager.singleton is NetworkRoomManager room ? Mathf.Max(1, room.roomSlots.Count) : 1;
    }

    private string PredatorsText(GameSettingsData data)
    {
        RoleComposition.Instance.ResolveCounts(CurrentPlayerCount(), data.predatorCount, data.neutralCount, out int predators, out _);
        return RoleText(data.predatorCount, predators);
    }

    private string NeutralsText(GameSettingsData data)
    {
        RoleComposition.Instance.ResolveCounts(CurrentPlayerCount(), data.predatorCount, data.neutralCount, out _, out int neutrals);
        return RoleText(data.neutralCount, neutrals);
    }

    // 직접 정한 값도 지금 인원에서 실제로 쓰일 수로 보여 준다(예: 3명인데 포식자 4 → 2명)
    private string RoleText(int setting, int resolved)
    {
        return setting == RoleComposition.Auto
            ? string.Format(roleAutoFormat, resolved)
            : string.Format(roleCountFormat, resolved);
    }

    // 맵 목록: 무작위(맵이 2개 이상일 때) + 고를 수 있는 맵. 인원 제한이 있는 맵은 범위를 붙인다
    private void SetupMapDropdown()
    {
        _mapIds.Clear();
        if (mapDropdown == null) return;

        var catalog = MapCatalog.Instance;
        var options = new List<TMP_Dropdown.OptionData>();

        if (catalog.AllowRandom)
        {
            _mapIds.Add(MapCatalog.RandomId);
            options.Add(new TMP_Dropdown.OptionData(catalog.RandomDisplayName));
        }

        foreach (var map in catalog.SelectableMaps())
        {
            _mapIds.Add(map.Id);
            string range = catalog.PlayerRangeText(map);
            string label = range.Length > 0 ? string.Format(mapOptionWithRangeFormat, map.DisplayName, range) : map.DisplayName;
            options.Add(new TMP_Dropdown.OptionData(label, map.Thumbnail, Color.white));
        }

        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(options);
        // 고를 맵이 하나뿐이면 바꿀 것이 없으므로 잠근다
        mapDropdown.interactable = _mapIds.Count > 1;
    }

    // 90 → "1분 30초", 120 → "2분", 45 → "45초"
    private static string TimeText(int seconds)
    {
        int minutes = seconds / 60;
        int rest = seconds % 60;
        if (minutes == 0) return $"{rest}초";
        return rest == 0 ? $"{minutes}분" : $"{minutes}분 {rest}초";
    }

    private static void SetupSlider(Slider slider, GameSettingsRules.IntRange range)
    {
        if (slider == null) return;
        slider.wholeNumbers = true;
        slider.minValue = 0;
        slider.maxValue = range.StepCount;
    }

    private static void SetupRoleSlider(Slider slider, GameSettingsRules.IntRange range)
    {
        if (slider == null) return;
        slider.wholeNumbers = true;
        slider.minValue = 0;
        slider.maxValue = GameSettingsRules.RoleSliderSteps(range);
    }

    private static void SetSlider(Slider slider, int index)
    {
        if (slider != null) slider.SetValueWithoutNotify(index);
    }
}
