# 작업 계획 (테스트 제외, 2026-11-8 작성)

`progress.md`, `node-server-checklist.md`, `important.md`, `Docs/review/review-plan.md`에 흩어져 있던 남은 작업 중 **플레이 테스트를 뺀 것**을 순서대로 정리했다.
플레이 테스트는 `Docs/review/review-tests*.md`에서 따로 관리한다. 끝난 항목은 이 문서에서 지우고 `progress.md`의 "끝난 작업"에 한 줄로 남긴다.

- 진행 방식: 단계 안에서도 항목 하나씩 끝내고 확인받는다(CLAUDE.md). ★ = 시작 전에 사용자 결정 필요
- 순서 기준: ① 지금 바로 고칠 수 있는 버그 → ② 결정이 필요한 것 → ③ 기능 → ④ 서버·네트워크 → ⑤ 테스트 통과 후 리팩토링 → ⑥ 출시 전

---

## 1단계 — 바로 고칠 수 있는 버그·정리

- (현재 없음. 새로 발견한 작은 버그는 여기에 추가)

## 2단계 — 사용자 결정이 필요한 것 ★

## 3단계 — 기능·리소스

- [ ] **의상 그림 넣기** — `Assets/Data/Costume/Costume_○○.asset`의 `bodySprite`(+ 필요 시 `overlaySprite`, `icon`). 방법은 `handoff.md`
- [ ] 위장·예측 오브젝트 임시 그림 교체
- [ ] (2단계 결정 시) 구역 아이콘을 시야 기반으로 교체
- [ ] 미니맵 2차 기능(선택): 탐험한 영역만 보이는 안개, 사냥·미션 알림 깜빡임 — `Docs/prompt/minimap.md` 5번

## 4단계 — Node 서버·네트워크 (`node-server-checklist.md`)

서버를 고치면 짝이 되는 Unity 쪽(`RoomHost`, `RoomJoin`, `TitleUI.StartHost`, `RoomManager`)도 같이 고친다.

- [ ] **게임 진행 중 상태를 서버에 알리기**(1순위) — 게임 중인 방은 `/lookup`에서 바로 거부. Unity: 게임 시작·로비 복귀 시 상태 갱신, `RoomJoin`에 응답 코드·안내 문구
- [ ] 포트 3000 하드코딩 → `config.json`으로, `RoomHost`/`RoomJoin`의 `isConfigValid`를 매번 확인
- [ ] `NetworkCoordinator.EnsurePreviousSessionStopped` — 이전 세션이 남았을 때 방 코드/토큰이 지워진 뒤에도 `StartHost()`를 계속 시도하는 경로
- [ ] 서버 안정성: DB 콜백 예외로 프로세스 종료 방지, 자동 재시작(pm2 등)
- [ ] 서버 보안: 요청 횟수 제한, `cors()` 전체 허용 정리, 로그의 방 코드·IP 기록 줄이기
- [ ] 방장 PC가 심하게 느릴 때 정상 미션 완료가 최소 소요 시간 미달로 거부될 수 있음 — `minDurationSeconds` 판정을 실제 시간 기준으로 바꿀지 검토
- [ ] 원본 저장소 업로드 — 주소·브랜치·권한을 사용자에게 받은 뒤 ★

## 5단계 — 테스트 통과 후 리팩토링 (`Docs/review/review-plan.md`)

- [ ] R-1 미션 공통 뼈대를 베이스 클래스로(우선순위 높음)
- [ ] R-2 같은 미션 파일 통합, R-3 미션 정의 단일화, R-4 캐릭터 데이터 단일화, R-6 상호작용 오브젝트 공통화
- [ ] 이름 오타 정리(`GameMamager`, `NickNamemanager`, `WinCondutionFactory`, `hasEate`, `Utill`) — 단독 커밋
- [ ] `Setting UI.prefab` 정리: 원본 없는 음소거 토글 프리팹 4개, 탭 버튼 5개의 없는 메서드 연결 — **사용자의 다른 PC에서 토글 프리팹을 가져온 뒤** ★
2121
## 6단계 — 출시 전 과제

- [ ] **음성 채널 입장 토큰**(엿듣기 방지) — `important.md`. 우선순위: `dead_` 채널 제한 → 방 구성원 외 차단 → 거리·벽 무시 듣기 방지
- [ ] 접속 구조: 릴레이 또는 전용 서버(공유기 뒤 호스트·외부 접속), 호스트 이탈 시 방 유지·재접속 ★
- [ ] 서버 HTTPS(외부 공개 시)
- [ ] 저장소·설정: `.gitignore`의 `*.json` 전체 무시, `config.json` 배포 방식, 추적 안 된 파일(`Examples & Extras/`, `*.slnx`, `.claude/`) 처리, 회사 이름 `DefaultCompany` 확정 ★