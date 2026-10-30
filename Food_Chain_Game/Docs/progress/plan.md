# 작업 계획 (테스트 제외, 2026-10-23 작성)

`progress.md`, `node-server-checklist.md`, `important.md`, `Docs/review/review-plan.md`에 흩어져 있던 남은 작업 중 **플레이 테스트를 뺀 것**을 순서대로 정리했다.
플레이 테스트는 `Docs/review/review-tests*.md`에서 따로 관리한다. 끝난 항목은 이 문서에서 지우고 `progress.md`의 "끝난 작업"에 한 줄로 남긴다.

- 게임 완성에 필요한 큰 기능·디테일(회의·투표, 결과 화면, 방장 설정, 아트·사운드 등) 후보는 `game-completion.md`. 하기로 정한 것만 이 문서로 옮긴다
- 진행 방식: 단계 안에서도 항목 하나씩 끝내고 확인받는다(CLAUDE.md). ★ = 시작 전에 사용자 결정 필요
- 순서 기준: ① 지금 바로 고칠 수 있는 버그 → ② 결정이 필요한 것 → ③ 기능 → ④ 서버·네트워크 → ⑤ 테스트 통과 후 리팩토링 → ⑥ 출시 전

---

## 1단계 — 바로 고칠 수 있는 버그·정리

### 테스트 후 개선사항 정리 (2026-10-30, 위에서부터 순서대로)
- (테스트 후 개선사항 9개 처리 완료, 2026-10-30)
- [ ] 서버가 30초 대기 후 시작한 다음에 늦게 들어온 플레이어는 플레이어 목록·미션 배정(`GameMamager.InitPlayers`)에서 빠짐 → 끊을지, 늦게라도 넣을지 정하기 ★ (보통은 타임아웃 30초로 그 전에 끊김)

## 2단계 — 사용자 결정이 필요한 것 ★

결정만 받으면 대부분 작은 수정이다. 한 번에 묻고 답에 따라 1·3단계에 넣는다.

| 항목 | 지금 상태 | 결정할 것 |
|---|---|---|
| 하마 복수 미션 내용 | `RevengeA`는 예전 독 미션 화면 복사본, 완료 효과 없음. `RevengeB`~`E`는 enum만 있음 | 미션 내용과 완료 시 복수 효과 |
| 새 캐릭터(올빼미·하마·카멜레온·미어캣) | 이름·코드 이름·거주지 변경 완료 | 능력·미션 풀을 새 동물에 맞게 바꿀지 |

## 3단계 — 기능·리소스

- [ ] **의상 그림 넣기** — `Assets/Data/Costume/Costume_○○.asset`의 `bodySprite`(+ 필요 시 `overlaySprite`, `icon`). 방법은 `handoff.md`
- [ ] 위장·예측 오브젝트, 로비 커스터마이징 오브젝트(`GameRoom` 씬 `Room Map/Customize Object`, 갈색 사각형) 임시 그림 교체
- [ ] 시작 연출 직업 일러스트 그림 넣기 — `GamePlay` 씬 `GamePlayUI`의 `Role Illustrations`(동물 → 그림). 없으면 내 캐릭터 모습으로 대신 보임
- [ ] 시작 연출 능력 설명 문구 확인·수정(`CharacterData.cs`의 `AbilityDescription`, 임시 작성)
- [ ] 귓속말 알림음 넣기 — 지금은 코드로 만든 임시 소리. `GamePlay` 씬 `Chat UI/Channel Button`의 `ChatChannelToggle.alertClip`에 넣으면 바뀜
- [ ] 음성 스피커 아이콘 임시 이미지(`Assets/Sprite/UI/SpeakerIcon_Temp.png`) 교체
- [ ] 미니맵 2차 기능(선택): 탐험한 영역만 보이는 안개, 사냥·미션 알림 깜빡임 — `Docs/prompt/minimap.md` 5번
- [ ] 음성 사소한 문제: 결과 화면 전환 때 약 2.4초 끊김
- [ ] 낮·밤 길이 정식 값 정하기 — 지금 `GamePlay` 씬 낮 30초(`dayRoundTime`)·밤 20초(`nightRoundTime`)라 시작부터 빨간 표시이고 굶주림 경고도 밤 시작 때 뜬다. 1분 넘게 늘리면 요청대로 동작 ★
- [ ] **구현해야 하는 미션** (2026-10-24 정리. 이미 있는 17종은 스크립트·UI 프리팹·`GamePlay` 씬 오브젝트가 다 연결됨)
  - (2026-10-24 결정: `Badger*` → `Revenge*` 이름 변경 완료, 같은 표시 이름(`Fruit_A`/`B`, `Wood_A`/`B`)은 그대로 둠, 전갈 독 미션 새로 구현 완료)
  - [ ] 하마 복수 미션 `RevengeA` — 화면은 예전 독 미션을 복사한 것(재료 버튼 누르기, 이름 "복수 A 미션"). 완료해도 `HippoAbility.ApplyPenalty`가 로그만 남기고 아무 효과가 없음 → 미션 내용과 복수 효과를 정해야 함 ★
  - [ ] 하마 복수 미션 `RevengeB`~`E` — enum과 `ApplyPenalty`의 빈 case만 있음(스크립트·프리팹·씬 오브젝트·이름 없음, `RevengeMissions`에서 주석 처리) ★
  - [ ] `Grass_C` "풀 세척하기" — enum만 있음(이름·카테고리 등록은 주석 처리)
  - [ ] 조개(`MissionCategory.Shellfish`) 미션 — 카테고리만 있고 미션도, 이 카테고리를 쓰는 동물도 없음

