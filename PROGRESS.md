# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.


## 지금 상태 (2026-09-09)

**전투 코어 + 체력/피해 + 성장곡선(레벨·골드강화·난이도 스케일링) + 마법사 스킬트리 전체가 `main`에 병합·검증 완료.**
`main`이 유일한 최신 브랜치이고, 미병합 작업은 없다. **PlayMode 61/61 통과.**

지금 게임은 이렇게 동작한다: `Assets/Scenes/CombatCore.unity`를 열면 플레이어(파란 원)가 ←/→ 이동,
**C**로 점프(원본은 C+Space지만 **의도적 편차로 C만**), **Z** 길게 눌러 차지 후 발사한다. 원본 발판 15개
(원웨이, 층간 1.35유닛 — 의도적 편차)를 실제 Physics2D 중력으로 오르내리고, 몹(오니)은 3.6초마다
웨이브 스폰돼 추적/배회하며 접촉 데미지를 준다. 처치하면 골드·경험치가 들어오고 레벨이 오른다.
**6/7** 키로 마법사 빌드(폭발/중력)를 배우면 Z 공격이 바뀌고 **X** 스킬이 열린다.

### 다음 할 일 — 런 사이클

**스펙: [`HANDOFF.md`](HANDOFF.md) / 전체 분업: [`docs/worksplit.md`](docs/worksplit.md) / 원본 대조: [`docs/original-parity.md`](docs/original-parity.md)**

착수 전제(`Core/GameState`·`RunState`·`CombatEvents`·`IRunResettable`)는 **이미 `main`에 있다.**
바로 `Systems/RunController`부터 만들면 된다. 구체적인 순서·완료 기준은 `HANDOFF.md`.

1. `Core/GameInput`에 `PauseDown`(ESC) 추가
2. `Systems/RunController` 신설 — 상태 전이, 90초 타이머, `Time.timeScale` 정지, 필드 정리, 플레이어 리셋
3. `PlayerHealth`·`CharacterMover2D`·`MageAttack`이 `Core/IRunResettable` 구현
4. `Core/CombatEvents`에 `PlayerDied` 추가 → 사망 시 결과 화면
5. **`Characters/PlayerDeathHandler` 삭제**(R키 부활 — 원본에 없음) + `BuildPartAScene`·테스트 정리
6. `Systems/RunProgress`를 `RunState.Region` 위임으로 교체(팀원 파일 `EnemySpawner`는 안 건드림)

**팀원 쪽**: `docs/worksplit.md`의 A~I. 착수 전제가 `main`에 올라가 있으므로 **지금 바로 시작 가능**하고,
A(몹 6종)는 착수 전제와 무관해서 언제든 시작할 수 있다.

## 작업 재개 방법 (인수인계)

```bash
git clone https://github.com/kimJ02/yokai_unity && cd yokai_unity
git checkout main            # 미병합 브랜치 없음 — main만 보면 된다
```

읽는 순서: `CLAUDE.md`(작업 규칙) → 이 파일 → `HANDOFF.md`(지금 스프린트) → `docs/worksplit.md`(분업).

**배치 검증 명령** (Unity 에디터를 반드시 닫고 실행 — 열려 있으면 프로젝트가 잠겨 크래시한다):

```bash
# 컴파일 + 씬 재생성
"/c/Program Files/Unity/Hub/Editor/6000.0.75f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "C:\dev\yokai_unity" -executeMethod YokaiFront.Editor.BuildPartAScene.Build \
  -logFile "C:\dev\yokai_unity\build.log"
```

```bash
# PlayMode 테스트 전체
"/c/Program Files/Unity/Hub/Editor/6000.0.75f1/Editor/Unity.exe" -batchmode -nographics \
  -projectPath "C:\dev\yokai_unity" -runTests -testPlatform PlayMode \
  -testResults "C:\dev\yokai_unity\test_results.xml" -logFile "C:\dev\yokai_unity\test.log"
```

`-executeMethod`는 **네임스페이스 전체 경로**가 필요하다(`YokaiFront.Editor.BuildPartAScene.Build`).
결과 파일명은 `.gitignore`에 걸리는 패턴(`*.log`, `*results*.xml`)을 쓸 것.

## 확인 필요 / 막힌 것

- **사용자 에디터 직접 플레이 확인이 밀려 있다.** 스프린트 2(체력·데미지)·성장곡선·마법사 스킬트리 전부
  배치 컴파일 + PlayMode는 통과했지만, 실제로 플레이해본 것은 그 이전 단계까지다. 지금까지 원본과 다른 점은
  **전부 사용자가 직접 플레이해보고 발견**했다(스폰 위치·이동/점프 디테일·공격 판정·카메라·물리 밀어내기·
  체력 표시·R키 부활). 자동 테스트로는 못 잡는 종류이니 이 항목을 섣불리 지우지 말 것.
- **원격에 stale 브랜치가 남아 있다** — `origin/feature/kill-rewards`, `origin/feature/monster-combat`,
  `origin/feature/player-field`, `origin/feature/run-cycle`. 전부 `main`에 병합돼 고유 커밋이 0개다.
  로컬 브랜치는 2026-09-09에 정리했지만, **원격은 팀원이 쓰고 있을 수 있어 임의로 지우지 않았다** —
  팀원 확인 후 `git push origin --delete <브랜치>`로 정리할 것.
- **캐릭터 컨트롤러는 자체 구현이다.** 초기 스펙은 에셋스토어 컨트롤러 재사용을 전제했지만 배치 환경에서
  인증 없이 받을 수 없어 `Characters/CharacterMover2D.cs`를 직접 짰다. 나중에 실제 에셋으로 바꾸기로 하면
  이 파일 하나만 교체하면 되도록 다른 스크립트와 결합시키지 않았다.

## 로그 (최신이 위)

- **2026-09-09** — **레포 전체 정리 — 낡은 문서·브랜치·죽은 참조 제거 + 인수인계 정비.** 사용자 지시("구버전 내용 싹 없애고, 인수인계 받은 쪽이 바로 재개할 수 있게"). 문서가 9개로 늘면서 **어느 게 최신인지 알 수 없는 상태**가 핵심 문제였다 — 특히 완료된 스프린트 스펙이 루트 `HANDOFF.md`에 그대로 남아 있어서, 새 세션이 읽으면 **이미 폐기된 결정(R키 부활·숫자키 강화)을 현행 스펙으로 오해**하게 돼 있었다.
  - **아카이브**: 완료된 `HANDOFF.md`(성장곡선 검증) → `docs/sprints/03-growth-curve.md`, `docs/sprint2-handoff-split.md` → `docs/sprints/03-growth-curve-worksplit.md`. 아카이브 4개 전부 상단에 "⚠️ 완료된 기록, 현재 스펙 아님" 배너를 달고, **뒤집힌 결정 2건은 표로 명시**(R키 부활·숫자키 강화 → 폐기).
  - **`HANDOFF.md` 신규 작성** — 현재 스프린트(런 사이클) 스펙. 원본 줄 인용·완료 기준(DoD)·범위 밖·이미 만들어진 계약 목록까지.
  - **`docs/ROADMAP.md` 삭제** — `docs/original-parity.md`와 내용이 겹쳐 어느 쪽이 최신인지 헷갈렸고, 2절(현재 구현 상태)·4절(스프린트 순서)은 이미 사실과 달랐다. 고유하게 쓸모 있던 "원본 위치 색인"만 `original-parity.md` 부록으로 옮김.
  - **죽은 참조 제거**: 존재하지 않는 `HANDOFF_sprint2_draft.md`를 가리키던 코드 주석, 이동된 `docs/sprint2-handoff-split.md`를 가리키던 코드/테스트 주석을 전부 현행 경로로 교체.
  - **`.gitignore` 구멍 수정**: `*_results*.xml`만 있어서 실제로 쓰던 `testresults_*.xml` 파일명이 안 걸렸다(매번 수동으로 지우고 있었음). `*results*.xml`로 확장.
  - **브랜치 정리**: `main`에 전부 병합된 로컬 브랜치 5개 삭제. `feature/run-cycle`의 WIP(`Core/IRunResettable`)는 계약 성격이라 `main`에 병합. 원격 stale 브랜치 4개는 팀원이 쓸 수 있어 손대지 않고 "확인 필요"에 남김.
  - **`PROGRESS.md` 재작성**: 완료된 체크리스트 3개·과거 분업 기록·이미 틀린 "확인 필요" 항목(예: "접촉 데미지 미구현" — 실제로는 구현됨)을 걷어내고 **작업 재개 방법(클론→읽는 순서→배치 검증 명령)** 섹션을 추가. 로그는 날짜 기록이라 전부 보존.

