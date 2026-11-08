using System;
using Mirror;
using UnityEngine;

// 방장이 로비에서 바꾸는 게임 설정 값
[Serializable]
public struct GameSettingsData
{
    public int roundCount;
    public int daySeconds;
    public int nightSeconds;
    // 방 목록에 보이는 제목. 서버가 검증한 값(서식 태그 차단, 길이 제한)
    public string roomTitle;
    // 공개 방 목록에 보일지(false면 코드로만 입장)
    public bool isPublic;
    // 고른 맵의 MapDefinition.Id 또는 MapCatalog.RandomId
    public string mapId;
    // 포식자 수·중립 역할 수. RoleComposition.Auto(-1)면 인원별 구성표를 따른다
    public int predatorCount;
    public int neutralCount;

    public bool Equals(GameSettingsData other)
    {
        return roundCount == other.roundCount
            && predatorCount == other.predatorCount
            && neutralCount == other.neutralCount
            && daySeconds == other.daySeconds
            && nightSeconds == other.nightSeconds
            && roomTitle == other.roomTitle
            && isPublic == other.isPublic
            && mapId == other.mapId;
    }
}

// 서버 → 클라이언트: 현재 게임 설정(입장할 때, 바뀔 때마다)
public struct GameSettingsMessage : NetworkMessage
{
    public GameSettingsData data;
}

// 게임 설정 보관소.
// - 서버(방장): 원본(ServerCurrent). 방장이 바꾸면 검증 후 저장하고 모두에게 보낸다. 다음 방에서도 쓰도록 PlayerPrefs에 저장
// - 클라이언트: 받은 사본(ClientCurrent). 로비 표시용. 게임 규칙에는 서버 값만 쓴다
public static class GameSettings
{
    private const string RoundsKey = "GameSettings_Rounds";
    private const string DayKey = "GameSettings_DaySeconds";
    private const string NightKey = "GameSettings_NightSeconds";
    private const string TitleKey = "GameSettings_RoomTitle";
    private const string PublicKey = "GameSettings_IsPublic";
    private const string MapKey = "GameSettings_MapId";
    private const string PredatorsKey = "GameSettings_Predators";
    private const string NeutralsKey = "GameSettings_Neutrals";

    public static GameSettingsData ServerCurrent { get; private set; }
    public static bool ServerReady { get; private set; }

    // 이번 게임에 서버가 배정한 맵(무작위면 실제로 뽑힌 맵). 게임을 시작할 때 정해진다
    public static string ServerAssignedMapId { get; private set; }

    public static GameSettingsData ClientCurrent { get; private set; }
    public static bool ClientHasValue { get; private set; }

    // 서버에서 설정이 바뀌었을 때(Node 서버에 방 정보를 알리는 데 쓴다)
    public static event Action<GameSettingsData> ServerChanged;
    // 클라이언트에서 새 설정을 받았을 때
    public static event Action<GameSettingsData> ClientChanged;

    // 방장은 이 PC의 플레이어이므로 저장된 닉네임을 기본 제목에 쓴다
    private static string HostNickname => NickNamemanager.GetNickname();

    // ───── 서버 ─────

    // 방을 열 때 방장이 지난번에 쓴 설정을 불러온다. 저장된 제목이 없으면 "닉네임의 방"
    public static void ServerLoad()
    {
        var rules = GameSettingsRules.Instance;
        var defaults = rules.Default;
        ServerCurrent = rules.Clamp(new GameSettingsData
        {
            roundCount = PlayerPrefs.GetInt(RoundsKey, defaults.roundCount),
            daySeconds = PlayerPrefs.GetInt(DayKey, defaults.daySeconds),
            nightSeconds = PlayerPrefs.GetInt(NightKey, defaults.nightSeconds),
            roomTitle = PlayerPrefs.GetString(TitleKey, defaults.roomTitle),
            isPublic = PlayerPrefs.GetInt(PublicKey, defaults.isPublic ? 1 : 0) != 0,
            mapId = PlayerPrefs.GetString(MapKey, defaults.mapId),
            predatorCount = PlayerPrefs.GetInt(PredatorsKey, defaults.predatorCount),
            neutralCount = PlayerPrefs.GetInt(NeutralsKey, defaults.neutralCount),
        }, HostNickname);
        ServerReady = true;
        ServerAssignedMapId = null;
        ServerChanged?.Invoke(ServerCurrent);
    }

    // 검증한 값을 저장하고 모두에게 보낸다. 바뀌었으면 true
    public static bool ServerApply(GameSettingsData requested)
    {
        var clamped = GameSettingsRules.Instance.Clamp(requested, HostNickname);
        if (ServerReady && clamped.Equals(ServerCurrent))
        {
            // 요청한 값이 검증으로 바뀌어 원래 값과 같아졌으면(금지어 제목 등) 방장 화면을 되돌리도록 다시 보낸다
            NetworkServer.SendToReady(new GameSettingsMessage { data = ServerCurrent });
            return false;
        }

        ServerCurrent = clamped;
        ServerReady = true;

        PlayerPrefs.SetInt(RoundsKey, clamped.roundCount);
        PlayerPrefs.SetInt(DayKey, clamped.daySeconds);
        PlayerPrefs.SetInt(NightKey, clamped.nightSeconds);
        // 기본 제목("닉네임의 방")은 저장하지 않는다. 닉네임을 바꾸면 다음 방에서 새 닉네임으로 보이게
        bool isDefaultTitle = clamped.roomTitle == GameSettingsRules.Instance.DefaultTitle(HostNickname);
        PlayerPrefs.SetString(TitleKey, isDefaultTitle ? "" : clamped.roomTitle);
        PlayerPrefs.SetInt(PublicKey, clamped.isPublic ? 1 : 0);
        PlayerPrefs.SetString(MapKey, clamped.mapId);
        PlayerPrefs.SetInt(PredatorsKey, clamped.predatorCount);
        PlayerPrefs.SetInt(NeutralsKey, clamped.neutralCount);
        PlayerPrefs.Save();

        NetworkServer.SendToReady(new GameSettingsMessage { data = clamped });
        ServerChanged?.Invoke(clamped);
        return true;
    }

    public static void ServerSendTo(NetworkConnectionToClient conn)
    {
        if (conn == null || !ServerReady) return;
        conn.Send(new GameSettingsMessage { data = ServerCurrent });
    }

    // 게임 시작 때 배정한 맵을 기록한다
    public static void ServerSetAssignedMap(string mapId)
    {
        ServerAssignedMapId = mapId;
    }

    public static void ServerReset()
    {
        ServerReady = false;
        ServerAssignedMapId = null;
    }

    // ───── 클라이언트 ─────

    public static void ClientRegisterHandler()
    {
        ClientHasValue = false;
        NetworkClient.ReplaceHandler<GameSettingsMessage>(OnClientReceived, false);
    }

    private static void OnClientReceived(GameSettingsMessage msg)
    {
        ClientCurrent = msg.data;
        ClientHasValue = true;
        ClientChanged?.Invoke(msg.data);
    }
}
