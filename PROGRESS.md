# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.

## 지금 상태

**1단계 — 전투 코어 프로토타입 (HANDOFF.md 참조).**
Part B(몬스터 & 스폰, `feature/monster-combat`) 스크립트+프리팹 작성 완료, 임시 GameObject(`MonsterSpawner` + `Monster` 프리팹)로 웨이브 스폰 확인 완료. 아직 커밋 전. Part A(플레이어 & 필드, `feature/player-field`)는 별도 세션이 진행 중.

## 오늘의 분업 (2026-08-25) — 구현 + 병합까지 오늘 안에

1단계(HANDOFF.md) 4개 항목을 두 세션이 절반씩 맡는다. **씬 파일 동시 편집을 피하려고, "플레이어 쪽"과 "몬스터 쪽"으로 나눴다** — 서로 새 파일만 만들면 되고 겹치는 파일이 최소화되도록 설계.

### Part A — 플레이어 & 필드 (다른 세션이 진행 중)
- 브랜치: `feature/player-field`
- 범위: HANDOFF.md 1번(필드 경계+카메라) + 2번(캐릭터 컨트롤러 임포트, 원형 스프라이트+아군색) + 3번의 공격 판정(버튼 입력 → 반경 안 `Enemy` 태그 오브젝트 `Destroy()`)
- 만드는 파일(예정): `Assets/Scripts/FieldBounds.cs`, `Assets/Scripts/PlayerAttack.cs`, 메인 씬에 Field·Camera·Player 오브젝트

### Part B — 몬스터 & 스폰 (이 세션이 진행 중)
- 브랜치: `feature/monster-combat`
- 범위: HANDOFF.md 2번(몬스터 스폰 알고리즘) — 몬스터는 원형+적군색 프리팹으로, 스폰 로직 + 몬스터의 단순 이동(플레이어 쪽으로 직선 이동)
- 만든 파일: `Assets/Scripts/MonsterSpawner.cs`, `Assets/Scripts/MonsterMove.cs`, `Assets/Prefabs/Monster.prefab` (전부 완료 + 임시 GameObject로 웨이브 스폰 확인됨)
- **씬은 건드리지 말 것** — 스포너도 프리팹 안에서든, 임시 테스트 씬에서든 독립적으로 만들어서 검증하고, 메인 씬에 스포너 오브젝트 하나 추가하는 건 Part A 병합 후 마지막에. (검증용으로 만든 임시 `MonsterSpawner` GameObject는 커밋 전에 Hierarchy에서 지울 것 — 씬에 남기지 않는다.)

### 둘을 잇는 인터페이스 계약 (임의로 바꾸지 말 것)
- 플레이어 오브젝트 Tag = `"Player"`
- 몬스터 프리팹 Tag = `"Enemy"` (`ProjectSettings/TagManager.asset`에 추가 완료)
- 필드 경계: `Assets/Scripts/FieldBounds.cs`에 `public static class FieldBounds { public static Vector2 Min, Max; }` — Part B는 스폰 위치를 이 범위 안에서 뽑는다. Part A가 먼저 만들어 푸시하면 그걸 그대로 가져다 쓸 것.
- 전투는 v0 기준 **몬스터 체력 개념 없이, 맞으면 즉시 `Destroy()`** (HANDOFF.md 3번 참고). `Health` 컴포넌트는 이번 판엔 안 만든다 — 두 파트가 서로 안 기다리게 하려는 의도적 단순화.

### 병합 순서
1. Part A 먼저 `main`에 병합 (씬 뼈대 확정 — Field/Camera/Player가 있어야 Part B가 마지막에 스포너를 얹을 자리가 생김)
2. Part B는 `main` 병합 후 최신 상태를 받아(`git pull origin main`) 자기 브랜치에 반영, 그 다음 **메인 씬에 스포너 오브젝트 하나만 추가**하는 작은 커밋으로 마무리 후 병합
3. 병합 후 플레이 테스트: 필드 안에 몬스터가 스폰되고, 공격 버튼으로 죽는지 확인

## 다음 할 일

