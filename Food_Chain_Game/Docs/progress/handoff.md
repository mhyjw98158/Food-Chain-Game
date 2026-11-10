# 작업 참고 메모

작업을 이어갈 때 읽는 문서. **진행 상태와 남은 일은 `progress.md`**, 프로젝트 규칙은 `CLAUDE.md`, 변경 상세는 `Docs/review/review-progress-2.md`에 있다.

## 1. 시스템별 메모 (2026-10-17 ~ 18 구현분)

### 탐색 오브젝트
- 플레이어 탐색은 탐색 오브젝트에서만, 모든 생존자 가능. 횟수: 일반 `investigationCount`(1), 조력자 `helperInvestigationCount`(2). 서버가 판정하고 결과를 보낼 때 차감
- 시체 조사(부검)는 조력자 전용. 결과창·노트 제목은 "부검 결과" / "탐색 결과"

### 맵
- 맵 경계: x -45.75 ~ 45.75, y -9 ~ 26.25(10-17에 공간 1.5배, 미니맵 그림 다시 찍음)

### 위장·예측, 승리 조건
- `DisguiseObject`(카멜레온, 1회), `PredictObject`(올빼미, 1회). 들판 탐색 오브젝트 오른쪽(임시 그림)
- 올빼미: 예측한 플레이어가 이기면 함께 승리(`IDependentWinCondition`, `GameResultEvaluator`에서 나중에 계산)
- 까마귀: 늑대 조력자(늑대 생존). 전갈: 독 반격 한 번 성공하면 승리(`ScorpionAbility.HasCountered`). 카멜레온: 생존
- 위장 가능 여부는 `CanDisguise`(카멜레온인지)만 본다
- 테스트용 직업 선택: `RoomManager.allowCharacterSelectForTest`, 에디터·개발 빌드에서만

### 미니맵
- 상호작용 마커: 할 수 있는 미션 노랑, 탐색·위장·예측 보라. 다 쓰면 사라짐
- 코너 미니맵은 시야 반경만큼만(`fitToVision`), 벽 가림은 없음
- 플레이어 마커: 내 시야 안 플레이어를 그 색으로. 얼룩말·타조는 파트너와 파트너 시야 안 플레이어도(`FOVMaskController2D.IsVisibleFrom`)

### 커스터마이징
- 스크립트 `Assets/Script/Costume/`, 목록 `Assets/Resources/CostumeCatalog.asset`(0번 기본, 강아지·고양이·토끼)
- 선택값 `RoomSessionData.CostumeIndex`(PlayerPrefs) → `RoomPlayer` → `GamePlayer`의 `costumeIndex` SyncVar
- **그림 넣는 법:** `Assets/Data/Costume/Costume_○○.asset`의 `bodySprite`(플레이어 색이 입혀짐), 필요하면 `overlaySprite`(색 없이 덮음), `icon`
- 새 의상은 목록 **끝**에 추가(순서를 바꾸면 저장된 번호가 달라짐)

### ESC 닫기 스택 (`UIManager`)
- `RegisterEscape(key, onEscape)`: 창을 열 때 등록(`onEscape`가 null이면 그 창이 떠 있는 동안 ESC 차단). `UnregisterEscape(key)`: 닫을 때. `HandleEscape()`: 가장 나중 창을 닫음
- 처리 순서(`SettingManager`): 키 설정 중 무시 → 나가기 확인창 → 설정 창 → 스택 맨 위 → 없으면 설정 창 열기
- ESC 차단: 미션 중, 탐색·부검 진행 중. ESC로 안 닫는 것: 채팅, 프로필, 캐릭터 소개, 결과 화면
- `UIManager.CloseAllEscapable()`: 스택의 창을 위에서부터 전부 닫는다(사망 시 사용). onEscape가 null인 항목은 목록에서만 빠지므로 그 작업은 먼저 취소해 둔다

