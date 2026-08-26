# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.

## 지금 상태

**1단계 — 전투 코어 프로토타입, Part A + Part B 전부 `main`에 병합 및 검증 완료. 아직 `origin`에는 push 안 함.**

`Assets/Scenes/CombatCore.unity`를 열면 플레이어(파란 원)가 원본과 동일한 조작(←/→ 이동, **C만** 점프, Z 길게 눌러 차지 후 발사)으로 실제 Physics2D 중력을 받으며 원본 발판 15개(원웨이, 층간 1.35유닛)를 오르내리고, 마법탄으로 Enemy 태그 대상을 관통 공격 가능. 몬스터도 3.6초마다 웨이브 스폰돼 원본처럼 발판/바닥그리드 스폰 포인트(균등 랜덤 아님) 중 하나에서 나타나고, 스폰 직후 2초 무적, GroundY에서 X축으로만 플레이어를 쫓아옴. PlayMode 테스트 **12/12 통과**.

Part B 병합 직후 컴파일 에러 포함 5건 + 사용자가 직접 플레이해보고 지적한 스폰 로직 불일치까지 전부 수정 완료. 남은 건 push뿐 — 사용자 최종 확인 후 진행.

## 오늘의 분업 (2026-08-25) — 구현 + 병합까지 오늘 안에

1단계(HANDOFF.md) 4개 항목을 두 세션이 절반씩 맡는다. **씬 파일 동시 편집을 피하려고, "플레이어 쪽"과 "몬스터 쪽"으로 나눴다** — 서로 새 파일만 만들면 되고 겹치는 파일이 최소화되도록 설계.

### Part A — 플레이어 & 필드 ✅ 완료, `main` 병합됨
- 브랜치: `feature/player-field` (병합 완료, 이제 새로 안 건드림)
- 범위: HANDOFF.md 1번(필드 경계+카메라, 이후 실제 Physics2D+발판 15개로 확장) + 2번(캐릭터 컨트롤러, 원형 스프라이트+아군색) + 3번(공격 판정, 이후 마법사 차지샷으로 구체화)
- 만든 파일: 아래 "로그" 참고

### Part B — 몬스터 & 스폰 ✅ 완료, `main`에 병합·수정·검증 끝(로컬)
- 브랜치: `feature/monster-combat` (`ffbf18d`까지 병합 — 이후 이 브랜치는 안 씀)
- 범위: HANDOFF.md 2번(몬스터 스폰 알고리즘) — 몬스터는 원형+적군색 프리팹, 스폰 로직 + 단순 이동
- 완료: 스폰/이동 스크립트, 프리팹, 병합 후 남은 수정 5건, PlayMode 테스트(`MonsterSpawnAndMoveTests.cs`) 전부 완료(아래 로그 참고)
- 남은 것: `origin`에 push (사용자 최종 확인 대기 중)

### 둘을 잇는 인터페이스 계약 (임의로 바꾸지 말 것)
- 플레이어 오브젝트 Tag = `"Player"` (씬에 이미 있음, Unity 기본 제공 태그라 TagManager에 별도 등록 불필요)
- 몬스터 프리팹 Tag = `"Enemy"` (TagManager에 등록 완료)
- **필드 경계(2026-08-25 개정): `FieldBounds.MinX=0, MaxX=26`, 횡스크롤 플랫포머로 구조 자체가 바뀜.** 원본 mapW(2600px) 그대로 100px=1유닛 축척. `FieldBounds.RandomX()` 헬퍼로 새 범위가 자동 반영되니 그대로 쓸 것.
- **Y축은 고정 바닥 하나가 아니라 실제 Physics2D**(원본 발판 15개, `Ground` 물리 레이어)다. 몬스터는 HANDOFF.md 2번 스펙대로 `GroundY`에서만 X로 오간다(발판을 오르내리는 AI는 범위 밖).
- 전투는 v0 기준 **몬스터 체력 개념 없이 즉시 `Destroy()`** — 근접형 `PlayerAttack`과 마법사 차지샷(`MageProjectile`) 둘 다 동일 정책(HANDOFF.md 3번 참고). `Health` 컴포넌트는 이번 판엔 안 만든다.
- **검증은 가능하면 PlayMode 테스트로.** Part A에서 Edit Mode 배치 실행으로는 `Physics2D`/지연 `Destroy()`가 못 미덥다는 걸 실제로 확인함(아래 로그 참고) — Part B도 여유가 되면 `Assets/Tests/PlayMode/`에 스폰/이동 테스트를 추가하는 걸 권장(필수는 아님, 지금은 수동 Play 테스트로 대체).

