using UnityEngine;

// 맵 하나 = 게임 씬 하나. 맵을 늘릴 때는 게임 씬을 복제해 배치를 바꾸고, 빌드 설정에 씬을 넣은 뒤
// 이 에셋을 만들어 MapCatalog(Resources/MapCatalog.asset)의 목록 끝에 추가한다
[CreateAssetMenu(fileName = "Map_", menuName = "Game/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [Tooltip("저장·통신에 쓰는 고유 이름(영문 소문자·숫자·_·-, 32자 이하). 한 번 정하면 바꾸지 않는다(방장 PC 저장값, 방 목록이 이 값을 쓴다)")]
    [SerializeField] private string id = "default";
    [Tooltip("화면에 보이는 이름. 글꼴(KCC-Hanbit SDF)에 있는 글자만 쓴다")]
    [SerializeField] private string displayName = "기본 맵";
    [Tooltip("맵 설명(방 목록·설정 창 등에서 쓸 수 있음)")]
    [TextArea] [SerializeField] private string description;
    [Tooltip("맵 미리보기 그림(없어도 됨)")]
    [SerializeField] private Sprite thumbnail;

    [Header("인원")]
    [Tooltip("이 맵으로 시작할 수 있는 최소 인원")]
    [SerializeField] private int minPlayers = 1;
    [Tooltip("이 맵으로 시작할 수 있는 최대 인원. 0이면 제한 없음")]
    [SerializeField] private int maxPlayers = 0;

    [Header("선택")]
    [Tooltip("끄면 설정 창 목록과 무작위 배정에서 빠진다(만드는 중인 맵 등)")]
    [SerializeField] private bool selectable = true;

    [Tooltip("게임 씬 경로. 아래 Scene에 씬을 넣으면 자동으로 채워진다")]
    [SerializeField] private string scenePath;

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset scene;

    private void OnValidate()
    {
        if (scene != null) scenePath = UnityEditor.AssetDatabase.GetAssetPath(scene);
        if (minPlayers < 1) minPlayers = 1;
        if (maxPlayers < 0) maxPlayers = 0;
    }
#endif

    public string Id => id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
    public string Description => description;
    public Sprite Thumbnail => thumbnail;
    public int MinPlayers => Mathf.Max(1, minPlayers);
    public int MaxPlayers => maxPlayers;
    public bool Selectable => selectable;
    public string ScenePath => scenePath;

    public bool HasScene => !string.IsNullOrEmpty(scenePath);

    public bool SupportsPlayerCount(int playerCount)
    {
        return playerCount >= MinPlayers && (MaxPlayers <= 0 || playerCount <= MaxPlayers);
    }
}