### 사망 처리 (`GamePlayUI.BeginLocalDeath`)
- 본인 사망 시: 미션 실패·대기 중 미션 요청 취소(GamePlayer) → 탐색·부검 취소 → 창 닫기 → 사망 연출
- **사망 연출 넣는 법:** 연출 오브젝트(꺼 둔 상태)를 만들어 `GamePlayUI`의 `deathEffectUI`에 연결, 길이는 `deathEffectSeconds`. 켜질 때 재생되도록 만들면 된다. 그동안 이동·ESC가 막힌다

### 상호작용 표시
- 테두리(`InteractOutline`)와 키 안내(`InteractPromptUI`)는 `Scanner.currentInteractTarget`만 따라간다. 새 상호작용 대상은 Scanner에 추가하면 표시도 함께 된다(행동 이름은 `GetActionName`)
- 색·두께·맥동은 `Assets/Resources/InteractOutlineSettings.asset`. 복사본은 처음 표시할 때 만들어지므로 플레이 중에 두께를 바꾸면 그 판의 이미 만든 테두리에는 반영되지 않는다
- 테두리는 대상의 루트 SpriteRenderer 모양을 따른다. 그림을 자식 오브젝트로 넣으면 테두리가 생기지 않는다
- 테두리는 원본보다 정렬이 1 낮다. 오브젝트 정렬이 바닥보다 2 이상 높아야 바닥에 묻히지 않는다
- 고정 오브젝트의 테두리는 시야 마스크를 받지 않고, 셰이더가 원본 자리를 뚫어 테두리만 남긴다. 플레이어·시체는 시야를 따른다(`InteractOutline.AddCopy`)

### 이동 (서버 권위, `ServerAuthMovement`)
- GamePlayer 위치는 서버가 정한다. 본인 클라이언트는 매 물리 틱 입력(8방향)을 보내면서 같은 계산으로 먼저 움직이고(예측), 서버가 돌려준 위치·입력 번호로 어긋난 만큼 다시 계산한다. 차이는 화면에서만 `correctionSmoothTime`(0.1초)에 걸쳐 메운다
- 다른 클라이언트는 `OwnerPredictedNetworkTransform`(Server→Client, 본인 캐릭터에는 적용 안 함)으로 서버 위치를 보간해 본다. 방장 화면의 다른 플레이어는 입력이 몰려 와도 끊기지 않게 표시만 따라간다(`serverDisplayDelayTicks`)
- 이동 계산은 물리 엔진이 아니라 콜라이더 캐스트(`Simulate`)라 서버와 클라이언트가 같은 결과를 낸다. Rigidbody2D는 Kinematic + Full Kinematic Contacts(구역 트리거 등 유지). 충돌 대상은 물리 설정의 레이어 충돌표(산 사람 `GamePlayer`, 유령 `Ghost`)를 그대로 쓴다
- 하늘 차단막(날지 못하는 동물)·밤 차단막(밤 동안 모두)은 클라이언트마다 켜지는 조건이 달라 실제 콜라이더 대신 규칙으로 판정한다(`GamePlayUI.skyBlock`·`block` 아래 BoxCollider2D의 위치만 읽음). 차단막을 옮기거나 새로 만들면 이 두 오브젝트 아래에 두면 된다. 유령은 통과
- 스피드핵: 서버는 실제 시간 한 틱에 입력 1개까지만 처리한다(렉 대비 `maxBurstInputs` 25틱까지 몰아서). 쌓인 입력이 `maxQueuedInputs`를 넘으면 버리고 클라이언트를 보정한다. 라운드가 진행 중이 아니면 서버가 이동을 막는다
- **게임 중에 서버에서 플레이어를 옮길 때는 `ServerAuthMovement.ServerTeleport`를 쓴다.** transform만 바꾸면 다음 틱에 원래 계산 위치로 돌아간다
- 이동 속도를 바꾸는 기능은 `PlayerMove.moveSpeed`(SyncVar)를 쓴다. 서버·클라이언트 예측이 같은 값을 쓴다
- 본인 캐릭터는 Rigidbody2D 보간 없이 물리 틱(50Hz)마다 그린다. 보간을 시험했다가 사용자 결정으로 뺐다(2026-11-08, 반응 0.02초 지연 대신 부드러움). 다시 넣을 때는 본인 캐릭터에만 켤 것: 다른 사람 캐릭터는 transform을 직접 옮겨서 보간과 서로 덮어쓴다
- 방장 본인은 서버이므로 검증을 받지 않는다. 로비(RoomPlayer)는 예전대로 클라이언트 권위 이동이다