### 병합 순서
1. ~~Part A 먼저 `main`에 병합~~ ✅ 완료 (커밋 `17b081e`)
2. Part B는 `main`을 받아(`git merge origin/main`) 자기 브랜치에 반영 — ✅ 충돌 해결 + 코드 수정 진행 중, 그 다음 **메인 씬에 스포너 오브젝트 하나만 추가**하는 작은 커밋으로 마무리 후 병합
3. 병합 후 플레이 테스트: 필드 안에 몬스터가 스폰되고, 공격 버튼으로 죽는지 확인

## 다음 할 일

1. **사용자가 다시 플레이해서 스폰/히트 로직 수정 확인 → `git push origin main`** — 배치 컴파일 + PlayMode 12/12는 통과했지만 아직 에디터로 직접 플레이 확인 전.
2. 그다음 스프린트로 넘어가기 전에 **CLAUDE.md "코딩/파일 정리 규칙"을 기존 코드에 적용하는 대규모 리팩터**(폴더 분리·네임스페이스·asmdef·Monster→Enemy 리네임). 미병합 브랜치가 0개인 지금 시점이 적기 — 절차는 CLAUDE.md "대규모 리팩터링 절차" 참고.

## 체크리스트 (HANDOFF.md 개발 순서)

- [x] 필드 경계 + 카메라 (처음엔 고정이었으나 발판 추가로 X축 스크롤로 개정, 아래 로그 참고)
- [x] 캐릭터 컨트롤러 임포트 → 원형 스프라이트 + 색 적용 (에셋스토어 패키지 대신 자체 구현, 아래 로그 참고)
- [x] 실제 Physics2D 중력/충돌 + 원본 발판 15개 (범위 확장, 아래 로그 참고)
- [x] 몬스터 스폰 로직 (HANDOFF.md 2번) — 병합·수정·PlayMode 테스트까지 완료
- [x] 공격 판정 — 마법사 차지샷으로 구체화(HANDOFF.md 3번)
- [x] 위 항목 다 붙어서 핵심 루프 한 번 플레이 가능 — 배치모드로 검증(PlayMode 12/12). 스폰 로직은 사용자가 직접 플레이해서 원본과 다른 점을 지적 → 재수정함(위 로그 참고), **이 수정에 대한 재확인은 아직**(위 "다음 할 일" 참고)

## 확인 필요 / 막힌 것

- **캐릭터 컨트롤러**: HANDOFF.md는 "이미 구현된 컨트롤러 재사용"을 전제했지만, 배치 자동화 환경(Unity Editor GUI 없이 명령줄로만 작업)에서는 에셋스토어 패키지를 인증 없이 받아올 수 없었다. 대신 `CharacterMover2D.cs`를 최소 구현으로 직접 짬. 나중에 실제 컨트롤러 에셋을 쓰기로 하면 이 파일 하나만 교체하면 되도록 다른 스크립트와 결합을 안 시켜뒀다. 문제 되면 여기 갱신할 것.
- **몬스터의 "접촉 시 공격"은 실제 데미지 미구현** — HANDOFF.md 2번엔 "접촉 시 공격"이라고만 나와 있고, 플레이어 Health(체력) 시스템 자체가 이번 스프린트 범위 밖이라 `MonsterMove.TryAttack()`에 자리만 만들어두고 실제 데미지 적용은 비워뒀다. 이후 스프린트에서 플레이어 Health가 생기면 연결.
- **몬스터 스프라이트는 Unity 기본 내장 Circle을 그대로 씀** (`Assets/Sprites/Circle.png`로 통일하진 않음) — HANDOFF.md 4번 스펙("유니티 기본 Circle 스프라이트로 충분")은 만족하지만, 플레이어 쪽과 완전히 같은 텍스처 에셋으로 맞추고 싶으면 나중에 `Circle.png`로 교체 가능. 기능상 문제는 없음.
- **스폰 로직 재수정(2026-08-26)은 아직 에디터로 직접 플레이 확인 전** — 배치 컴파일 + PlayMode 12/12만 통과 확인. 이전 버전(균등 랜덤 스폰)은 사용자가 직접 플레이해보고 "원본과 너무 다르다"고 지적해서 발견됐으니, 이번 수정도 실제로 플레이해서 확인 필요.

## 로그 (최신이 위)

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
