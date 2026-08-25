# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.

## 지금 상태

**Part A(필드+카메라+플레이어+공격) `main`에 병합 완료, 이후 물리엔진/발판/마법사 공격으로 확장 완료.** Part B(몬스터+스폰) 작업 시작 가능.
`Assets/Scenes/CombatCore.unity`를 열면 플레이어(파란 원)가 원본과 동일한 조작(←/→ 이동, **C만** 점프, Z 길게 눌러 차지 후 발사)으로 실제 Physics2D 중력을 받으며 원본 발판 15개를 오르내리고, 마법탄으로 Enemy 태그 대상을 관통 공격 가능한 상태까지 확인됨(PlayMode 테스트 7/7 통과, 아래 로그 참고).

## 오늘의 분업 (2026-08-25) — 구현 + 병합까지 오늘 안에

1단계(HANDOFF.md) 4개 항목을 두 세션이 절반씩 맡는다. **씬 파일 동시 편집을 피하려고, "플레이어 쪽"과 "몬스터 쪽"으로 나눴다** — 서로 새 파일만 만들면 되고 겹치는 파일이 최소화되도록 설계.

### Part A — 플레이어 & 필드 ✅ 완료, `main` 병합됨
- 브랜치: `feature/player-field` (병합 완료, 이제 새로 안 건드림)
- 범위: HANDOFF.md 1번(필드 경계+카메라) + 2번(캐릭터 컨트롤러, 원형 스프라이트+아군색) + 3번(공격 판정)
- 만든 파일: 아래 "완료된 것" 참고

### Part B — 몬스터 & 스폰 ← 지금 여기 작업할 차례
- 브랜치: `feature/monster-combat` (이미 만들어져 origin에 푸시돼 있음, `git fetch` 후 체크아웃)
- 범위: HANDOFF.md 2번(몬스터 스폰 알고리즘) — 몬스터는 원형+적군색 프리팹으로, 스폰 로직 + 몬스터의 단순 이동(플레이어 쪽으로 직선 이동)
- 만들 파일(예정): `Assets/Prefabs/Monster.prefab`, `Assets/Scripts/MonsterSpawner.cs`, `Assets/Scripts/MonsterMove.cs`
- **먼저 `git checkout feature/monster-combat && git merge main`으로 Part A 내용을 받고 시작할 것.** `CombatCore.unity`, `FieldBounds.cs`, `Enemy` 태그, `Circle.png`가 이미 존재한다.
- 스크립트/프리팹은 `Assets/Scripts/YokaiFront.Runtime.asmdef` 어셈블리 소속으로 만들 것(같은 폴더에 두면 자동 포함). Editor 자동화 패턴은 `Assets/Editor/BuildPartAScene.cs` 참고해도 됨(필수는 아님, GUI로 직접 만들어도 됨).
- 원형 스프라이트는 `Assets/Sprites/Circle.png` 재사용 — 새로 만들지 말 것. 색만 적군색(예: 빨강)으로.
- **메인 씬은 마지막 순간에만 건드린다** — 스포너 오브젝트 하나 추가하는 커밋으로 마무리.

### 둘을 잇는 인터페이스 계약 (임의로 바꾸지 말 것)
- 플레이어 오브젝트 Tag = `"Player"` (씬에 이미 있음)
- 몬스터 프리팹 Tag = `"Enemy"` (TagManager에 이미 등록돼 있음, Part B가 새로 추가할 필요 없음)
- **필드 경계 개정(2026-08-25): `FieldBounds.MinX=0, MaxX=26`로 확장됨(원본 mapW 2600px 그대로).** 이전엔 더 좁은 임시값이었는데 실제 발판(NORMAL_PLATFORMS)을 넣으면서 원본 폭 그대로 맞췄다. `FieldBounds.RandomX()` 헬퍼로 새 범위가 자동 반영되니 그대로 쓰면 됨.
- **Y축은 더 이상 고정 바닥 하나가 아니라 실제 Physics2D**(원본 발판 15개, `Ground` 물리 레이어)다. 몬스터는 HANDOFF.md 2번 스펙대로 `GroundY`에서만 X로 오가면 되고(발판을 오르내리는 AI는 이번 스프린트 범위 밖), 접지 판정이 필요하면 `Ground` 레이어를 참고할 것. `CharacterMover2D.cs`가 실제 구현 예시.
- 전투는 v0 기준 **몬스터 체력 개념 없이 즉시 `Destroy()`** — 근접형 `PlayerAttack`과 신규 마법사 차지샷(`MageProjectile`) 둘 다 동일 정책(HANDOFF.md 3번 참고). `Health` 컴포넌트는 이번 판엔 안 만든다.
- **검증은 PlayMode 테스트로 할 것.** Edit Mode에서는 `Physics2D` 쿼리와 지연 `Destroy()`가 못 미덥다는 게 Part A에서 실제로 확인됨(아래 로그 참고) — `-executeMethod`로 정적으로 씬만 만들어놓고 "됐다"고 하지 말고, `Assets/Tests/PlayMode/`에 테스트를 추가해서 `-runTests -testPlatform PlayMode`로 돌려 확인할 것.

