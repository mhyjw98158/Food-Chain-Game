# Steam 출시 대비 — 접속 구조 전환 계획

작성: 2026-10-30 / 상태: **계획만 있음(미구현)**

## 결정

- 지금은 방장 PC에 UDP로 직접 접속하는 구조를 유지한다(같은 망이나 포트포워딩 환경에서 플레이).
- 출시 목표가 Steam이므로, 출시 준비 단계에서 Steam 네트워킹으로 옮긴다. 공유기 뒤 방장 문제는 그때 해결된다.
- 방장이 나가면 방이 닫히고, 튕긴 플레이어는 사망 처리되는 동작은 유지한다(호스트 이전·게임 중 재접속 없음).

## Steam으로 옮기면 해결되는 것

| 지금 문제 | Steam에서 |
|---|---|
| 방장이 포트포워딩을 해야 외부에서 들어올 수 있음 | Steam 릴레이(SDR)가 중계해 필요 없음. 방장 IP도 드러나지 않음 |
| 기기 ID를 클라이언트가 보내서 변조하면 추방을 우회할 수 있음 | 서버가 Steam 인증 티켓으로 SteamID를 검증하면 위조 불가 |
| 방 코드를 메신저로 전달해야 함 | Steam 친구 초대, 오버레이에서 참가 |
| 게임 버전을 Player Settings로 직접 관리 | Steam 빌드 ID와 자동 업데이트로 버전이 맞춰짐 |

## 해결되지 않는 것

- **방장 치트:** 방장 PC가 서버이므로 방장은 검증을 받지 않는다(`ServerMoveValidator`도 방장은 제외). 막으려면 전용 서버가 필요하다.
- **방장 이탈:** 게임 중 방장이 나가면 그대로 게임이 끝난다.
- **Node 서버 필요성:** Steam 로비로 방 코드를 대체하면 필요 없어지지만, 음성 토큰을 서버가 발급한다면 계속 필요하다.

## 필요한 준비

1. Steamworks 파트너 가입, 앱 등록(등록비 있음), AppID 발급
2. Steamworks 패키지 선택 — Mirror용 트랜스포트가 있는지 그때 버전으로 확인(Mirror 96 호환 여부 포함)
3. 개발 중에는 테스트용 AppID로 시험. **같은 계정으로 두 클라이언트를 띄울 수 없어** 지금의 ParrelSync 테스트 방식이 그대로 통하지 않는다. 테스트 계정이 2개 이상 필요하다

## 코드에서 바뀌는 곳

이 목록이 전환 작업의 범위다. 지금 코드는 아래 지점 외에는 트랜스포트에 묶여 있지 않다.

| 파일 | 지금 | 바뀌는 내용 |
|---|---|---|
| `RoomManager` 프리팹(`Start.unity`) | `KcpTransport` | Steam 트랜스포트로 교체 |
| `ConfigManager.TrySetTransportPort`, `HostGamePort` | 포트 지정 | 포트 개념이 없어짐. 호출부(`NetworkCoordinator`, `RoomJoin`)와 함께 제거 |
| `RoomJoin`, `RoomHost` | 방 코드 → IP:포트 | 방 코드 → SteamID로 변경, 또는 Steam 로비로 대체 |
| `Server/server.js` `/register`, `/lookup` | IP·포트 저장 | SteamID 저장(방 코드를 유지할 때) |
| `ClientIdentity` | 기기 ID 해시 | SteamID + 인증 티켓 검증 |
| `RoomManager.OnServerConnect` | 메시지로 ID를 받음 | Steam 인증 결과로 판단 |
| `NetworkErrorMessages` | KCP 오류 기준 | Steam 전용 사유(로그인 안 됨, 릴레이 실패 등) 추가 |

그대로 쓰는 것: 게임 로직 전부, 이동 검증(`ServerMoveValidator`), 끊김 처리 흐름(`OnClientDisconnect`, `TransportError` 변환), 연결 불안정 경고(`ConnectionStatusUI`), Vivox 음성.

## 방 코드를 유지할지 ★

전환할 때 정한다.

- **Steam 로비로 대체:** 친구 초대가 자연스럽고 Node 서버의 방 기능이 필요 없다.
- **방 코드 유지(Node 서버가 SteamID 중개):** 지금 UI를 그대로 쓰고, 친구가 아닌 사람도 코드로 들어올 수 있다.