- **2026-09-08** — **원본 전수 대조 + 전체 분업안 문서화.** 사용자가 "원본이랑 똑같이 만들어달라고 했는데 지금 게임은 다르다"고 지적 — 확인해보니 맞았다: (a) **R키 부활은 원본에 없다**(죽으면 `endRun('dead')`→결과→로비, `:1927`. 부활은 아이템 '최후의 발악' 보유 시 자동 발동뿐, `:1917`), (b) **강화는 로비에서 하는 것**(ESC는 일시정지 `:1026`, 강화/전문화는 로비 탭 `:6974`)인데 우리는 인게임 숫자키로 대체해뒀다. 둘 다 스프린트 2에서 "런 사이클이 아직 없으니 테스트가 끊기지 않게" 넣은 임시방편이었는데, 그 임시방편이 원본과의 차이로 쌓이고 있었다.
  - **근본 원인**: 원본은 로비↔사냥(90초)↔결과를 오가는 게임인데 우리는 끝없는 전투 씬 하나뿐 — `state.scene`(`:1466`)·런 타이머·결과 정산·세이브가 통째로 없어서 위 두 문제가 파생됐다.
  - 원본 7199줄을 시스템 단위로 다시 훑어 현재 구현(30파일 2636줄)과 전수 대조 → **[`docs/original-parity.md`](docs/original-parity.md)** 작성. 비주얼/렌더링(원본 1734줄)은 사용자 확인대로 대조 제외.
  - 2026-09-08 채팅에서 확정했던 전체 분업(7항목/9항목)이 **어느 파일에도 안 남아 있어 통째로 유실**된 것을 발견. 상대 세션은 채팅을 못 보므로 문서에 없는 분업은 존재하지 않는 것과 같다 → `CLAUDE.md`에 **"분업안 — 작성 위치와 형식"**(필수 9항목: 머리말/착수전제/공유계약/트랙별담당/건드리는·안되는 파일/DoD/확정사항/병합순서/범위밖) 규칙을 신설하고, 그 형식대로 **[`docs/worksplit.md`](docs/worksplit.md)**(단일 living 문서)를 작성. 스프린트마다 새 분업 파일을 만들지 않기로 함.
  - **분업 축**: 나 = 게임 구조·플레이어·메타(런사이클→UI→세이브→배수체인→구슬→아이템/가챠/윤회→메카닉), 팀원 = 적·월드·나머지 캐릭터(몹6종→엘리트→성소→보스→지역스폰→출혈→섬영→드루이드→비기). 씬(`CombatCore.unity`)·`BuildPartAScene.cs` 소유권은 나, 새 프리팹은 팀원 자유.
  - **다음**: 착수 전제 3파일(`GameState`/`RunState`/`CombatEvents`)을 `main`에 올리고 팀원에게 연락 → 런 사이클 착수.

- **2026-09-08** — **`Core/DifficultyScalingConfig.cs`로 스케일링/보상 상수 분리 — `feature/kill-rewards` 브랜치.** 팀원이 `docs/sprint2-handoff-split.md`에 "0. 착수 전 필독"을 추가해 지시: 원본(`project_test.html`) 밸런스 자체가 완전히 정리된 게 아니라서(예: 아무 데서도 안 읽는 죽은 설정값 `CONFIG.souls`/`CONFIG.scale.soulGrow`를 실제로 발견, `:701`,`:727`) 나중에 직접 세밀하게 조정할 가능성이 높다는 판단 — 그때 로직 코드를 안 뒤지고 숫자만 한 파일에서 바꾸게 하려는 목적. 값 자체는 변경 없음, 위치만 이동.
  - `Core/DifficultyScalingConfig.cs` 신설 — `KillsPerRegionLevel`(100)/`HpPerRegion`(2.15)/`DmgPerRegion`(1.4)/`RewardGrow`(1.42)/`OniBaseHp`(38)/`OniBaseDmg`(13)/`OniBaseExp`(8)/`OniGoldMin,Max`(5,10)/`GoldDropChance`(0.75) 상수 + `ScaledHp()`/`ScaledDmg()`/`RewardMultiplier()` 계산 함수. 문서에 있는 코드 그대로 사용.
  - `Systems/RunProgress.cs`에서 자체 `KillsPerRegionLevel` 상수 제거하고 `DifficultyScalingConfig.KillsPerRegionLevel` 참조로 교체.
  - `Systems/EnemySpawner.cs`에 하드코딩했던 필드 9개(`baseEnemyHp`/`baseEnemyDamage`/`hpPerRegion`/`dmgPerRegion`/`rewardGrow`/`baseExp`/`goldDropChance`/`goldMin`/`goldMax`)를 전부 제거하고 `ApplyRegionScaling()`/`HandleEnemyDied()`가 `DifficultyScalingConfig`를 직접 호출하도록 정리 — 로직 자체(SetMaxHp 경유, Died 구독, RegisterKill/AddExp/AddGold 순서)는 그대로.
  - **테스트도 매직넘버 제거**: `RunProgressAndRewardsTests.cs`의 기대값 계산(hp/dmg 스케일링, EXP)을 리터럴 대신 `DifficultyScalingConfig`의 상수·함수를 직접 참조하도록 갱신 — 나중에 Config 값이 바뀌어도 테스트가 안 깨지고 같이 따라간다.
  - **배치 컴파일·PlayMode 재검증은 이 세션이 직접 못 함 — 사용자 확인 필요.** 확인되면 `main`에 병합.