### 방장 게임 설정 (`GameSettings`)
- 값: `GameSettingsData`(라운드 수, 낮·밤 시간, 방 제목, 공개 여부, 맵 id, 포식자 수·중립 수(-1 = 자동)). 범위·간격·기본값은 `Assets/Resources/GameSettingsRules.asset`
- 흐름: 방장 슬라이더(`GameSettingsUI`) → `RoomPlayer.CmdChangeGameSettings`(방장인지 확인) → `RoomManager.ServerChangeGameSettings`(로비·시작 전만) → `GameSettings.ServerApply`(범위로 자름, 방장 PC PlayerPrefs 저장, `GameSettingsMessage`로 전파). 입장·로비 복귀 때 `OnServerReady`에서 다시 보낸다
- 게임 적용: `GameMamager.OnStartServer` → `ApplyGameSettings`(`maxRounds`·`dayRoundTime`·`nightRoundTime`은 SyncVar라 클라이언트도 같은 값). 게임 씬을 바로 실행하면 씬의 값을 쓴다
- 새 설정을 넣을 때: `GameSettingsData` 필드 → `GameSettingsRules`(범위·Clamp·Default) → `GameSettings`의 PlayerPrefs 키 → UI 행 → 게임 쪽 적용 순서로 추가
- 문구에 쓰는 글자는 `KCC-Hanbit SDF`(정적 아틀라스)에 있어야 한다. `·`가 없어서 요약은 `/`로 구분했다

### 역할 구성 (`RoleComposition`)
- 구성표는 `Assets/Resources/RoleComposition.asset`: 역할마다 분류(포식자·중립·피식자·조력자), 항상 포함, `requires`(이 역할이 있을 때만), `pairWith`(함께만, 양쪽에 적음). 자동일 때 인원별 수는 `autoPredators`·`autoNeutrals`(minPlayers 이상이면 count)
- 게임 시작 때 `RoomManager.GetCharacterPool` → `RoleComposition.Build(인원, 설정)`. 로비 표시는 같은 규칙의 `ResolveCounts`
- **새 역할을 넣을 때:** `AnimalType`·`CharacterZoneData`·`CharacterConfig` 외에 `RoleComposition.asset`의 `roles`에도 넣어야 게임에 나온다. 포식자가 늘면 `GameSettingsRules.predators.max`도 같이 올린다
- 피식자·조력자 자리가 모자라면 남은 중립 → 남은 포식자 순으로 더 넣는다(13명 기본은 중립 3)

### 맵 (`MapDefinition`, `MapCatalog`)
- 맵 하나 = 게임 씬 하나. 목록은 `Assets/Resources/MapCatalog.asset`, 맵마다 `Assets/Data/Map/Map_○○.asset`(지금은 `Map_Default` = `GamePlay.unity`)
- **맵 추가 방법:** `GamePlay.unity`를 복제해 배치를 바꾼다 → 빌드 설정(Scenes In Build)에 넣는다 → `Assets/Data/Map`에서 Create > Game > Map Definition, `id`(영문 소문자, 한 번 정하면 바꾸지 않음)·이름·씬·인원 범위를 채운다 → `MapCatalog`의 `maps` 끝에 넣는다. 만드는 중인 맵은 `selectable`을 끈다
- 맵이 2개 이상이면 설정 창에 "무작위"가 생긴다. 시작 때 서버가 `MapCatalog.ResolveForStart`로 실제 맵을 정한다(무작위면 지금 인원으로 가능한 맵 중에서, 고른 맵이 인원 범위 밖이면 시작을 막고 사유 표시) → `RoomManager.GameplayScene`을 그 씬으로 바꾸고 연다. 클라이언트는 `OnClientChangeScene`에서 같은 값으로 맞춘다
- 게임 씬인지 판정할 때 `"GamePlay"` 이름을 쓰지 말고 `MapCatalog.Instance.IsMapScene(...)`이나 `GameplayScene`을 쓴다
- 이번 게임의 맵 id는 서버의 `GameSettings.ServerAssignedMapId`(지금은 클라이언트에 보내지 않음)
- 문구(맵 이름 포함)는 글꼴 `KCC-Hanbit SDF`에 있는 글자만 쓴다. 에셋에 저장된 문구는 코드 기본값을 바꿔도 그대로이므로 에셋에서 고친다