## 4단계 — Node 서버·네트워크 (`node-server-checklist.md`)

서버를 고치면 짝이 되는 Unity 쪽(`RoomHost`, `RoomJoin`, `TitleUI.StartHost`, `RoomManager`)도 같이 고친다.

- (2026-10-23 1~6번 완료. 실행 중인 Node 서버는 새 코드로 다시 켜야 적용 — pm2 사용 권장, `Server/README.md`)
- [ ] 원본 저장소 업로드 — 주소·브랜치·권한을 사용자에게 받은 뒤 ★

## 5단계 — 테스트 통과 후 리팩토링 (`Docs/review/review-plan.md`)

- [ ] R-1 미션 공통 뼈대를 베이스 클래스로(우선순위 높음)
- [ ] R-2 같은 미션 파일 통합, R-3 미션 정의 단일화, R-4 캐릭터 데이터 단일화, R-6 상호작용 오브젝트 공통화
- [ ] 이름 오타 정리(`GameMamager`, `NickNamemanager`, `WinCondutionFactory`, `hasEate`, `Utill`) — 단독 커밋
- [ ] `Setting UI.prefab` 정리: 원본 없는 음소거 토글 프리팹 4개, 탭 버튼 5개의 없는 메서드 연결 — **사용자의 다른 PC에서 토글 프리팹을 가져온 뒤** ★

## 6단계 — 출시 전 과제

- [ ] 방 정원 정식 기본값·범위 정하기(최소 3, 목표 7 이상) — 기본 구현이 끝난 뒤. 그 전까지는 테스트용으로 1명부터 가능하게 둔다 ★
- [ ] **음성 채널 입장 토큰**(엿듣기 방지) — 보류, Steam 출시 준비 때 진행(2026-10-30 결정). 시작 전 확인 사항은 `important.md`. 우선순위: `dead_` 채널 제한 → 방 구성원 외 차단 → 거리·벽 무시 듣기 방지
- [ ] Steam 네트워킹 전환 — 지금은 직접 접속 유지(2026-10-30 결정), 계획은 `steam-plan.md`. 호스트 이탈·재접속은 지금 동작 유지로 결정
- [ ] Cloudflare Tunnel 실제 적용 — 코드·문서 준비 완료(`Server/README.md`). `cloudflared` 설치, 터널 실행, `config.json`의 `ServerUrl` 설정은 사용자가 진행
- [ ] 저장소·설정: `.gitignore`의 `*.json` 전체 무시, `config.json` 배포 방식, 추적 안 된 파일(`Examples & Extras/`, `*.slnx`, `.claude/`) 처리, 회사 이름 `DefaultCompany` 확정 ★
