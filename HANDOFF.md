# 현재 스프린트 — 런 사이클 (게임의 뼈대)

> **최종 목표(변경 없음): `reference/project_test.html` 원본을 Unity로 그대로 포팅.**
> 확정 날짜 2026-09-09. 이전 스프린트 스펙은 전부 `docs/sprints/`에 아카이브돼 있다(현재 스펙 아님).
>
> | 문서 | 소유하는 것 |
> |---|---|
> | **이 파일** | 지금 스프린트의 범위·수치·완료 기준 |
> | `docs/worksplit.md` | 남은 전체 작업의 분업(나 1~7 / 팀원 A~I) |
> | `docs/original-parity.md` | 원본↔Unity 전수 대조표 + 원본 위치 색인 |
> | `PROGRESS.md` | 지금 상태와 바로 다음에 할 일 |

## 배경 — 왜 이걸 먼저 하는가

원본은 **로비 ↔ 사냥(90초) ↔ 결과**를 오가는 게임인데, 우리는 **끝없이 이어지는 전투 씬 하나**뿐이다.
이 뼈대가 없어서 임시방편이 쌓였고, 그게 원본과의 차이로 드러났다(2026-09-08 사용자 지적):

- **R키 부활** — 원본에 없다. 원본은 죽으면 `endRun('dead')` → 결과 화면 → 로비(`:1927`).
  부활은 아이템 '최후의 발악' 보유 시 체력 40%로 **자동** 발동뿐(`:1917`).
- **인게임 숫자키 강화(1~5)·빌드 선택(6~7)** — 원본은 로비 탭에서 한다(`:6974`).

둘 다 이번 스프린트에서 **삭제**한다. 자세한 갭 목록은 `docs/original-parity.md` 0·1절.

## 이번 스프린트에서 만드는 것 (= `docs/worksplit.md` 내 담당 1번)

### 1. 씬 상태머신
원본 `state.scene`(`:1465`) 4상태: `lobby` / `run` / `pause` / `result`.
→ **`Core/GameState`로 이미 구현·병합됨.** 이걸 실제로 구동하는 주체(`Systems/RunController`)를 만든다.

### 2. 런 시작 — `startRun` (`:4293`)
- `RunState.Begin(region, mode)` 호출 (제한시간 일반 90초 / 보스 180초, `CONFIG.run` `:695`)
- **필드 정리**: 원본은 `enemies=[] projectiles=[] zones=[] pickups=[]`로 전부 비운다(`:4318`)
- **플레이어 리셋**: 원본 `resetPlayerForRun()`(`:1511`) — 위치 x=220(=2.2유닛)·바닥, 속도 0, facing 1,
  `maxHp = statMaxHp()`로 재계산 후 풀피, 무적 0, 모든 쿨다운 0
- `GameState.Set(GameScene.Run)`

### 3. 런 진행 — `updateRun` (`:4409`)
- `RunState.Tick(dt)`가 true를 돌려주면 `EndRun(timeout)`
- 원본은 게임 루프 한 곳에서 `state.scene !== 'run' || run.over`면 `dt=0`으로 **전부** 멈춘다(`:7169`)

> **설계 결정 — 일시정지는 `Time.timeScale`로 한다.**
> 우리는 컴포넌트마다 `Update()`가 따로 돌아서 원본처럼 한 곳에서 `dt=0`을 못 준다. 그래서 런이
> 아닐 때 `Time.timeScale = 0`으로 물리·`Time.deltaTime`을 통째로 멈춘다 — 원본 `dt=0`과 같은 효과다.
> **덕분에 팀원이 `Enemies/`에 `IsRunning` 가드를 넣지 않아도 된다.**
> `RunController` 자신과 UI만 `unscaledDeltaTime`/`OnGUI`로 돌면 된다.

### 4. 런 종료 — `endRun(reason)` (`:4354`)
종료 사유 3종: `dead`(사망) / `timeout`(시간 초과) / `bossdead`(보스 격파 — 보스가 없으니 이번엔 미사용).
- `RunState.MarkOver()` → `GameState.Set(GameScene.Result)` → `Time.timeScale = 0`
- 결과 화면에 표시할 값: 처치 수 · 획득 골드 · 획득 경험치 (`RunState`가 이미 들고 있음)
- **사망 경로**: `Characters/PlayerHealth.Died` → `Core/CombatEvents.PlayerDied`(신설) → `RunController`가 구독
  (`Systems`가 `Characters`를 직접 참조하지 않게 — asmdef 계층 규칙)