### 방 제목·공개
- 설정 창의 방 제목(입력을 마칠 때 전송), 방 공개(켜면 공개 방 목록에 표시 예정). 서버 검증: 제어 문자 제거, 20자(`GameSettingsRules.titleMaxLength`), 금지어(닉네임 금지어 목록)·빈 제목은 "닉네임의 방", 서식 태그 차단. 기본 제목은 저장하지 않아 닉네임을 바꾸면 따라간다
- 설정이 바뀌면 `GameSettings.ServerChanged` → `RoomHost.SetRoomInfo`가 Node 서버에 바로 알리고, 이후 하트비트에도 싣는다(Node `rooms.title`·`is_public`·`map_id`)

### 공개 방 목록 (`RoomListUI`)
- 타이틀 "방 목록" 버튼 → `RoomListUI.Open` → `RoomJoin.FetchPublicRooms`(Node `GET /rooms`) → 줄(`RoomListRow`)마다 제목·맵·인원·입장. 입장은 `TitleUI.JoinRoom(code)`(코드 입장과 같은 흐름: 닉네임 확인 → `/lookup` → 접속)
- 공개·대기 중·자리 있는 방만 나온다. 비공개 방은 코드로만. 열려 있는 동안 5초마다 다시 불러온다(`autoRefreshSeconds`)
- 제목은 Node 서버가 받은 그대로 저장하므로 줄에 그릴 때 서식 태그를 다시 막는다(`RoomListRow.Bind`). 맵 이름은 `MapCatalog.DisplayName(mapId)`(모르는 맵 id는 "-")
- 창은 타이틀의 `activeUi`에 맡겨 ESC로 닫힌다

### 닉네임 (`NicknameRules`)
- 금지어는 `Assets/Resources/NicknameRules.asset`에서 고친다. 정상 단어에 들어가기 쉬운 말은 `exactBannedWords`에 넣는다
- 닉네임 검사는 설정할 때(타이틀)만 한다. 저장할 때 `Validate`, 방 만들기·입장 직전에 `TitleUI.EnsureValidNickname`(저장된 이름이 규칙에 어긋나면 변경 창을 띄우고 입장을 막음). 로비에서는 안내하지 않는다
- 서버(`RoomPlayer.CmdSetData` → `MakeRoomNickname`)는 조작된 클라이언트 대비로 금지어·빈 이름을 안내 없이 `Player`로 바꾼다
- 방에서 보이는 이름은 서버가 `RoomPlayer.CmdSetData`에서 정하고(숫자 붙음, 중복 안내는 띄우지 않음. 금지어 대체 안내만 띄움), 저장된 닉네임(`NickNamemanager`)과 다를 수 있다. 내 방 이름이 필요하면 `NickNamemanager`가 아니라 `RoomPlayer.nickname`을 쓴다

### 끊김 안내
- 문구와 오류 코드는 `NetworkErrorMessages` 한곳에 있다. 새 사유는 `NetworkErrorReason`에 코드를 정해 추가하고 `GetText`에 문구를 넣는다. 폰트 `KCC-Hanbit SDF`가 정적 아틀라스라 새 글자는 없을 수 있으니 `HasCharacters`로 확인한다
- 클라이언트 오류는 `OnClientError`에서 기록만 하고 타이틀 복귀는 `OnClientDisconnect`에서 한다. 게임 버전은 `Application.version`(Player Settings의 Version)을 비교한다

