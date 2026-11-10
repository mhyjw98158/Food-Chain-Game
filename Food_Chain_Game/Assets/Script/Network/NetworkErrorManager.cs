using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 값이 곧 화면에 표시되는 오류 코드다(NetworkErrorMessages). 1xx 접속 실패, 2xx 연결 끊김, 3xx 방 입장·퇴장, 4xx 시작 실패
public enum NetworkErrorReason
{
    None = 0,

    ConnectTimeout = 101,
    ConnectFailed = 102,
    DnsFailed = 103,

    ConnectionTimeout = 201,
    ConnectionLost = 202,
    Congestion = 203,
    InvalidData = 204,
    NoInternet = 205,
    ServerStopped = 206,

    HostClosedRoom = 301,
    Kicked = 302,
    Banned = 303,
    GameInProgress = 304,
    RoomFull = 305,
    VersionMismatch = 306,

    HostStartFailed = 401,
    ClientStartFailed = 402,

    Unknown = 900
}
public class NetworkErrorManager : MonoBehaviour
{
    public static NetworkErrorManager Instance { get; private set; }

    public NetworkErrorReason CurrentReason { get; private set; } = NetworkErrorReason.None;
    public string CurrentMessage { get; private set; } = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // inGame: 게임 플레이 중에 일어난 일인지(재참여 불가 안내 등 문구가 달라진다)
    public void SetError(NetworkErrorReason reason, bool inGame)
    {
        CurrentReason = reason;
        CurrentMessage = NetworkErrorMessages.Build(reason, inGame);
    }

    public void Clear()
    {
        CurrentReason = NetworkErrorReason.None;
        CurrentMessage = null;
    }

    public bool HasError => CurrentReason != NetworkErrorReason.None && !string.IsNullOrEmpty(CurrentMessage);
}