- **2026-09-08** — **트랙 A(처치 보상 + 난이도 스케일링) 구현 — `feature/kill-rewards` 브랜치.** `docs/sprint2-handoff-split.md` 트랙 A 스펙 그대로 반영, 0단계에서 준비된 `EnemyHealth.SetMaxHp()`/`Died`·`ProfileService.Current`를 그대로 사용.
  - `Systems/RunProgress.cs` 신설 — 가상 지역 레벨 정적 카운터(`FieldBounds`와 같은 패턴). `TotalKills`/`RegionLv`(=1+TotalKills/100, 원본 regionKillTarget project_test.html:699)/`RegisterKill()`/`Reset()`.
  - `Systems/EnemySpawner.cs`에 스폰 직후 훅 2개 추가: `ApplyRegionScaling()`이 `EnemyHealth.SetMaxHp(38×2.15^(RegionLv-1))`(필드 직접 대입 대신 반드시 SetMaxHp 경유 — CurrentHp 미갱신 함정 회피) + `EnemyMove.attackPower = 13×1.4^(RegionLv-1)`을 계산해 적용하고 `EnemyHealth.Died += HandleEnemyDied` 구독. `HandleEnemyDied`가 `RunProgress.RegisterKill()` + `ProfileService.Current.AddExp(round(8×1.42^(RegionLv-1)))`(항상 지급) + 75% 확률로 `AddGold(round(randInt(5,10)×같은 배율))` 호출(원본 killEnemy(), project_test.html:1793).
  - **건드리지 않은 것(계약대로)**: `Core/PlayerProfile.cs`(메서드 호출만, 필드·이름 변경 없음), `Characters/` 전체, `Enemies/EnemyHealth.cs`·`Enemies/EnemyMove.cs`(0단계에서 이미 준비된 훅/필드를 호출만 함 — 소스 수정 없음).
  - **테스트 신규 4건**(`Assets/Tests/PlayMode/Systems/RunProgressAndRewardsTests.cs`): `RegionLv` 100마리 단위 증가 검증, 스폰 시 hp/dmg 스케일링 공식 검증, 처치 시 EXP 결정적 지급 검증, 40마리 연속 처치로 골드 확률 드랍 통계적 검증(0드랍 확률 0.25^40≈0).
  - **컴파일 에러 1건 + 테스트 실패 1건 발견·수정**: 테스트 파일에 `using YokaiFront.Systems;` 누락으로 `EnemySpawner` 타입을 못 찾은 컴파일 에러(사용자가 Safe Mode 진입 후 에러 메시지 전달해 확인) → using 추가로 해결. `SpawnWave_ScalesEnemyStatsByRegionLevel` 테스트가 "1마리만 스폰" 기대했는데 2마리 나옴 → `EnemySpawner.waveTimer` 기본값이 0이라 수동 `SpawnWave()` 호출과 별개로 그 프레임의 자동 `Update()`가 웨이브를 한 번 더 터뜨린 것(프로덕션 버그 아님, 테스트가 스포너를 새로 만들 때 흔히 걸리는 함정) — 테스트 헬퍼에서 `waveTimer`를 리플렉션으로 크게 밀어 자동 웨이브만 차단(컴포넌트 자체를 끄면 `aliveMonsters.RemoveAll(null)` 같은 다른 Update 로직까지 죽어서 다른 테스트가 깨짐).
  - **아직 못 한 것 — 사용자 확인 필요**: 이 세션은 이 컴퓨터에서 git/Unity 배치모드를 직접 실행할 수 없어(파일 읽기/쓰기만 가능) 배치 컴파일·PlayMode 테스트 실행·커밋은 전부 사용자가 GitHub Desktop/에디터로 직접 해야 한다. **팀원이 `docs/sprint2-handoff-split.md`에 "0. 착수 전 필독"을 추가해 스케일링/보상 상수를 `Core/DifficultyScalingConfig.cs`로 분리하라고 지시 — 이 `main` 병합 끝나는 대로 반영 예정(값 자체는 변경 없음, 위치만 이동).**
- **2026-09-08** — **트랙 B 완료: 레벨업 + 골드 강화 5종 + 파생 스탯 적용 (PlayMode 40/40).** `feature/level-gold-upgrades` 브랜치에서 작업, 착수 전 팀원(트랙 A) 커밋이 아직 없음을 `git fetch`로 확인.
  - **레벨업**: `PlayerProfile.AddExp()`가 원본 `gainExpMeta()`(`:1440`)를 그대로 옮김 — 초과분 이월, 한 번의 지급으로 여러 레벨 가능, 실제로 레벨이 오를 때만 `LeveledUp` 이벤트 발생.
  - **골드 강화**: `Core/GoldUpgrade.cs`(비용표, 원본 `upCost` `:1268`) + `PlayerProfile.TryBuyUpgrade()`. `Core/PlayerStatCalculator.cs`(파생 스탯 5종, 원본 `:1284~1288`).
  - **원본을 다시 읽어 확인한 비직관적 동작**(그대로 구현): 골드 강화(hp)를 사도 **즉시 반영되지 않는다** — 원본은 레벨업 때만(`:1850`) 최대체력을 재계산하고 풀피로 채운다. `PlayerHealth`가 `PlayerProfile.LeveledUp`을 구독해서 그 순간에만 재계산하도록 만듦(매 프레임 폴링이었으면 원본과 다른 동작이 될 뻔함 — 구현 전에 원본을 직접 실행 로직까지 추적해서 확인).
  - **스탯 연결**: `MageAttack`(공격력→`baseDamage`, 공격속도→`cooldown`, 매 프레임 프로필에서 재계산해서 자기 참조 누적 없음), `MageProjectile`(치명타 확률을 `PlayerStatCalculator`에서), `CharacterMover2D`(이동속도 배율). `Combat.DamageCalculator`에 치명타 확률을 인자로 받는 오버로드 추가(기존 호출부 하위호환 유지).
  - **확정 사항 구현**: `PlayerDeathHandler`(HP0 → 정지, `Revive()` 공개 메서드로 R키 처리와 테스트 양쪽에서 호출), `PlayerDebugController`(숫자키 1~5 구매 + `OnGUI` 디버그 표시 — 골드/레벨/강화단계/다음비용).
  - **테스트**: 신규 10건(레벨업 공식·이월·이벤트, 강화 비용 공식·구매 성공실패, 파생 스탯 공식, 치명타 확률 0/1 경계, 레벨업시에만 체력반영 통합, MageAttack 스탯반영 통합, 사망→정지→부활 통합). `Core.ProfileService`에 정적 싱글턴 오염 방지용 `Reset()` 추가하고 모든 신규 테스트가 `[SetUp]`/`[TearDown]`에서 호출(EnemyHealth 테스트 오염을 이미 겪은 것과 같은 함정 사전 차단).
  - **막혔던 것**: 1건 실패 — `CharacterMover2D`가 `Collider2D`를 요구하는데 테스트가 안 붙여서 `CheckGrounded()`에서 NRE. 테스트 버그였고 콜라이더 추가로 해결. 이동속도 통합 테스트 하나는 애초에 `Assert.Pass()`로 항상 통과하는 가짜 테스트라 삭제(Input 시뮬레이션이 안 돼서 실제 검증이 불가능했음 — 없는 것보다 정직하게 빼는 게 낫다고 판단).
  - 에디터 직접 플레이 확인, 팀원 트랙 A 완료는 아직.