### 추방 (`RoomManager`)
- 클라이언트가 접속 직후 `ClientIdentityMessage`로 기기 ID를 보내고, 방장(Mirror 서버)이 추방 목록과 대조한다. 추방 목록은 방장 메모리에만 있고 새 방을 열면 비워진다. Node 서버는 관여하지 않는다
- 기기 ID는 변조된 클라이언트까지 막지는 못한다. 빌드 둘을 한 PC에서 띄우면 같은 ID라 함께 막힌다(에디터 클론은 구분됨)

## 2. 작업 방법과 주의할 점

### 사용자 지침
- 답변은 한국어.
- 씬·프리팹은 **Unity MCP로만** 수정한다. `.scene` / `.prefab` YAML 직접 편집 금지.
- C# 수정 뒤에는 MCP로 컴파일 성공을 확인하고, 컴파일 에러 없는 상태로만 마무리한다.
- 수치는 `[SerializeField]`로 빼고 하드코딩하지 않는다.
- 기능 단위로 나눠 하나씩 끝내고 확인받는다.
- 코드를 수정하면 `Docs/review/review-tests-2.md`에 확인 항목을, `Docs/review/review-progress-2.md`에 변경 내역을 적고, `Docs/progress/progress.md`의 남은 작업을 갱신한다(규칙: `Docs/review/review-rules.md` 6번).
- ParrelSync 클론에서는 에셋을 수정하지 않는다(메인 에디터에서만 수정, 클론은 새로 고침만).
- 플레이 모드는 필요하면 직접 꺼도 된다(사용자 허락).
- push는 사용자 확인 후에만.

### 미션 수정·테스트 (2026-11-10 사용자 지시)
- 미션을 고칠 때는 게임용 코드·프리팹을 바로 바꾸지 않는다. `Assets/Prefab/Mission/MissionTest/Mission_○○_Test.prefab`(게임 프리팹 복사)와 필요하면 `○○Test` 스크립트를 만들고, `Mission Test` 씬의 `Btn GroupB/Btn Group1`에 버튼을 복제해 OnClick을 `Mission Test Launcher.Launch(테스트 프리팹)`으로 연결한다(씬에 미션 인스턴스를 두지 않는다)
- `Mission Test` 씬: `MissionTestLauncher`가 그룹 탭(`groups` 목록: 탭 버튼 ↔ 버튼 묶음)으로 한 그룹만 보여 주고, 미션은 `Mission Root`(화면 전체)에 프리팹을 새로 만들어 연다. 새 그룹 묶음을 만들면 `groups`에 탭과 함께 추가. Canvas Scaler는 게임과 같은 1920x1080·0.5
- 사용자가 테스트해서 확정하면 게임에 적용: 테스트 스크립트 내용을 게임 스크립트로 옮기고(클래스 이름만 원래대로), 바뀐 인스펙터 값·오브젝트를 게임 프리팹(`MissionPrefab/`)에도 반영한 뒤, 테스트 스크립트를 지우고 테스트 프리팹은 게임 프리팹 새 복사본으로 다시 맞춘다
- fishC(`Mission_FishC`, 게임 적용됨, 맵 오브젝트 `Room Map/River/Fish C`)는 B-1 버튼으로도 시험할 수 있다. fishB·fishC는 같은 `FishB` 진행 코드를 쓰고 던지는 도구만 다르다(`FishNetBBase` ← `FishNetB` 그물 / `HarpoonC` 작살). 물고기 움직임도 같은 `FishSchoolB`(fishC는 큰 물고기 하나를 schoolShadow로)

### Unity MCP
- 인스턴스
  - 메인(호스트): `Food_Chain_Game@4ff81786c685e2db`
  - 클론 0: `Food_Chain_Game_clone_0@e2d3bb4307baeb03`
  - 클론 1: `Food_Chain_Game_clone_1@07f28478eb06ef18` (반응이 느릴 때가 있다)
