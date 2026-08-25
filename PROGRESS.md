# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.

## 지금 상태

**Part A(필드+카메라+플레이어+공격) `main`에 병합 완료.** Part B(몬스터+스폰) 작업 시작 가능.
`Assets/Scenes/CombatCore.unity`를 열면 플레이어(파란 원)가 원본과 동일한 조작(←/→ 이동, C/Space 점프, Z 공격)으로 필드 안에서 움직이고 공격 가능한 상태까지 확인됨(PlayMode 테스트 통과, 아래 로그 참고).

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
- 필드 경계: 원본처럼 횡스크롤 구조라 **X만 자유, Y는 고정 바닥**이다 — `FieldBounds.MinX` / `MaxX` / `GroundY` (float). 몬스터도 `GroundY`에서 X만 오가게 만들 것(공중부양 X). `FieldBounds.RandomX()` 헬퍼 있음.
- 전투는 v0 기준 **몬스터 체력 개념 없이, `PlayerAttack`이 맞은 대상을 즉시 `Destroy()`** (HANDOFF.md 3번 참고). `Health` 컴포넌트는 이번 판엔 안 만든다.
- **검증은 PlayMode 테스트로 할 것.** Edit Mode에서는 `Physics2D` 쿼리와 지연 `Destroy()`가 못 미덥다는 게 Part A에서 실제로 확인됨(아래 로그 참고) — `-executeMethod`로 정적으로 씬만 만들어놓고 "됐다"고 하지 말고, `Assets/Tests/PlayMode/`에 테스트를 추가해서 `-runTests -testPlatform PlayMode`로 돌려 확인할 것.

### 병합 순서
1. ~~Part A 먼저 `main`에 병합~~ ✅ 완료 (커밋 `17b081e`)
2. Part B는 `main`을 받아(`git merge main` 또는 `git pull origin main`) 자기 브랜치에 반영, 그 다음 **메인 씬에 스포너 오브젝트 하나만 추가**하는 작은 커밋으로 마무리 후 병합
3. 병합 후 플레이 테스트: 필드 안에 몬스터가 스폰되고, 공격 버튼으로 죽는지 확인

## 다음 할 일

**→ Part B: `feature/monster-combat`에서 위 범위대로 몬스터 스폰 구현.** 진행되는 대로 아래 로그와 체크리스트를 갱신할 것.

## 체크리스트 (HANDOFF.md 개발 순서)

- [x] 필드 경계 + 고정 카메라
- [x] 캐릭터 컨트롤러 임포트 → 원형 스프라이트 + 색 적용 (에셋스토어 패키지 대신 자체 구현, 아래 로그 참고)
- [ ] 몬스터 스폰 로직 (HANDOFF.md 2번)
- [x] 공격 판정 (HANDOFF.md 3번)
- [ ] 위 4개 다 붙어서 핵심 루프 한 번 플레이 가능 (Part B 완료 후)

## 확인 필요 / 막힌 것

- **캐릭터 컨트롤러**: HANDOFF.md는 "이미 구현된 컨트롤러 재사용"을 전제했지만, 배치 자동화 환경(Unity Editor GUI 없이 명령줄로만 작업)에서는 에셋스토어 패키지를 인증 없이 받아올 수 없었다. 대신 `CharacterMover2D.cs`를 최소 구현으로 직접 짬. 나중에 실제 컨트롤러 에셋을 쓰기로 하면 이 파일 하나만 교체하면 되도록 다른 스크립트와 결합을 안 시켜뒀다. 문제 되면 여기 갱신할 것.

## 로그 (최신이 위)

- **2026-08-25** — **버그 수정: 조작키가 원본과 다름 + 필드 구조 자체가 잘못됨.** 사용자 피드백으로 발견. 원본 `KEYMAP`은 ArrowLeft/Right(좌우) · KeyC/Space(점프) · KeyZ(공격)인데 임의로 WASD+Space/좌클릭으로 구현했었다. 더 근본적으로, "점프"가 있다는 것 자체가 원본이 횡스크롤 플랫포머(X=좌우, Y=고정바닥+점프 중력)라는 뜻인데 `FieldBounds`를 top-down 자유이동 사각형(Vector2 Min/Max)으로 잘못 설계했었다 — HANDOFF.md의 "평면 아레나(점프 없음)" 결정 자체가 안일했음. `FieldBounds`를 `MinX/MaxX/GroundY`로, `CharacterMover2D`에 실제 점프 물리(원본 상수를 100px=1유닛로 축척: moveSpeed 2.7, jumpSpeed 9.6, gravity 26) 추가, `PlayerAttack` 트리거를 Z 단독으로 수정. PlayMode 테스트도 점프 물리(실제 컴포넌트를 FixedUpdate로 직접 구동, private 필드는 리플렉션으로 점프 시작 상태만 주입)까지 추가해 재검증 — 통과. `HANDOFF.md` 1·3번 항목도 같이 수정.
- **2026-08-25** — **Part A 완료, `main` 병합.** `FieldBounds.cs`(경계+clamp), `CharacterMover2D.cs`(이동), `PlayerAttack.cs`(공격 1개, Space/좌클릭)을 만들고, `Assets/Editor/BuildPartAScene.cs`(배치 실행용 씬 조립 스크립트)로 `CombatCore.unity`(Field/Camera/Player) 생성. 절차적 원형 스프라이트(`Circle.png`) 생성. TagManager에 `Enemy` 태그 등록.
  - **검증 과정에서 실제 이슈 하나 발견**: 처음엔 Edit Mode에서 `-executeMethod`로 물리 쿼리(`Physics2D.OverlapCircleAll`)를 직접 돌려 확인하려 했는데, 사거리 안의 Enemy가 안 죽는 것으로 나왔다(오탐). 원인은 Edit Mode에서는 Physics2D 월드가 제대로 안 돌고 `Destroy()`도 다음 프레임에 반영이 안 돼서였다 — 실제 `PlayerAttack` 로직 버그가 아니었다. `Assets/Tests/PlayMode/`에 PlayMode 테스트 2건(`PlayerAttackTests.cs`)을 만들어 `-runTests -testPlatform PlayMode`로 재검증 → 2/2 통과. **Edit Mode 배치 실행은 "씬/에셋이 예상대로 만들어졌는지" 구조 확인용으로만 쓰고, 실제 게임 로직(물리·충돌·Destroy) 검증은 항상 PlayMode 테스트로 할 것** — Part B도 동일하게 적용.
- **2026-08-25** — 레포 세팅 완료. `HANDOFF.md`(스펙), `CLAUDE.md`(작업 규칙), `reference/project_test.html`(원본 참고) 커밋. 코드 작업은 아직 시작 전.