- **2026-09-08** — **스프린트 2 초안 접수 → 검토 → 팀 분업 확정.** 팀원이 `HANDOFF_sprint2_draft.md`("성장곡선 검증" — 체력/피격무적·처치보상·레벨EXP·골드강화5종·가상지역레벨·검증방법)를 `origin/main`에 push.
  - **겹치는 작업 발견**: 초안 "1. 체력 시스템" 항목이 바로 아래 로그(같은 날짜, 원본 전수 분석 로드맵)에서 이미 `feature/health-damage`에 구현·검증까지 끝낸 것과 사실상 동일 범위. 이 사실을 확인하고 사용자에게 알림.
  - 초안이 "확인 필요"로 남긴 3건을 논의해 확정: HP 0 처리(정지+R키 재시작 — 원안의 "로그만"은 죽을 때마다 에디터 재시작해야 해서 검증 자체를 방해한다고 판단해 변경), 강화 입력(숫자키+`OnGUI` 디버그 표시 — 표시가 없으면 "체감 벽" 검증이 불가능해서 원안에 추가), `regionLv` 트리거(누적 처치 100마리당 1 — 원본 `regionKillTarget`과 가장 가까움, 시간/레벨 기준은 원본에 없는 설계라 기각).
  - **트랙 분업 확정**: 트랙 A(팀원, Enemies/Systems 도메인) = 처치 보상 + 난이도 스케일링. 트랙 B(나, Core/Characters 도메인) = 레벨업 + 골드 강화 + 스탯 적용. 근거: 팀원은 최근(`d27af46`) `Enemy_Oni.prefab`/물리 세팅을 직접 다뤘고, 나는 `PlayerHealth`/`MageAttack`/`CharacterMover2D`를 이번 세션에서 직접 만들어 가장 잘 앎. 상세 분업표는 `docs/sprint2-handoff-split.md`.
  - **공유 계약**: `Core.PlayerProfile`(`level, exp, gold, spUsed, upgrades{atk,hp,ms,atkSpeed,crit}`, `as`는 C# 예약어라 `atkSpeed`로 대체) + `Core.ProfileService.Current` 정적 접근자. 트랙 A는 `AddGold()`/`AddExp()` 메서드만 호출(필드 직접 증가 금지 — 레벨업 판정은 트랙 B 소유). `EnemyHealth`에 스탯 배율 setter(`SetMaxHp` 등) 필요 — `Awake()`가 이미 돈 뒤 `maxHp` 필드만 바꾸면 `CurrentHp`엔 반영 안 되는 이 프로젝트 단골 함정 주의(RequireComponent 자동보충·AddComponent Awake타이밍과 같은 종류).
  - 이어서 0단계(`feature/health-damage` 병합)를 바로 진행 — 아래 이 로그 항목 참고.
- **2026-09-03** — **원본 전수 분석 → 포팅 로드맵 작성 + 스프린트 2(체력&데미지) 확정·구현.** 사용자가 "프로토타입 전체적으로 분석하고 어떻게 업데이트할지 문서로 작성"을 요청.
  - `docs/ROADMAP.md` 신설 — 원본 7199줄을 전 구간 훑어 시스템 전수 목록화(씬 4상태 머신 / 런 루프 / 전투 코어 / 캐릭터 4종 / 적 7종+보스 / 성장 3계층 / 윤회·가챠 / 세이브), 현재 Unity 구현과의 갭, 의존성 기반 권장 스프린트 순서를 정리. *(→ 2026-09-09에 `docs/original-parity.md`로 통합되고 삭제됨)*
  - **검증**: 문서에 쓴 원본 인용 68개를 전부 실제 파일과 재대조해 일치 확인. 2절(현재 구현 상태) 주장도 코드로 검증(스크립트 10개, `UI` 폴더 없음, `Destroy()` 직접 호출 2곳, `IDamageable`/`GameInput`/`PlayerProfile` 미구현, `PlayerAttack` 씬 미사용, 보스 코드 전무). 작성 중 오류 1건 발견·수정(아이템 개수를 26종으로 잘못 적었으나 실제 35종 — 등급별 9/14/8/4).
  - **핵심 결론**: 거의 모든 시스템(보상·런 종료·원소·엘리트/보스 hp배수·아이템 스탯)이 체력에 의존 → 순서가 사실상 강제됨. 체력/데미지 → 런 사이클 → 세이브/로비까지 해야 "게임 루프"가 처음 성립.
  - 사용자가 이 순서를 승인하고 1번부터 착수 지시 → **스프린트 2(체력 & 데미지) 확정 후 구현.** 신설: `Core/IDamageable`(피격 인터페이스) · `Combat/DamageCalculator`(치명타 10%·배수 1.7·±10% 난수·최소 1) · `Enemies/EnemyHealth`(체력 38·넉백·피격 플래시) · `Characters/PlayerHealth`(체력 100·무적 0.9초·피격 반동). 공격 스크립트를 `Destroy()` 직접 호출 → `IDamageable.TakeDamage()`로 전환, `EnemyMove.TryAttack()` 빈 스텁 → 실제 접촉 데미지.
  - **원본을 다시 읽어 확인한 비대칭 2건**(그대로 구현): ① 적 넉백은 AI 속도에 *더해지는* 별도 성분 + 초당 9배율 감쇠(`:4021`,`:4056`) — 그냥 속도에 넣으면 `FixedUpdate`가 다음 프레임에 지운다. ② 반면 **플레이어 수평 넉백은 유지하면 안 된다** — 원본도 다음 프레임 이동 입력이 vx를 덮어써서(`:1548`, 피격 중 이동잠금 없음) 사실상 1프레임짜리다. 우리 `CharacterMover2D`도 vx를 덮어쓰므로 아무 처리 안 하는 게 원본과 같음. 수직 튕김만 남는다.
  - **원본과 어긋나 있던 것 하나 같이 수정**: 스폰 보호 중 적이 계속 움직이고 접촉 데미지까지 줬는데, 원본은 "상호 무적 — 움직이지도, 때리지도, 맞지도 않는다"(`:4022~4023`)로 갱신 자체를 건너뛴다. `EnemyMove`가 보호 중엔 AI·접촉·수평이동을 전부 스킵하도록 수정.
  - **테스트**: 신규 10건 추가(적 체력/스폰무적/넉백/투사체 통합/데미지 공식, 플레이어 체력/무적창/반동/사망이벤트/접촉). 기존 4건은 "한 방에 파괴"를 전제로 짜여 있어 새 동작에 맞게 갱신 — 각 테스트가 원래 검증하던 것(사거리·태그 필터, 관통 횟수, 스폰 무적)은 그대로 두고 기대값만 "파괴됨"→"피해 입음"으로 바꿨다.
  - **막혔던 것**: 1차 실행에서 8건 실패. 그중 `EnemyMove_Chases...`는 단독 실행하면 통과 → **테스트 오염**이었다(내 신규 테스트가 중력 켜진 적을 같은 좌표에 남겨서 다른 테스트의 몹을 물리로 밀어냄). 프로덕션 버그 아님. 신규 테스트에 `[TearDown]` 정리 + 중력 off + 스폰 무적 해제를 넣어 해결. 접촉 테스트는 Player(6)/Enemy(7) 레이어를 실제 게임과 동일하게 지정해야 겹친 상태가 유지된다(팀원이 만든 레이어 분리 덕).
  - PlayMode 30/30. `feature/health-damage` 브랜치에 커밋(`c4518ad`) — 당시엔 `main` 미병합. CLAUDE.md "문서 구조" 규칙대로 HANDOFF.md를 `docs/sprints/01-combat-core.md`로 옮기고 이 스펙으로 교체하려 했으나, 같은 날 팀원 초안이 도착해 스프린트 2 범위 자체가 위(9월8일 로그) 항목처럼 재확정됨.
- **2026-09-08** — **0단계 완료: `feature/health-damage` → `main` 병합.** 위 스프린트 2 구현을 그대로 `main`에 반영(`--no-ff` 병합). PROGRESS.md만 충돌(로그 두 버전 병합, "다음 할 일" 새로 씀) — 코드 파일은 전부 깨끗하게 병합됨. 다음은 `Core/PlayerProfile` 스캐폴딩.
- **2026-08-29** — **버그 수정: 플레이어·몬스터가 서로 겹치지 못하고 물리로 밀어내는 문제.** 사용자가 리팩터링 이후분을 직접 플레이해보다 발견해 지적. 원본(`project_test.html:4148` `updateEnemies()`)은 물리엔진이 없어 `rectsOverlap()`로 접촉만 검사해 `damagePlayer()`를 부르고 끝 — 겹쳐도 밀어내는 로직 자체가 없다. 반면 Unity 쪽은 발판 착지를 위해 Player·Enemy 둘 다 `Rigidbody2D`(Dynamic) + 막힌(non-trigger) `CircleCollider2D`를 쓰는데, 같은 물리 레이어(`Default`, 0번)라 겹치는 순간 Physics2D가 자동으로 밀어냈다 — 원본엔 없는, 실제 물리엔진 도입(2025-08-25) 부작용이었음(그동안 안 걸리다가 이번에 처음 지적됨).
  - 콜라이더를 트리거로 바꾸면 발판 충돌까지 깨지므로, 새 물리 레이어 `Player`(6)·`Enemy`(7) 신설(`CLAUDE.md` "번호/이름 예약표" 갱신) 후 Player 오브젝트·`Enemy_Oni` 프리팹에 각각 배정, **Physics2D Layer Collision Matrix에서 Player×Enemy 충돌만 비활성화**(Player×Ground, Enemy×Ground는 유지)하는 방식으로 해결.
  - 접촉 데미지 판정은 원래도 물리 충돌과 무관하게 `EnemyMove.OverlapsTarget()`의 수동 거리 계산으로 처리되고 있어 코드 수정은 불필요 — 순수 프로젝트 세팅(TagManager/Physics2DSettings) 변경만으로 해결됨.
  - 사용자가 Unity 에디터에서 직접 레이어·콜리전 매트릭스 설정 후 플레이로 재확인 완료. 코드 변경이 없어 컴파일/PlayMode 테스트에는 영향 없음(재실행 안 함 — 다음 코드 변경 시 같이 재검증).
  - `main`에서 바로 진행(팀원이 동시에 이 레포를 안 건드리고 있음을 확인함, CLAUDE.md 협업규칙 3번 예외 — 이미 병합된 기능의 소규모 후속 수정).
- **2026-08-26** — **대규모 리팩터링 완료 — 폴더/네임스페이스/asmdef 분리 + `Monster`→`Enemy` 리네임.** 사용자가 "리펙터링 ㄱㄱㄱ"로 승인, CLAUDE.md "대규모 리팩터링 절차"(미병합 브랜치 0개 확인 → 진행 중 선언 → 단일 커밋 → 배치 검증 → 병합)를 그대로 따라 진행. 착수 전 미병합 브랜치 0개 확인(`feature/monster-combat`/`feature/player-field` 둘 다 `main` 대비 unique 커밋 없음).
  - **폴더 = 네임스페이스 = asmdef 이름 일치**: `Assets/Scripts/`를 `Core/World/Combat/Characters/Enemies/Systems` 6개 폴더로 분리(전부 `git mv`로 이동해 히스토리 보존), 각각 `YokaiFront.<도메인>` 네임스페이스 + 같은 이름의 asmdef 신설. 계층(`Core`←`World`/`Combat`←`Characters`/`Enemies`←`Systems`)대로 `references` 설정, 같은 층(`Characters`↔`Enemies`)은 서로 참조 안 함. 기존 단일 `YokaiFront.Runtime.asmdef`는 삭제.
  - **`Core/ISpawnProtectable.cs` 신설** — 리팩터 도중 실제 계층 위반을 발견: `MageProjectile`(`Characters`)·`PlayerAttack`(`Combat`)이 `MonsterMove`(`Enemies` 예정) 타입을 직접 `GetComponent`하고 있었다. `Enemies`를 모르는 도메인이 스폰 무적 상태만 물을 수 있게 `Core`에 인터페이스를 만들고 `EnemyMove`가 구현, 공격 스크립트는 `GetComponent<ISpawnProtectable>()`로 전환.
  - **`Monster`→`Enemy` 리네임**: `MonsterMove.cs`→`EnemyMove.cs`, `MonsterSpawner.cs`→`EnemySpawner.cs`(클래스명도 동일하게), `Monster.prefab`→`Enemy_Oni.prefab`(내부 `m_Name`도 갱신). `BuildPartAScene.cs`의 프리팹 경로 상수·타입 참조 전부 갱신.
  - `Assets/Editor/BuildPartAScene.cs`도 `YokaiFront.Editor` 네임스페이스로 감쌈 — **배치모드 `-executeMethod`는 이제 `YokaiFront.Editor.BuildPartAScene.Build`처럼 전체 경로가 필요**(옛 짧은 이름은 "class could not be found"로 실패).
  - `Assets/Tests/PlayMode/`도 도메인별 하위 폴더(`Combat/Characters/Enemies/`)로 재배치, `MonsterSpawnAndMoveTests.cs`→`Enemies/EnemySpawnAndMoveTests.cs`로 리네임하며 내부 타입 참조 갱신, 전부 `YokaiFront.Tests.PlayMode` 네임스페이스로 감쌈. `YokaiFront.PlayModeTests.asmdef`의 참조를 옛 `YokaiFront.Runtime` 대신 새 6개 asmdef로 교체.
  - **1차 배치 검증에서 실제 컴파일 에러 발견**: `EnemySpawnAndMoveTests.cs`에 `using YokaiFront.Enemies;`를 빠뜨려서 `EnemyMove`를 못 찾음(CS0246) — 추가하고 재검증. 두 번째 시도에선 컴파일은 통과했지만 `-executeMethod BuildPartAScene.Build`(옛 이름)가 네임스페이스 이동 때문에 실패 — `YokaiFront.Editor.BuildPartAScene.Build`로 고쳐서 씬 재생성 성공.
  - 최종 배치 컴파일 + PlayMode 테스트 전체 재실행 → **20/20 통과**(리팩터 전과 동일한 개수, 회귀 없음).
  - ScriptableObject 데이터 전환·`GameInput` 중앙화는 CLAUDE.md 규칙상 **이번 리팩터 범위 밖**(SO는 "2번째 무기/적 종류를 만들기 전" 시점으로, GameInput은 새 코드부터 — 둘 다 명시적으로 유보).
  - `CLAUDE.md`에 "문서 3종 동기화" 규칙 신설(사용자가 세 문서 갱신이 잘 안 지켜지는 것 같다고 지적) + "지금 프로젝트 상태"를 리팩터 완료 상태로 갱신.
  - `origin/main`에 이 리팩터 커밋 push까지 완료.
- **2026-08-26** — **"만들어진 것 전체를 원본과 대조" 전수 감사 — 플레이어 이동/점프 디테일 4건 발견해 이식.** 사용자가 지금까지 구현된 전체를 원본과 다시 대조해달라고 요청 → 원본 플레이어 업데이트 루프(`bowFire` 호출부 포함 3500~3560행)를 처음부터 재검토:
  - 원본에 `bladeMove()`(관성 가속 이동)가 있어서 "이것도 빠졌나" 싶었는데, 확인해보니 **블레이드 캐릭터 전용**(`if (meta.weapon==='blade') updateBladePlayer()`로 분기)이고 활(마법사, 우리 스코프)은 원래 즉시-속도 이동이 맞다 — 헛다리 아니었음, 기존 구현 그대로 유지.
  - **코요테 타임(0.10초)·점프 입력 버퍼(0.13초)** 완전히 빠져있었음(`CharacterMover2D`에 추가, 원본 `COYOTE_T`/`INPUT_BUF_T`) — 이전엔 "grounded인 바로 그 프레임에 keydown"만 인정해서 원본보다 조작감이 빡빡했다.
  - **차지 중 이동속도 50% 감소**(원본 `chargeSlow`) 완전히 빠져있었음 — `CharacterMover2D.SpeedMultiplier`를 추가하고 `MageAttack`이 매 프레임 갱신하도록 연결. 처음엔 `charging` 플래그를 프레임 끝에서 읽어서 테스트가 실패했는데(리플렉션으로 charging=true만 주입하면 같은 프레임에서 "방금 뗐다"로 오인돼 즉시 발사되며 charging이 다시 꺼짐), 원본처럼 프레임 시작 시점 상태를 기준으로 삼도록 순서를 바꿔서 해결 — 실제 게임에서도 더 정확한 순서.
  - **낙하 종단속도 15유닛/s**(원본 1500px/s) 상한 없었음 — 실제 물리 중력만 쓰면 최상단 발판(5.4유닛)에서 떨어질 때 이론상 ≈16.8유닛/s까지 나서 원본보다 세게 떨어졌다. `CharacterMover2D`/`MonsterMove` 둘 다 추가.
  - **필드 X 경계 여백**: 플레이어 0.24유닛(원본 24px), 몬스터 0.3유닛(원본 30px, 서로 다른 값) — 몬스터 쪽은 경계 clamp 자체가 아예 없어서 배회 중 필드 밖으로 나갈 수 있었다.
  - PlayMode 테스트 5건 추가(`MovementFidelityTests.cs`) — 코요테 타임 안/밖, 차지 감속, 종단속도, 경계 여백. 종단속도 테스트는 처음에 실패했는데, 물리 스텝이 클램프보다 한 스텝 늦게 반영돼 매 스텝 중력만큼(26×fixedDeltaTime≈0.52유닛/s) 일시 초과하는 게 정상이라 허용치를 그에 맞게 조정 — 실제 게임에선 감지 불가능한 서브프레임 오차. 전체 **20/20 통과**.
  - `HANDOFF.md` 1번에 상세 기록. 아직 에디터로 직접 확인 전, `origin`에도 push 안 함(로컬에 8개 커밋 쌓임 — 플레이 확인되면 한 번에 push 권장).
- **2026-08-26** — **카메라 크기를 원본 캔버스 비율(1280×720px)에 맞게 재계산.** 사용자가 "원본은 맵이 크고 넓직한데 여긴 너무 작아 보인다, 원본과 맵을 똑같이 만들어달라"고 지적 — 확인해보니 카메라 `orthographicSize=5.5`가 원본 캔버스 크기랑 아무 관계 없이 "발판이 다 보이는 정도"로 임의로 잡은 값이었다. 원본은 캔버스가 1280×720px 고정이라 항상 맵(2600px)의 49%만 화면에 보이는데(나머지는 스크롤로 드러남), 이전 값은 맵의 75%(19.6/26유닛)를 한 번에 보여주고 있었다 — 원본보다 훨씬 더 넓게 보여줘서 오히려 "안 커 보인다"는 역설이 생긴 것.
  - 100px=1유닛 규칙을 카메라에도 그대로 적용: 세로 절반 크기 = 720px÷100÷2 = 3.6유닛이 원본 그대로의 기준값. 다만 발판 층간 간격을 넓힌 우리 필드(사용자가 이미 명시적으로 요청한 편차, HANDOFF.md 1번)에 이 값을 그대로 쓰면 최상단 발판(5.40유닛) 위에 선 캐릭터 머리가 화면 위로 살짝 잘린다 — 그래서 하단 여백(바닥 아래 1유닛)은 원본 그대로 유지하고, 상단만 우리 발판 높이 기준으로 재계산해서 `orthographicSize=3.84`(원본 3.6에서 소폭 확대), 카메라 Y=2.84로 최종 확정.
  - 가로세로 비율은 유지돼 필드/화면 비(26/12.8≈2.03)가 원본(2600/1280≈2.03)과 일치 — 스크롤되는 느낌까지 원본과 같아짐.
  - `BuildPartAScene.cs`만 수정(씬 재생성), 코드 로직 변경 없어 PlayMode 테스트는 그대로 **15/15 통과**.
  - `HANDOFF.md` 1번에 재개정 기록. 아직 에디터로 직접 확인 전, `origin`에도 push 안 함.
- **2026-08-26** — **플레이어 기본공격(마법사) 사거리 재검증 + 판정 크기를 원본과 일치시킴.** 사용자가 "공격이 맵 끝까지 간다, 원본은 사정거리가 있는 걸로 안다"고 지적 → 원본 `bowFire()`와 화살 업데이트 루프(3595~3690행)를 다시 정밀 대조:
  - **사거리(life) 자체는 실제로 원본과 일치했다** — `life = B.range/B.speed`(880/1080≈0.8148초, 차지와 무관하게 고정)를 새 PlayMode 테스트(`Projectile_DespawnsAfterFixedLife_NotOnFieldEdge`)로 직접 검증: 예상 수명 전엔 살아있고, 8.8~11.4유닛(필드 26유닛의 절반 이하)만 이동한 뒤 정확히 그 시점에 사라짐. **버그 아님.** 원본 자체가 캔버스 1280px 중 사거리 880px(68.75%)라 화면 대부분을 가로지르는 게 원래 그렇게 설계된 것 — "맵 끝까지 가는 것처럼 보인다"는 원본과 같은 체감.
  - **진짜 빠져있던 것을 찾음 — 판정(히트박스) 크기**: 원본은 `rectsOverlap(x-20*size, y-15*size, 40*size, 30*size, ...)` — 40×30px(0.4×0.3유닛) 직사각형이고 `size=1+chargeK*0.9`로 차지할수록 커진다. 기존 구현은 반지름 0.5(지름 1유닛) 고정 원이라 원본보다 훨씬 크고 차지에 반응하지 않았다. `MageProjectile`을 `BoxCollider2D`(0.4×0.3, 차지 시 최대 0.76×0.57)로 교체, `MageAttack`도 `sizeMul=1+chargeK*0.9` 공식을 그대로 전달하도록 수정. 판정과 시각(스프라이트)을 자식 오브젝트로 분리해서 스케일 이중 곱 버그도 같이 예방.
  - PlayMode 테스트 1건 추가, 전체 **15/15 통과**.
  - **확인 필요로 남긴 것**: 원본은 순수 수직 조준(위/아래만) 시 투사체 스폰 X 오프셋이 캐릭터의 "마지막 좌우 방향"(`p.facing`, 이동뿐 아니라 대각선 조준으로도 갱신됨)을 따르는데, 우리 `CharacterMover2D.Facing`은 이동 입력으로만 갱신돼서 순수 수직 조준 직후의 스폰 위치가 미묘하게 다를 수 있다(좌우 0.52유닛 차이, 코스메틱 수준). 지금은 안 고침 — 필요하면 나중에.
  - 아직 에디터로 직접 플레이 확인 전, `origin`에도 push 안 함.
- **2026-08-26** — **몬스터가 발판 스폰 포인트를 골라도 실제로는 바닥에만 나던 문제 + 이동 AI를 원본 그대로 재구현.** 사용자가 "왜 적이 바닥 축에서 스폰되냐, 발판 위에도 소환된다며 거짓말했냐, 제대로 작업해봐"라고 지적 — 확인해보니 실제로 X좌표만 발판 위치에서 빌리고 Y는 항상 GroundY로 고정해뒀던 게 맞았다(발판에 스폰된다는 말과 실제 동작이 달랐음). 사용자가 이어서 "이건 프로토타입이 아니라 실제 구현이니 원본과 세세한 부분까지 그대로 만들어야 한다, 단순화는 나중에 지시하겠다"고 명확히 함 — 원본 `updateEnemies()`를 처음부터 끝까지 다시 읽고 몹 관련 물리/AI를 전부 이식:
  - **몹도 실제 중력을 받는다**(`e.vy += gravity*dt`) — `Monster.prefab`에 `Rigidbody2D` 추가, `MonsterMove`가 `transform.position +=` 대신 물리 기반 이동으로 전환. (막혔던 지점: `[RequireComponent(typeof(Rigidbody2D))]`를 붙이면 프리팹 파일에 실제로 없어도 로드 시점에 엔진이 메모리상으로만 자동 보충해서 "이미 있음" 체크가 거짓으로 통과 — 항상 로드→설정→저장하도록 `EnsureMonsterPrefabPhysics()` 수정.)
  - **스폰 위치가 실제 발판 높이를 쓴다** — 발판 스폰 포인트는 `FieldLayout.PlatformLandingY()`(발판 윗면+몹 반지름)로 스폰, 이제 물리로 그 위에 실제로 선다.
  - **추적/배회 AI**: 원본 오니 분기 그대로 — 플레이어가 추적범위(300px=3유닛) 안이면 방향만 맞추고 계속 걷는다(도달해도 안 멈춤). 범위 밖이면 1.5~3.5초마다 무작위로 방향을 바꾸며 배회. 이전엔 항상 무조건 플레이어를 향해 걷다가 접촉 시 멈추는, 원본에 없는 동작이었음.
  - **발판 가장자리 반전**(원본 주석 "발판 위 몹은 가장자리에서 되돌아간다 — 내려오지 않는다"): 발판 위에서 정지 중이고 가장자리 14px 안쪽이면 그 자리에 고정하고 안쪽으로 방향 반전. 몹이 실제로 발판 위에 계속 남아있게 됨(전 버전은 이 로직이 없어서 그냥 물리로 굴러떨어지게 뒀었는데, 원본과 다른 동작이었음).
  - **접촉 데미지**: 원본은 매 프레임 겹침 판정 + 무쿨다운(반복 방지는 플레이어 무적시간이 담당) — `contactDistance`로 멈춰서 쿨다운 도는 방식을 걷어내고 겹치는 동안 매 프레임 호출로 교체(실제 데미지는 Health 없어서 여전히 스텁).
  - `FieldLayout.cs`에 `PlatformLandingY`/`PlatformLeftX`/`PlatformRightX` 헬퍼 추가(스포너·이동 AI가 공유).
  - PlayMode 테스트 재정비: 발판 착지 물리 검증, 가장자리 반전 검증(신규), 추적범위 안/밖 방향 검증을 우연히 기본값과 맞아떨어지던 약한 테스트에서 실제로 반대 방향 이동을 요구하는 강한 테스트로 교체. **14/14 전체 통과.**
  - `HANDOFF.md` 2번을 실제 구현 상태로 갱신, "프로토타입처럼 단순화하지 않는다"는 작업 방침을 `CLAUDE.md`에도 기록.
  - 아직 에디터로 직접 플레이 확인 전, `origin`에도 push 안 함.
- **2026-08-26** — **스폰/히트 로직을 원본과 다시 대조해서 빠진 부분 보강.** 사용자가 직접 플레이해보고 "적 스폰 로직이 이상하다, 원본과 너무 다르다"고 지적 → 원본 `spawnWave()`/`dealDamage()`를 다시 읽어서 확인:
  - **스폰 위치**: 필드 전체 균등 랜덤이었던 걸 원본처럼 "발판 15개 중심 + 바닥 그리드 6곳" 총 21개의 정해진 스폰 포인트 중 하나를 골라 그 폭 안에서 흔드는 방식으로 교체(`MonsterSpawner.PickSpawnPointX`). 발판 좌표 데이터를 `BuildPartAScene.cs`와 중복으로 안 두려고 `FieldLayout.cs`로 공용화.
  - **웨이브 내 간격 체크**: 원본은 `spawnWave()` 호출마다 `placed=[]`를 새로 시작해서 이전 웨이브 몹과는 안 겹쳐도 되는데, 지금까진 전체 생존 몹과 비교해서 더 엄격했다 — 원본대로 이번 웨이브 안에서만 체크하도록 수정, 간격 기준도 원본 실측치(52px→0.52유닛, X축 전용)로 교체.
  - **스폰 무적(`spawnInvuln`)**: 원본은 스폰 직후 2초간(`CONFIG.run.spawnProtect`) 모든 피해 판정 함수가 대상을 건너뛰는데 이게 통째로 빠져 있었다. `MonsterMove`에 `IsSpawnProtected` 추가하고 `PlayerAttack`/`MageProjectile` 둘 다 파괴 전에 확인하도록 연결(관통 카운트도 원본처럼 안 깎임).
  - PlayMode 테스트 2건 추가(스폰 포인트가 균등 랜덤이 아니라 정해진 창 안에만 찍히는지, 스폰 무적 중엔 공격이 안 통하고 무적 풀리면 통하는지). 처음엔 무적 테스트가 실패했는데 원인은 `AddComponent`가 `Awake()`를 동기 실행해서 그 뒤에 바꾼 public 필드가 반영이 안 된 테스트 코드 버그였음(private 타이머를 리플렉션으로 직접 세팅하도록 수정) — 프로덕션 코드 문제 아니었음. 재실행 **12/12 전체 통과**.
  - 아직 에디터로 직접 플레이 확인 전, `origin`에도 push 안 함.
- **2026-08-26** — **Part B 병합 후 남은 수정 5건 + 테스트 완료.** 사용자가 "Part B가 수정 다 했다는데 남은 게 있다"고 지적, 직접 고쳐달라고 요청해서 진행:
  - `MonsterSpawner.cs`: `FieldBounds.Min/Max`(없어진 Vector2 API) → `RandomX()`/`GroundY`로 교체(컴파일 에러였음)
  - `MonsterMove.cs`: 추적을 X축만으로 제한(Y는 `GroundY` 유지), `moveSpeed` 76→0.76(÷100 축척)
  - `Monster.prefab`: `moveSpeed` 0.76로, 색을 흰색→빨강(0.85, 0.2, 0.2)으로(HANDOFF.md 4번 "아군/적군 색 구분" 위반이었음 — 병합 후 코드 diff 재확인 중 새로 발견)
  - `BuildPartAScene.cs`: `BuildMonsterSpawner()` 추가해 씬에 스포너 등록(프리팹 참조 연결)
  - PlayMode 테스트 신설(`MonsterSpawnAndMoveTests.cs`): 웨이브 스폰이 필드 경계 안·GroundY에 배치되는지, 몹이 X축으로만 쫓아가고 Y는 그대로인지(발판 위 플레이어를 시뮬레이션해서 예전 버그가 재발하면 바로 잡히게 설계)
  - Unity 에디터가 닫혀 있는 걸 확인 후 배치 컴파일 + PlayMode 테스트 전체 실행 → 처음엔 신설 테스트 1건 실패(비활성화한 프리팹 템플릿을 Instantiate하면 인스턴스도 비활성 상태로 복사돼 `FindGameObjectsWithTag`가 못 찾음 — 내 테스트 코드 버그, 프로덕션 코드 문제 아니었음). `aliveMonsters` 리스트를 리플렉션으로 직접 읽는 방식으로 고쳐 재실행 → **10/10 전체 통과**.
  - 아직 `origin`에는 push 안 함 — 사용자 최종 확인 대기.
- **2026-08-25** — **Part B 브랜치를 `main`에 병합(로컬, 아직 push 안 함).** `PROGRESS.md` 한 건만 충돌(예상된 정상 상황, CLAUDE.md 협업규칙 6번대로 로그는 양쪽 보존·상태 섹션은 새로 작성). **단, Part B 로그에 "고쳤다"고 적힌 수정 3건이 실제 커밋(`ffbf18d`)에는 들어있지 않은 것을 파일 diff로 확인함** — CLAUDE.md 협업규칙 3번("로그 말고 실제 diff로 재확인")이 바로 이 사례에서 나왔다. 미반영 항목은 아래 "확인 필요"에 목록으로 정리했고, 이번엔 사용자가 Unity에서 직접 수정하기로 함.
- **2026-08-25** — **발판을 원웨이로 수정 + 층간 간격 확대.** 사용자가 플레이해보고 "플랫폼에 머리를 박으면 안 되는데(점프할 땐 통과, 밟을 수는 있게), 그리고 발판 사이 간격이 너무 작다"고 지적. 두 가지 다 원본과의 실제 차이였음:
  - 막힌(solid) `BoxCollider2D`였던 발판을 `PlatformEffector2D`(`useOneWay=true`) + `Collider2D.usedByEffector=true`로 교체 — 원본처럼 아래/옆에서는 통과하고 위에서 떨어질 때만 착지된다. 원본의 `dropTimer`(아래로 뛰어내리기)까지는 재현 안 함(범위 밖).
  - 발판 층간 간격을 원본 값(1.0~1.1유닛)에서 1.35유닛으로 넓힘 — **사용자가 명시적으로 요청한 의도적 편차**(원본 그대로 되돌리지 말 것). X 배치(발판 개수·폭·좌우 위치)는 원본 그대로 유지, Y(층 높이)만 조정. 점프 최대 높이(1.772유닛) 대비 76% 지점이라 여유 있게 닿는다.
  - 카메라 Y 위치를 2.5→3으로 살짝 올림(발판이 더 높아져서). PlayMode 테스트 1건 추가(`Player_PassesThroughOneWayPlatformFromBelow_ThenLandsOnTopFromAbove` — 아래서 위로 지나갈 때 안 막히는지 + 위에서 착지하는지 둘 다 확인). **8/8 전체 통과.**
  - `HANDOFF.md` 1번의 "단순화" 문구를 실제 구현 상태로 갱신.
- **2026-08-25** — **Part B: `main` 병합 + 신규 `FieldBounds` API 대응** ⚠️ *아래 항목 중 코드 수정 3건은 실제 커밋에 반영되지 않았음(바로 위 로그 참고) — 기록만 남기고 실제 수정은 별도로 진행.* `git merge origin/main`에서 충돌 5건 발생, 다음과 같이 해결:
  - `Assets/Scripts.meta`, `Assets/Scripts/FieldBounds.cs.meta` — GUID 충돌, `main` 쪽 GUID로 통일
  - `Assets/Scripts/FieldBounds.cs` — Part B가 넣어둔 임시 스텁을 버리고 `main`(Part A) 버전으로 완전 교체
  - `ProjectSettings/TagManager.asset` — `Enemy` 태그·`Ground` 레이어는 자동으로 잘 합쳐졌고, `serializedVersion: 2` vs `3` 한 줄만 `3`(main)으로
  - `PROGRESS.md` — 이 파일. 양쪽 로그 다 보존해서 병합
  - 충돌 해결만으론 컴파일이 안 돼서 추가로 고침: `MonsterSpawner.TryGetSpawnPosition()`이 옛 `FieldBounds.Min/Max`(Vector2) API를 쓰고 있었는데, 새 `FieldBounds`엔 그 필드가 없어져서 `FieldBounds.RandomX()`/`FieldBounds.GroundY`를 쓰도록 수정
  - 인터페이스 계약 위반도 같이 수정: `MonsterMove`의 추적 로직이 X·Y 둘 다 플레이어를 쫓아가고 있어서, 플레이어가 발판 위에 있으면 몬스터가 공중으로 떠오르는 문제가 있었음 — X축만 이동하고 Y는 `FieldBounds.GroundY`에 고정하도록 수정
  - 병합 중 추가로 발견한 버그: `Monster.prefab`/`MonsterMove.cs`의 `moveSpeed`가 원본 오니 스탯(76, px/s 스케일)을 캐릭터 이동속도와 같은 규칙(100px=1유닛, ÷100)으로 축척하지 않고 그대로 쓰고 있었음 — 26유닛 필드를 0.34초에 주파하는 셈이라 명백한 버그. `0.76`로 수정.
- **2026-08-25** — **물리엔진 실물 전환 + 원본 발판 15개 + 마법사(bow) 차지샷 구현.** 사용자 피드백("왜 이렇게 퀄리티가 낮지? 물리엔진이랑 마법사 캐릭터 기본공격을 구현해줘봐 그리고 플랫폼도 구현해줘")으로 이번 스프린트 범위를 확장(HANDOFF.md 1·3번 개정, 위 표 참고).
  - `CharacterMover2D`: 손으로 적분하던 중력(vy 변수)을 없애고 `Rigidbody2D`(실제 gravityScale) + `Physics2D.OverlapCircle` 접지 판정으로 교체. 전역 중력은 `Physics2D.gravity=(0,-26)`(원본 2600px/s² 축척).
  - `Ground` 물리 레이어 신설(`BuildPartAScene.EnsureGroundLayer`가 TagManager.asset에 직접 등록) — 바닥·발판만 여기 소속, 접지 판정이 플레이어/적/투사체를 오탐하지 않게.
  - 원본 `NORMAL_PLATFORMS`(15개, 4개 층) 좌표를 100px=1유닛로 환산해 실물 `BoxCollider2D` 발판으로 배치. **원웨이(아래서 통과) 로직은 이번엔 뺌** — 막힌 콜라이더로 단순화(위 HANDOFF.md 표 아래 단순화 항목 참고).
  - 필드 폭을 원본 그대로(`FieldBounds.MinX/MaxX` = 0/26, mapW 2600px)로 넓히고, 카메라를 고정 → X축 스크롤(`CameraFollow2D`, 플레이어를 따라가되 필드 경계에서 clamp)로 바꿈 — 넓어진 필드를 고정 카메라로 다 담으면 캐릭터가 너무 작아져서.
  - `MageAttack`/`MageProjectile` 신설: 원본 `bowFire()`/`CONFIG.bow` 차지 공식(데미지·관통·탄속 전부 차지율에 비례) 그대로 이식, ↑/↓로 상하 조준까지 지원. 근접형 `PlayerAttack`을 씬에서 빼고 이걸로 교체(코드는 남겨둠, 재사용 대비).
  - PlayMode 테스트 3건 추가(`PhysicsAndMageTests.cs`: 발판 착지, 풀차지/무차지 탄속·관통 비교, pierce 정확히 base+1타에서 멈추는지) + 기존 점프 테스트를 새 Rigidbody2D 기반 구현에 맞게 재작성. **7/7 전체 통과.**
- **2026-08-25** — **버그 수정: 조작키가 원본과 다름 + 필드 구조 자체가 잘못됨.** 사용자 피드백으로 발견. 원본 `KEYMAP`은 ArrowLeft/Right(좌우) · KeyC/Space(점프) · KeyZ(공격)인데 임의로 WASD+Space/좌클릭으로 구현했었다. 더 근본적으로, "점프"가 있다는 것 자체가 원본이 횡스크롤 플랫포머(X=좌우, Y=고정바닥+점프 중력)라는 뜻인데 `FieldBounds`를 top-down 자유이동 사각형(Vector2 Min/Max)으로 잘못 설계했었다 — HANDOFF.md의 "평면 아레나(점프 없음)" 결정 자체가 안일했음. `FieldBounds`를 `MinX/MaxX/GroundY`로, `CharacterMover2D`에 실제 점프 물리(원본 상수를 100px=1유닛로 축척: moveSpeed 2.7, jumpSpeed 9.6, gravity 26) 추가, `PlayerAttack` 트리거를 Z 단독으로 수정. PlayMode 테스트도 점프 물리(실제 컴포넌트를 FixedUpdate로 직접 구동, private 필드는 리플렉션으로 점프 시작 상태만 주입)까지 추가해 재검증 — 통과. `HANDOFF.md` 1·3번 항목도 같이 수정.
- **2026-08-25** — **Part A 완료, `main` 병합.** `FieldBounds.cs`(경계+clamp), `CharacterMover2D.cs`(이동), `PlayerAttack.cs`(공격 1개, Space/좌클릭)을 만들고, `Assets/Editor/BuildPartAScene.cs`(배치 실행용 씬 조립 스크립트)로 `CombatCore.unity`(Field/Camera/Player) 생성. 절차적 원형 스프라이트(`Circle.png`) 생성. TagManager에 `Enemy` 태그 등록.
  - **검증 과정에서 실제 이슈 하나 발견**: 처음엔 Edit Mode에서 `-executeMethod`로 물리 쿼리(`Physics2D.OverlapCircleAll`)를 직접 돌려 확인하려 했는데, 사거리 안의 Enemy가 안 죽는 것으로 나왔다(오탐). 원인은 Edit Mode에서는 Physics2D 월드가 제대로 안 돌고 `Destroy()`도 다음 프레임에 반영이 안 돼서였다 — 실제 `PlayerAttack` 로직 버그가 아니었다. `Assets/Tests/PlayMode/`에 PlayMode 테스트 2건(`PlayerAttackTests.cs`)을 만들어 `-runTests -testPlatform PlayMode`로 재검증 → 2/2 통과. **Edit Mode 배치 실행은 "씬/에셋이 예상대로 만들어졌는지" 구조 확인용으로만 쓰고, 실제 게임 로직(물리·충돌·Destroy) 검증은 항상 PlayMode 테스트로 할 것** — Part B도 동일하게 적용.
- **2026-08-25** — `Assets/Prefabs/Monster.prefab` 에디터에서 제작(SpriteRenderer+CircleCollider2D+`MonsterMove`, Tag=`Enemy`), `ProjectSettings/TagManager.asset`에 `Enemy` 태그 추가. 임시 GameObject에 `MonsterSpawner` 올리고 프리팹 연결해서 Play 테스트 — 웨이브 스폰 정상 동작 확인. 커밋 전.
- **2026-08-25** — Part B(몬스터&스폰) 착수. `Assets/Scripts/MonsterSpawner.cs`(웨이브 스폰: 3.6초 간격, 웨이브당 최대 7마리, 전체 상한 22, 최소 간격 유지 랜덤 배치·10회 재시도), `Assets/Scripts/MonsterMove.cs`(가장 가까운 Player 태그로 직선 이동 + 접촉 시 정지, 실제 데미지는 미구현) 작성. 브랜치 단독 컴파일용 `Assets/Scripts/FieldBounds.cs` 임시 스텁 추가(Part A 병합 시 교체 예정). `Monster.prefab`은 에디터 작업으로 남김.
- **2026-08-25** — 레포 세팅 완료. `HANDOFF.md`(스펙), `CLAUDE.md`(작업 규칙), `reference/project_test.html`(원본 참고) 커밋. 코드 작업은 아직 시작 전.
