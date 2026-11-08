using UnityEngine;

// 방장 게임 설정의 범위·간격·기본값. Resources/GameSettingsRules.asset에서 고친다.
// 서버는 받은 값을 여기에 맞춰 자르고(검증), 설정 창 슬라이더도 이 범위를 쓴다
[CreateAssetMenu(fileName = "GameSettingsRules", menuName = "Game/Game Settings Rules")]
public class GameSettingsRules : ScriptableObject
{
    private const string ResourcePath = "GameSettingsRules";

    [System.Serializable]
    public struct IntRange
    {
        public int min;
        public int max;
        [Tooltip("슬라이더 한 칸의 크기")]
        public int step;
        public int defaultValue;

        public IntRange(int min, int max, int step, int defaultValue)
        {
            this.min = min;
            this.max = max;
            this.step = step;
            this.defaultValue = defaultValue;
        }

        public int StepCount => Mathf.Max(0, (max - min) / Mathf.Max(1, step));

        // 범위 안으로 자르고 간격에 맞춘다
        public int Clamp(int value)
        {
            int s = Mathf.Max(1, step);
            int index = Mathf.RoundToInt((Mathf.Clamp(value, min, max) - min) / (float)s);
            return Mathf.Clamp(min + Mathf.Clamp(index, 0, StepCount) * s, min, max);
        }

        public int ToIndex(int value) => (Clamp(value) - min) / Mathf.Max(1, step);
        public int FromIndex(int index) => Clamp(min + index * Mathf.Max(1, step));
    }

    [Header("라운드(일) 수")]
    public IntRange rounds = new IntRange(3, 8, 1, 4);

    [Header("낮 시간(초)")]
    public IntRange daySeconds = new IntRange(30, 300, 10, 120);

    [Header("밤 시간(초)")]
    public IntRange nightSeconds = new IntRange(20, 180, 10, 60);

    [Header("역할 구성")]
    [Tooltip("방장이 고를 수 있는 포식자 수. 슬라이더 맨 왼쪽은 '자동'(RoleComposition의 인원별 표)")]
    public IntRange predators = new IntRange(1, 4, 1, 1);
    [Tooltip("방장이 고를 수 있는 중립 역할 수. 슬라이더 맨 왼쪽은 '자동'")]
    public IntRange neutrals = new IntRange(0, 3, 1, 0);
    [Tooltip("처음 방을 만들 때 포식자·중립 수를 자동으로 둘지")]
    public bool defaultRoleAuto = true;

    [Header("방 제목")]
    [Tooltip("방 제목 최대 글자 수")]
    public int titleMaxLength = 20;
    [Tooltip("제목을 비우거나 쓸 수 없는 제목일 때 쓰는 기본 제목. {0}: 방장 닉네임")]
    public string defaultTitleFormat = "{0}의 방";

    [Header("방 공개")]
    [Tooltip("처음 방을 만들 때 공개 방 목록에 보일지")]
    public bool defaultPublic = false;

    private static GameSettingsRules _instance;

    public static GameSettingsRules Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameSettingsRules>(ResourcePath);
                if (_instance == null)
                {
                    Debug.LogWarning($"[GameSettingsRules] Resources/{ResourcePath} 에 규칙이 없습니다. 기본 범위를 씁니다.");
                    _instance = CreateInstance<GameSettingsRules>();
                }
            }
            return _instance;
        }
    }

    public GameSettingsData Default => new GameSettingsData
    {
        roundCount = rounds.defaultValue,
        daySeconds = daySeconds.defaultValue,
        nightSeconds = nightSeconds.defaultValue,
        roomTitle = "",
        isPublic = defaultPublic,
        mapId = MapCatalog.Instance.DefaultId,
        predatorCount = defaultRoleAuto ? RoleComposition.Auto : predators.defaultValue,
        neutralCount = defaultRoleAuto ? RoleComposition.Auto : neutrals.defaultValue,
    };

    // 자동(-1)은 그대로 두고, 그 밖의 값은 범위에 맞춘다
    public static int ClampOrAuto(IntRange range, int value) => value == RoleComposition.Auto ? value : range.Clamp(value);

    // 역할 수 슬라이더: 0칸 = 자동, 1칸부터 range의 값
    public static int RoleSliderSteps(IntRange range) => range.StepCount + 1;
    public static int RoleToSlider(IntRange range, int value) => value == RoleComposition.Auto ? 0 : range.ToIndex(value) + 1;
    public static int RoleFromSlider(IntRange range, int index) => index <= 0 ? RoleComposition.Auto : range.FromIndex(index - 1);

    // 서버 검증: 모든 값을 범위·간격에 맞춘다. 제목이 비었거나 쓸 수 없으면 기본 제목(방장 닉네임)으로
    public GameSettingsData Clamp(GameSettingsData data, string hostNickname)
    {
        data.roundCount = rounds.Clamp(data.roundCount);
        data.daySeconds = daySeconds.Clamp(data.daySeconds);
        data.nightSeconds = nightSeconds.Clamp(data.nightSeconds);
        data.mapId = MapCatalog.Instance.Sanitize(data.mapId);
        data.predatorCount = ClampOrAuto(predators, data.predatorCount);
        data.neutralCount = ClampOrAuto(neutrals, data.neutralCount);
        data.roomTitle = SanitizeTitle(data.roomTitle, hostNickname);
        return data;
    }

    public string DefaultTitle(string hostNickname)
    {
        string name = string.IsNullOrWhiteSpace(hostNickname) ? "Player" : hostNickname.Trim();
        return CutTitle(string.Format(defaultTitleFormat, name));
    }

    // 줄바꿈·제어 문자 제거, 앞뒤 공백 제거, 길이 제한, 금지어(닉네임 규칙과 같은 목록)면 기본 제목, 서식 태그 차단
    public string SanitizeTitle(string title, string hostNickname)
    {
        if (string.IsNullOrEmpty(title)) return DefaultTitle(hostNickname);

        var sb = new System.Text.StringBuilder(title.Length);
        foreach (char c in title)
        {
            if (!char.IsControl(c) && c != (char)0x200B) sb.Append(c);
        }

        string cleaned = CutTitle(sb.ToString().Trim());
        if (cleaned.Length == 0 || NicknameRules.Instance.IsBanned(cleaned)) return DefaultTitle(hostNickname);
        return TextSanitizer.BlockRichText(cleaned);
    }

    private string CutTitle(string title)
    {
        int max = Mathf.Max(1, titleMaxLength);
        return title.Length > max ? title.Substring(0, max).TrimEnd() : title;
    }
}