**→ Part B 남은 작업:**
1. 검증용 임시 `MonsterSpawner` GameObject를 씬에서 지운 뒤(위 "씬은 건드리지 말 것" 참고) `git add` + `git commit`으로 이 브랜치 작업 저장
2. Part A가 `main`에 병합될 때까지 대기
3. Part A 병합 후 `git pull origin main` 받고, 이 브랜치의 임시 `FieldBounds.cs` 스텁을 Part A 버전으로 교체(충돌 예상), `TagManager.asset`도 Part A가 `Player` 태그를 따로 추가했다면 합치기(작은 충돌 예상)
4. 그 다음 메인 씬에 스포너 오브젝트 하나 추가하는 작은 커밋으로 마무리 후 병합
5. 병합된 `main`에서 최종 플레이 테스트 (필드+몬스터 스폰+공격 판정 전부 붙여서 확인)

## 체크리스트 (HANDOFF.md 개발 순서)

- [ ] 필드 경계 + 고정 카메라 (Part A)
- [ ] 캐릭터 컨트롤러 임포트 → 원형 스프라이트 + 색 적용 (Part A)
- [x] 몬스터 스폰 로직 (HANDOFF.md 2번) — 코드 작성 + 프리팹 제작 + 임시 GameObject로 웨이브 스폰 확인 완료
- [ ] 공격 판정 (HANDOFF.md 3번) (Part A)
- [ ] 위 4개 다 붙어서 핵심 루프 한 번 플레이 가능

## 확인 필요 / 막힌 것

- **`FieldBounds.cs` 임시 스텁 추가함** — Part B 단독 컴파일/테스트를 위해 계약대로(`Vector2 Min, Max`) 최소 버전을 이 브랜치에 넣었다(값은 임시: Min(-8,-4.5)/Max(8,4.5)). Part A가 `main`에 병합된 뒤 `git pull` 받으면 이 파일에서 충돌 날 것 — 그때 Part A 버전으로 덮어쓰면 됨. Part A 세션도 이 점 인지해두면 좋음.
- **`TagManager.asset`에 `Enemy` 태그만 추가함** — Part A가 `Player` 태그를 독립적으로 추가한다면 병합 시 같은 파일에서 작은 충돌이 날 수 있음(리스트 두 줄 합치기 수준이라 위험하지 않음).
- **몬스터의 "접촉 시 공격"은 실제 데미지 미구현** — HANDOFF.md 2번엔 "접촉 시 공격"이라고만 나와 있고, 플레이어 Health(체력) 시스템 자체가 이번 스프린트 범위 밖(HANDOFF.md "범위 밖" 목록)이라 `MonsterMove.TryAttack()`에 자리만 만들어두고 실제 데미지 적용은 비워뒀다. 이후 스프린트에서 플레이어 Health가 생기면 연결. 지금 범위에서 이대로 둬도 되는지 확인 필요.

## 로그 (최신이 위)

- **2026-08-25** — `Assets/Prefabs/Monster.prefab` 에디터에서 제작(SpriteRenderer+CircleCollider2D+`MonsterMove`, Tag=`Enemy`), `ProjectSettings/TagManager.asset`에 `Enemy` 태그 추가. 임시 GameObject에 `MonsterSpawner` 올리고 프리팹 연결해서 Play 테스트 — 웨이브 스폰 정상 동작 확인. 커밋 전.
- **2026-08-25** — Part B(몬스터&스폰) 착수. `Assets/Scripts/MonsterSpawner.cs`(웨이브 스폰: 3.6초 간격, 웨이브당 최대 7마리, 전체 상한 22, 최소 간격 유지 랜덤 배치·10회 재시도), `Assets/Scripts/MonsterMove.cs`(가장 가까운 Player 태그로 직선 이동 + 접촉 시 정지, 실제 데미지는 미구현) 작성. 브랜치 단독 컴파일용 `Assets/Scripts/FieldBounds.cs` 임시 스텁 추가(Part A 병합 시 교체 예정). `Monster.prefab`은 에디터 작업으로 남김.
- **2026-08-25** — 레포 세팅 완료. `HANDOFF.md`(스펙), `CLAUDE.md`(작업 규칙), `reference/project_test.html`(원본 참고) 커밋. 코드 작업은 아직 시작 전.