### 병합 순서
1. ~~Part A 먼저 `main`에 병합~~ ✅ 완료 (커밋 `17b081e`)
2. Part B는 `main`을 받아(`git merge main` 또는 `git pull origin main`) 자기 브랜치에 반영, 그 다음 **메인 씬에 스포너 오브젝트 하나만 추가**하는 작은 커밋으로 마무리 후 병합
3. 병합 후 플레이 테스트: 필드 안에 몬스터가 스폰되고, 공격 버튼으로 죽는지 확인

## 다음 할 일

**→ Part B: `feature/monster-combat`에서 위 범위대로 몬스터 스폰 구현.** 진행되는 대로 아래 로그와 체크리스트를 갱신할 것.

## 체크리스트 (HANDOFF.md 개발 순서)

- [x] 필드 경계 + 카메라 (처음엔 고정이었으나 발판 추가로 X축 스크롤로 개정, 위 로그 참고)
- [x] 캐릭터 컨트롤러 임포트 → 원형 스프라이트 + 색 적용 (에셋스토어 패키지 대신 자체 구현, 아래 로그 참고)
- [x] 실제 Physics2D 중력/충돌 + 원본 발판 15개 (범위 확장, 위 로그 참고)
- [ ] 몬스터 스폰 로직 (HANDOFF.md 2번)
- [x] 공격 판정 — 마법사 차지샷으로 구체화(HANDOFF.md 3번)
- [ ] 위 항목 다 붙어서 핵심 루프 한 번 플레이 가능 (Part B 완료 후)

## 확인 필요 / 막힌 것

- **캐릭터 컨트롤러**: HANDOFF.md는 "이미 구현된 컨트롤러 재사용"을 전제했지만, 배치 자동화 환경(Unity Editor GUI 없이 명령줄로만 작업)에서는 에셋스토어 패키지를 인증 없이 받아올 수 없었다. 대신 `CharacterMover2D.cs`를 최소 구현으로 직접 짬. 나중에 실제 컨트롤러 에셋을 쓰기로 하면 이 파일 하나만 교체하면 되도록 다른 스크립트와 결합을 안 시켜뒀다. 문제 되면 여기 갱신할 것.

- **Part B가 `main` 병합할 때 이렇게 할 것** (로컬 임시 브랜치로 `origin/feature/monster-combat` + `main`을 미리 시험 병합해서 실제로 확인해둔 내용 — 추측 아님):
  - `git fetch origin && git merge origin/main`을 `feature/monster-combat`에서 실행하면 충돌 5개가 난다:
    - `Assets/Scripts.meta`, `Assets/Scripts/FieldBounds.cs.meta` — 폴더/파일을 양쪽이 독립적으로 만들면서 GUID가 서로 다름. `main`(`>>>>>>> main` 아래) 쪽 GUID로 통일.
    - `Assets/Scripts/FieldBounds.cs` — Part B가 넣어둔 스텁을 **통째로 버리고 `main` 버전으로 교체**(스텁 자체 주석에도 이미 이렇게 하라고 적혀있음).
    - `ProjectSettings/TagManager.asset` — `Enemy` 태그·`Ground` 레이어는 자동으로 잘 합쳐지고, `serializedVersion: 2` vs `3` 딱 한 줄만 충돌. `3`(main)으로.
    - `PROGRESS.md` — 이 파일. 양쪽 로그를 다 남기고 합칠 것(한쪽으로 덮어쓰지 말 것).
  - **충돌 해결만으론 안 끝남 — 컴파일 에러 남는 곳 1건**: `MonsterSpawner.cs`의 `TryGetSpawnPosition()`(대략 76~77번째 줄)이 옛 `FieldBounds.Min.x/y`, `FieldBounds.Max.x/y`(Vector2) API를 쓰는데, 새 `FieldBounds`는 `MinX/MaxX/GroundY`(float)로 바뀌어서 그 필드 자체가 없다. 이렇게 고칠 것:
    ```csharp
    Vector2 candidate = new Vector2(FieldBounds.RandomX(), FieldBounds.GroundY);
    ```
  - **동작 관련(컴파일은 되지만 인터페이스 계약 위반)**: `MonsterMove.cs`의 추적 로직이 `toTarget.normalized`로 X·Y 둘 다 플레이어 쪽으로 이동한다. 위 "필드 경계" 계약("몬스터는 GroundY에서 X만 오간다")과 안 맞아서, 플레이어가 발판 위에 있으면 몬스터가 공중으로 떠올라 쫓아가는 모양이 된다. X축만 움직이고 Y는 `FieldBounds.GroundY`로 고정하도록 고칠 것.

## 로그 (최신이 위)

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
- **2026-08-25** — 레포 세팅 완료. `HANDOFF.md`(스펙), `CLAUDE.md`(작업 규칙), `reference/project_test.html`(원본 참고) 커밋. 코드 작업은 아직 시작 전.
