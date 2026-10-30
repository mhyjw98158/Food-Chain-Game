using UnityEngine;

// 끊김·접속 실패 사유별 안내 문구. 메시지 창에 제목, 설명, 오류 코드 순서로 표시한다.
// 오류 코드는 enum 값이다(1xx 접속 실패, 2xx 연결 끊김, 3xx 방 입장·퇴장, 4xx 시작 실패)
public static class NetworkErrorMessages
{
    private const string CannotRejoinNotice = "\n진행 중이던 게임에는 다시 참여할 수 없습니다.";
    private const string CheckInternetNotice = "인터넷 연결을 확인한 뒤 다시 시도해 주세요.";

    private const int TitleSize = 40;
    private const int BodySize = 30;
    private const int CodeSize = 22;
    private const string CodeColor = "#666666";

    public static string Build(NetworkErrorReason reason, bool inGame)
    {
        GetText(reason, inGame, out string title, out string body);

        // 연결이 끊긴 경우만 재참여 안내를 붙인다(방이 닫혔거나 입장을 거부당한 경우는 해당 없음)
        int code = (int)reason;
        if (inGame && code >= 200 && code < 300) body += CannotRejoinNotice;

        return $"<size={TitleSize}><b>{title}</b></size>\n"
             + $"<size={BodySize}>{body}</size>\n"
             + $"<size={CodeSize}><color={CodeColor}>오류 코드: {code}</color></size>";
    }

    // Node 서버에 닿지 못했을 때의 안내(방 만들기·코드 조회)
    public static string ServerUnreachable()
    {
        return Application.internetReachability == NetworkReachability.NotReachable
            ? CheckInternetNotice
            : "서버와 연결할 수 없습니다. 잠시 후 다시 시도해 주세요.";
    }

    private static void GetText(NetworkErrorReason reason, bool inGame, out string title, out string body)
    {
        switch (reason)
        {
            case NetworkErrorReason.ConnectTimeout:
                title = "방에 연결하지 못했습니다";
                body = "방장의 응답이 없습니다. 방장이 게임을 종료했거나 방장의 네트워크에 문제가 있을 수 있습니다.";
                break;
            case NetworkErrorReason.ConnectFailed:
                title = "방에 연결하지 못했습니다";
                body = "방이 닫혔거나 접속할 수 없는 상태입니다. 초대 코드를 확인한 뒤 다시 시도해 주세요.";
                break;
            case NetworkErrorReason.DnsFailed:
                title = "방 주소를 찾지 못했습니다";
                body = CheckInternetNotice;
                break;

            case NetworkErrorReason.ConnectionTimeout:
                title = "연결이 끊어졌습니다";
                body = "방장의 응답이 없어 연결을 종료했습니다. 방장의 게임이 종료되었거나 네트워크가 불안정할 수 있습니다.";
                break;
            case NetworkErrorReason.ConnectionLost:
                title = "연결이 끊어졌습니다";
                body = "방장과의 연결이 끊어졌습니다.";
                break;
            case NetworkErrorReason.Congestion:
                title = "연결이 끊어졌습니다";
                body = "네트워크가 너무 혼잡해 연결을 유지할 수 없습니다. 다른 프로그램의 네트워크 사용을 줄인 뒤 다시 시도해 주세요.";
                break;
            case NetworkErrorReason.InvalidData:
                title = "연결이 끊어졌습니다";
                body = "잘못된 데이터를 받아 연결을 종료했습니다. 문제가 반복되면 게임을 다시 실행해 주세요.";
                break;
            case NetworkErrorReason.NoInternet:
                title = "인터넷 연결이 끊어졌습니다";
                body = CheckInternetNotice;
                break;
            case NetworkErrorReason.ServerStopped:
                title = "방이 닫혔습니다";
                body = "게임 서버가 예기치 않게 중지되었습니다.";
                break;

            case NetworkErrorReason.HostClosedRoom:
                title = "방이 닫혔습니다";
                body = inGame ? "방장이 게임을 종료했습니다." : "방장이 방을 닫았습니다.";
                break;
            case NetworkErrorReason.Kicked:
                title = "방에서 추방당했습니다.";
                body = "이 방에는 다시 입장할 수 없습니다.";
                break;
            case NetworkErrorReason.Banned:
                title = "입장할 수 없습니다";
                body = "추방당해서 입장할 수 없습니다.";
                break;
            case NetworkErrorReason.GameInProgress:
                title = "입장할 수 없습니다";
                body = "게임이 진행 중인 방에는 입장할 수 없습니다.";
                break;
            case NetworkErrorReason.RoomFull:
                title = "입장할 수 없습니다";
                body = "방 인원이 가득 찼습니다.";
                break;
            case NetworkErrorReason.VersionMismatch:
                title = "입장할 수 없습니다";
                body = "방장과 게임 버전이 다릅니다. 같은 버전으로 업데이트한 뒤 다시 시도해 주세요.";
                break;

            case NetworkErrorReason.HostStartFailed:
                title = "방을 만들지 못했습니다";
                body = "게임 포트를 다른 프로그램이 사용 중일 수 있습니다. 게임을 다시 실행한 뒤 시도해 주세요.";
                break;
            case NetworkErrorReason.ClientStartFailed:
                title = "방에 연결하지 못했습니다";
                body = "네트워크를 시작하지 못했습니다. 잠시 후 다시 시도해 주세요.";
                break;

            default:
                title = "연결이 끊어졌습니다";
                body = "알 수 없는 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.";
                break;
        }
    }
}