- `set_active_instance`로 대상을 바꾼 뒤 `execute_code`를 쓴다. `execute_code`는 `compiler: codedom`(C# 6 문법만)으로 쓴다.
- 프리팹 수정: `execute_code` 안에서 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`.
- 컴파일 확인: `refresh_unity`(compile: request) 후 20초쯤 기다렸다가 `EditorUtility.scriptCompilationFailed`와 콘솔을 본다. 바로 읽으면 컴파일 중이라 비어 있을 수 있다.
- 기다릴 때는 `sleep`을 백그라운드로 실행한다(포그라운드 sleep은 막혀 있다).

### 주의할 점
- **스크립트를 쓰기 전에 세 에디터가 플레이 중인지 먼저 확인**한다. 플레이 중에 컴파일되면 그 세션이 깨진다(실제로 사용자의 플레이 세션을 한 번 날린 적이 있다).
- 메인 에디터가 플레이 중에 이유 없이 도메인을 다시 불러오는 일이 몇 번 있었다(원인 파일 미확인). 테스트가 중간에 끊기면 다시 시도한다.
- **`GamePlay.unity`는 저장할 때마다 그림자 메시가 다시 만들어져 무관한 변경이 1만 줄 넘게 생긴다.** 꼭 필요할 때만 저장하고, 씬을 열어 본 뒤 저장하지 않고 닫아도 "변경됨"으로 표시되는 것은 이 때문이다.
- `MinimapView`는 컴포넌트를 붙이는 순간 `OnValidate`가 기본값(우측 하단 300x300)으로 위치를 덮어쓴다. 새로 붙일 때는 Corner를 먼저 정한 뒤 위치를 잡는다.
- `execute_code` 안에서 `OpenScene`을 부르면 그 전에 불러 둔 에셋 참조가 무효가 된다. 씬을 연 뒤에 에셋을 다시 불러온다.
- `Sprite.OverrideGeometry`는 플레이 루프 밖에서 부르면 "Not allowed to override geometry" 에러가 난다(리플렉션으로 `BuildMask`를 직접 부를 때 생김. 평소 실행에서는 나지 않는다).
- PlayerPrefs는 세 에디터가 공유한다. 테스트로 바꿨으면 되돌린다(`PlayerNickname`은 `1234`, `Voice_MicMuted`는 0).
- 한글이나 따옴표가 든 여러 줄 수정은 스크래치패드에 파이썬 스크립트를 써서 실행하는 편이 안전하다(BOM과 줄바꿈 유지).

### 네트워크 없이 화면 확인하는 방법 (미니맵·시야에 사용)
1. 메인 에디터에서 `GamePlay` 씬을 열고 플레이.
2. 꺼 둔 빈 오브젝트 아래에 `GamePlayer.prefab`을 생성해 `Awake`가 돌지 않게 한 뒤, `NetworkIdentity`를 뺀 모든 스크립트·콜라이더·리지드바디를 끄고 밖으로 꺼낸다.
3. 리플렉션으로 `GamePlayer.LocalPlayer`에 넣는다. 카메라는 `CinemachineBrain`을 끄고 직접 옮긴다.
4. `ScreenCapture.CaptureScreenshot`으로 찍어 확인한다.
5. 끝나면 플레이를 끄고, 씬을 저장하지 않은 채 Start 씬으로 돌아간다.

이 방법으로는 실제 스폰, 동기화, 키 입력을 확인할 수 없다. 5단계는 호스트 + 클라이언트로 실제 방에 들어가서 해야 한다(호스트 시작은 플레이 후 3초쯤 지나 `NetworkCoordinator.Instance`와 `NetworkManager.singleton`이 생긴 뒤에).

### 맵·씬 구조 요약
- 게임 씬 `GamePlay`: 타일맵 없이 색칠된 사각형 스프라이트. 구역 4개(숲 왼쪽, 들판 가운데, 강 오른쪽, 하늘 가운데 위)의 T자 모양.
- 캔버스 3개 모두 1920x1080 기준 Scale With Screen Size: `Canvas`(order 0), `ICanvas`(order 10, 설정 창 등), `Mission Canvas`(order 100).
- 키 입력은 Input Manager 축이 아니라 `KeySetting`(자체 키 설정). 새 동작은 `KeyAction`의 `KEYCOUNT` 바로 앞에 추가하고 `defaultKeys`, 프리팹의 줄·배열·`ChangeKey` 번호를 함께 맞춘다.
- 역할(`animalType`, `isPredator`)은 서버와 본인만 가진다. 미니맵에는 쓰지 않는다.