### 5. 일시정지 — ESC (`:1026`, `pauseRun` `:4332` / `resumeRun` `:4346`)
`Core/GameInput`에 `PauseDown`(ESC) 추가. `run` ↔ `pause` 토글.

### 6. 로비 복귀 — `toLobby` (`:4382`)
`GameState.Set(GameScene.Lobby)`. 정식 로비 UI는 **다음 작업(내 2번)** 이므로, 이번엔
`RunController`의 최소 `OnGUI`(사냥 시작 / 결과 확인 / 로비로)로만 흐름을 잇는다.

### 7. 삭제 — 임시방편 제거
- `Characters/PlayerDeathHandler.cs` **삭제**(R키 부활). 이 파일을 참조하는
  `Assets/Editor/BuildPartAScene.cs`와 `PlayerProgressionTests`의 관련 테스트도 같이 정리.
- `Characters/PlayerDebugController.cs`는 **이번엔 남긴다** — 로비 UI(내 2번)가 생길 때 삭제.
  강화/빌드를 살 다른 수단이 아직 없어서 지금 지우면 검증이 막힌다.

### 8. 가상 지역 → 진짜 지역
`Systems/RunProgress`(누적 처치 100 = 가상 지역 레벨)를 `RunState.Region`을 돌려주도록 바꾼다.
**파일을 지우지 말 것** — `Systems/EnemySpawner`(팀원 소유)가 참조 중이라, 내부만 위임으로 바꾸면
팀원 파일을 안 건드리고 동작만 진짜 지역 기준이 된다.

## 이미 만들어져 있는 것 (다시 만들지 말 것)

| 파일 | 내용 |
|---|---|
| `Core/GameState` | 씬 4상태 + `IsRunning` + `Changed` 이벤트 + `Reset()` |
| `Core/RunState` | 지역·남은시간·처치수·살기·획득골드/경험치·`Over`, 90/180초 상수, `Begin`/`Tick`/`RegisterKill`/`RegisterReward` |
| `Core/CombatEvents` | `EnemyKilled` / `ShrineBuffGranted` / `RewardGranted` |
| `Core/IRunResettable` | `ResetForRun()` — **구현체가 아직 없다.** `PlayerHealth`·`CharacterMover2D`·`MageAttack`이 구현하고 `RunController`가 호출하면 된다 |
| `Core/GameInput` | 키 입력 중앙화 — ESC만 추가하면 됨 |

계약 테스트는 `Assets/Tests/PlayMode/Core/RunLifecycleContractTests.cs`(7건) 참고.

## 완료 기준 (DoD)

- [ ] 로비에서 사냥을 시작하면 필드가 초기화되고 플레이어가 리셋된다
- [ ] 90초가 지나면 `timeout`으로 끝나고 결과(처치수·골드·경험치)가 뜬다
- [ ] 죽으면 `dead`로 끝나고 결과 → 로비로 나가진다. **R키 부활이 없다**
- [ ] ESC로 멈추고 다시 ESC로 이어진다. 멈춘 동안 적·투사체·장판이 전부 정지한다
- [ ] `RunProgress.RegionLv`가 `RunState.Region`을 반영한다
- [ ] PlayMode 테스트로 상태 전이·타이머·사망/타임아웃 경로 검증
- [ ] 배치 컴파일 + PlayMode 전체 통과 후 `main` 병합

## 범위 밖 (이번엔 안 만든다)

- 정식 로비 UI·HUD(내 2번) — 이번엔 `OnGUI` 최소 흐름만
- 세이브/로드(내 3번) — 껐다 켜면 초기화되는 상태 유지
- 지역 선택·입장료·9지역 진행도(내 2번 + 팀원 E)
- 보스 스테이지(`bossdead` 경로) — 팀원 D
- 콤보·살기·연쇄처치 등 배수 체인(내 4번)
