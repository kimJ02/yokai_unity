# 진행 상황

> 새 세션은 이 파일부터 읽고 이어서 작업할 것. 작업할 때마다 여기를 갱신한다.

## 지금 상태

**1단계 — 전투 코어 프로토타입 (HANDOFF.md 참조), 아직 코드 작업 시작 전.**
Unity 프로젝트 스캐폴드와 문서만 있는 상태. Assets 폴더에 게임플레이 스크립트/씬 작업 전무.

## 오늘의 분업 (2026-08-25) — 구현 + 병합까지 오늘 안에

1단계(HANDOFF.md) 4개 항목을 두 세션이 절반씩 맡는다. **씬 파일 동시 편집을 피하려고, "플레이어 쪽"과 "몬스터 쪽"으로 나눴다** — 서로 새 파일만 만들면 되고 겹치는 파일이 최소화되도록 설계.

### Part A — 플레이어 & 필드 (이 세션이 진행 중)
- 브랜치: `feature/player-field`
- 범위: HANDOFF.md 1번(필드 경계+카메라) + 2번(캐릭터 컨트롤러 임포트, 원형 스프라이트+아군색) + 3번의 공격 판정(버튼 입력 → 반경 안 `Enemy` 태그 오브젝트 `Destroy()`)
- 만드는 파일(예정): `Assets/Scripts/FieldBounds.cs`, `Assets/Scripts/PlayerAttack.cs`, 메인 씬에 Field·Camera·Player 오브젝트

### Part B — 몬스터 & 스폰 (다른 세션에게 인수인계)
- 브랜치: `feature/monster-combat`
- 범위: HANDOFF.md 2번(몬스터 스폰 알고리즘) — 몬스터는 원형+적군색 프리팹으로, 스폰 로직 + 몬스터의 단순 이동(플레이어 쪽으로 직선 이동)
- 만드는 파일(예정): `Assets/Prefabs/Monster.prefab`, `Assets/Scripts/MonsterSpawner.cs`, `Assets/Scripts/MonsterMove.cs`
- **씬은 건드리지 말 것** — 스포너도 프리팹 안에서든, 임시 테스트 씬에서든 독립적으로 만들어서 검증하고, 메인 씬에 스포너 오브젝트 하나 추가하는 건 Part A 병합 후 마지막에.

### 둘을 잇는 인터페이스 계약 (임의로 바꾸지 말 것)
- 플레이어 오브젝트 Tag = `"Player"`
- 몬스터 프리팹 Tag = `"Enemy"`
- 필드 경계: `Assets/Scripts/FieldBounds.cs`에 `public static class FieldBounds { public static Vector2 Min, Max; }` — Part B는 스폰 위치를 이 범위 안에서 뽑는다. Part A가 먼저 만들어 푸시하면 그걸 그대로 가져다 쓸 것.
- 전투는 v0 기준 **몬스터 체력 개념 없이, 맞으면 즉시 `Destroy()`** (HANDOFF.md 3번 참고). `Health` 컴포넌트는 이번 판엔 안 만든다 — 두 파트가 서로 안 기다리게 하려는 의도적 단순화.

### 병합 순서
1. Part A 먼저 `main`에 병합 (씬 뼈대 확정 — Field/Camera/Player가 있어야 Part B가 마지막에 스포너를 얹을 자리가 생김)
2. Part B는 `main` 병합 후 최신 상태를 받아(`git pull origin main`) 자기 브랜치에 반영, 그 다음 **메인 씬에 스포너 오브젝트 하나만 추가**하는 작은 커밋으로 마무리 후 병합
3. 병합 후 플레이 테스트: 필드 안에 몬스터가 스폰되고, 공격 버튼으로 죽는지 확인

## 다음 할 일

**→ Part A는 HANDOFF.md 1번(필드 경계+카메라)부터.** 진행되는 대로 아래 로그와 체크리스트를 갱신한다.

## 체크리스트 (HANDOFF.md 개발 순서)

- [ ] 필드 경계 + 고정 카메라
- [ ] 캐릭터 컨트롤러 임포트 → 원형 스프라이트 + 색 적용
- [ ] 몬스터 스폰 로직 (HANDOFF.md 2번)
- [ ] 공격 판정 (HANDOFF.md 3번)
- [ ] 위 4개 다 붙어서 핵심 루프 한 번 플레이 가능

## 확인 필요 / 막힌 것

(없음)

## 로그 (최신이 위)

- **2026-08-25** — 레포 세팅 완료. `HANDOFF.md`(스펙), `CLAUDE.md`(작업 규칙), `reference/project_test.html`(원본 참고) 커밋. 코드 작업은 아직 시작 전.
