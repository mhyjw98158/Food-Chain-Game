using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 고를 수 있는 맵 목록(Resources/MapCatalog.asset).
// - 방장은 설정 창에서 맵 하나 또는 "무작위"를 고른다(GameSettingsData.mapId)
// - 게임을 시작할 때 서버가 실제로 쓸 맵을 배정한다(ResolveForStart): 무작위면 지금 인원으로 시작할 수 있는 맵 중에서 고른다
[CreateAssetMenu(fileName = "MapCatalog", menuName = "Game/Map Catalog")]
public class MapCatalog : ScriptableObject
{
    private const string ResourcePath = "MapCatalog";

    // 설정 값으로 쓰는 "무작위"의 id. 맵 id와 겹치지 않게 맵 id에는 쓰지 않는다
    public const string RandomId = "random";

    [Tooltip("맵 목록. 새 맵은 끝에 추가한다. 첫 번째 고를 수 있는 맵이 기본값")]
    [SerializeField] private List<MapDefinition> maps = new List<MapDefinition>();
    [Tooltip("설정 창에 \"무작위\"를 보여 줄지")]
    [SerializeField] private bool allowRandom = true;
    [SerializeField] private string randomDisplayName = "무작위";
    [Tooltip("고른 맵의 인원 범위를 벗어났을 때. {0}: 맵 이름, {1}: 인원 범위 문구(PlayerRangeText)")]
    [SerializeField] private string unsupportedCountMessage = "{0} 맵은 {1}이 필요합니다.";
    [Tooltip("인원 범위 문구. {0}: 최소, {1}: 최대")]
    [SerializeField] private string playerRangeFormat = "{0}~{1}명";
    [Tooltip("최대 인원이 없는 맵의 인원 문구. {0}: 최소")]
    [SerializeField] private string playerMinOnlyFormat = "{0}명 이상";
    [SerializeField] private string noPlayableMapMessage = "지금 인원으로 시작할 수 있는 맵이 없습니다.";

    private static MapCatalog _instance;

    public static MapCatalog Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<MapCatalog>(ResourcePath);
                if (_instance == null)
                {
                    Debug.LogError($"[MapCatalog] Resources/{ResourcePath} 가 없습니다. 맵을 고를 수 없습니다.");
                    _instance = CreateInstance<MapCatalog>();
                }
            }
            return _instance;
        }
    }

    public bool AllowRandom => allowRandom && SelectableMaps().Count > 1;
    public string RandomDisplayName => randomDisplayName;

    // 설정 창 목록에 보여 줄 맵(고를 수 있고 씬이 있는 것)
    public List<MapDefinition> SelectableMaps()
    {
        var result = new List<MapDefinition>();
        foreach (var map in maps)
        {
            if (map != null && map.Selectable && map.HasScene) result.Add(map);
        }
        return result;
    }

    public string DefaultId
    {
        get
        {
            var selectable = SelectableMaps();
            return selectable.Count > 0 ? selectable[0].Id : RandomId;
        }
    }

    public MapDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var map in maps)
        {
            if (map != null && map.Id == id) return map;
        }
        return null;
    }

    // 설정 값으로 받을 수 있는 id인지(고를 수 있는 맵 또는 무작위)
    public bool IsSelectableId(string id)
    {
        if (id == RandomId) return AllowRandom;
        var map = Find(id);
        return map != null && map.Selectable && map.HasScene;
    }

    // 받을 수 없는 id면 기본 맵으로 바꾼다(서버 검증, 지난번 저장값이 지워진 맵일 때)
    public string Sanitize(string id)
    {
        return IsSelectableId(id) ? id : DefaultId;
    }

    public string DisplayName(string id)
    {
        if (id == RandomId) return randomDisplayName;
        var map = Find(id);
        return map != null ? map.DisplayName : "-";
    }

    // 씬 경로나 이름이 맵 씬인지(Mirror는 경로를, SceneManager는 이름을 넘긴다)
    public MapDefinition FindByScene(string sceneNameOrPath)
    {
        if (string.IsNullOrEmpty(sceneNameOrPath)) return null;
        string name = Path.GetFileNameWithoutExtension(sceneNameOrPath);
        foreach (var map in maps)
        {
            if (map == null || !map.HasScene) continue;
            if (map.ScenePath == sceneNameOrPath || Path.GetFileNameWithoutExtension(map.ScenePath) == name) return map;
        }
        return null;
    }

    public bool IsMapScene(string sceneNameOrPath) => FindByScene(sceneNameOrPath) != null;

    // "3~8명", "3명 이상". 인원 제한이 없으면(1명부터, 최대 없음) 빈 문자열
    public string PlayerRangeText(MapDefinition map)
    {
        if (map == null || (map.MinPlayers <= 1 && map.MaxPlayers <= 0)) return "";
        return map.MaxPlayers > 0
            ? string.Format(playerRangeFormat, map.MinPlayers, map.MaxPlayers)
            : string.Format(playerMinOnlyFormat, map.MinPlayers);
    }

    // 게임 시작 때 서버가 실제로 쓸 맵을 정한다. 시작할 수 없으면 null과 사유
    public MapDefinition ResolveForStart(string selectedId, int playerCount, out string failReason)
    {
        failReason = null;

        if (selectedId == RandomId)
        {
            var candidates = new List<MapDefinition>();
            foreach (var map in SelectableMaps())
            {
                if (map.SupportsPlayerCount(playerCount)) candidates.Add(map);
            }

            if (candidates.Count == 0)
            {
                failReason = noPlayableMapMessage;
                return null;
            }
            return candidates[Random.Range(0, candidates.Count)];
        }

        var selected = Find(Sanitize(selectedId));
        if (selected == null)
        {
            failReason = noPlayableMapMessage;
            return null;
        }

        if (!selected.SupportsPlayerCount(playerCount))
        {
            failReason = string.Format(unsupportedCountMessage, selected.DisplayName, PlayerRangeText(selected));
            return null;
        }
        return selected;
    }
}
