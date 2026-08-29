# 요괴전선 Unity 포팅 — 작업 규칙

이 파일은 이 레포에서 작업하는 모든 클로드 세션이 시작할 때 읽는 기본 수칙이다.

## 시작할 때 반드시 먼저 읽을 것 (순서대로)

1. **`PROGRESS.md`** — 지금까지 뭐가 됐고 다음에 뭘 해야 하는지. 여기서부터 이어서 작업한다.
2. **`HANDOFF.md`** — 현재 스프린트의 목표·범위·확정된 수치. 최종 목표(원본 전체 포팅)와 지금 단계가 뭔지 여기 적혀있다.
3. `reference/project_test.html` — 원본 게임 전체(완성본). **참고·수치 인용 전용, 절대 수정하지 않는다.** Unity 코드가 아니다.

## 작업 규칙

- **이건 프로토타입이 아니라 실제 구현이다.** HANDOFF.md 범위 안에 있는 기능이라면, "일단 대충 굴러가게 만들고 나중에 다듬는다"는 식으로 임의로 단순화하지 않는다 — 원본(`reference/project_test.html`)의 관련 메커니즘을 실제로 찾아 읽고, 세부 동작(물리·AI 상태·엣지케이스)까지 원본과 동일하게 구현하는 게 기본값이다. 의도적으로 원본과 다르게 갈 부분은 사용자가 명시적으로 지시했을 때만이다(예: 발판 층간 간격, C 전용 점프 — HANDOFF.md에 "의도적 편차"로 명시된 것들). **2026-08-26, 사용자가 몬스터 스폰/이동 로직에서 이걸 직접 지적함**(실제론 발판 스폰이 아니라 바닥에서만 스폰되고 있었는데 "발판에도 스폰된다"고 잘못 보고한 사례) — 애매하면 원본을 다시 읽어서 확인하고, 대충 짐작으로 "이 정도면 비슷하겠지"하고 넘어가지 않는다.
- **HANDOFF.md에 없는 시스템은 손대지 않는다.** 지금 스프린트 범위가 아니면, 아무리 원본에 있고 나중에 필요해도 지금 만들지 않는다. 범위는 스프린트마다 갱신된다.
- **기능 하나가 눈에 보이게 동작하면 그 단위로 커밋한다.** 하루 끝에 몰아서 커밋하지 않는다 — 다른 세션이 언제든 최신 커밋만 보고 상황을 파악할 수 있어야 한다.
- **커밋 전에 반드시 `PROGRESS.md`를 갱신한다.** 뭘 했는지, 뭐가 남았는지, 다음에 이어서 할 사람이 바로 알 수 있게. 형식은 파일 안의 기존 항목을 참고.
- 커밋 메시지는 무엇을 했는지 + (필요하면) 왜. 예: `몬스터 스폰 알고리즘 구현 — HANDOFF.md 2번 스펙 반영`
- 막히거나 HANDOFF.md 스펙이 애매하면 임의로 확장하지 말고 `PROGRESS.md`에 "확인 필요" 항목으로 남겨둔다.

### 문서 3종 동기화 (2026-08-26, 사용자 지적 — 이게 잘 안 지켜지고 있었음)
`CLAUDE.md`/`PROGRESS.md`/`HANDOFF.md`는 역할이 다르고, **커밋 하나가 셋 다 건드릴 필요는 없지만 각자의 트리거가 오면 반드시 그 자리에서 같이 갱신한다** — "나중에 몰아서" 하지 않는다. 세션 끝에 세 파일이 실제 코드 상태와 안 맞으면 다음 세션(또는 사용자)이 잘못된 전제로 작업을 시작하게 된다.
- **`PROGRESS.md`**: 매 커밋마다(위 규칙 그대로) — 로그 추가 + "지금 상태"/"다음 할 일"/"체크리스트"를 그 커밋 이후 사실에 맞게.
- **`HANDOFF.md`**: 원본과 대조해서 **수치·알고리즘·범위가 바뀔 때마다** — 스펙을 다시 읽는 사람이 최신 확정값을 보게. 의도적 편차는 `> **의도적 편차 —` 콜아웃으로.
- **`CLAUDE.md`**: (a) 작업 규칙 자체가 바뀔 때, (b) **스프린트/단계가 완료되거나 전환될 때 "지금 프로젝트 상태" 섹션을** — 이 섹션이 낡으면 새 세션이 시작하자마자 잘못된 다음 할 일을 고른다. 리팩터링·대형 마일스톤 완료 직후가 특히 놓치기 쉬운 시점이니 커밋 체크리스트에 넣을 것.
- 셋 중 하나라도 갱신을 빠뜨린 채 커밋했다는 걸 나중에 발견하면, 그 자리에서 바로 따라잡는 커밋을 만든다(다음 세션에 떠넘기지 않는다).

## 협업 규칙 (두 세션이 동시에 작업할 때)

Unity 씬(`.unity`)·프리팹(`.prefab`) 파일은 내부적으로 GUID/fileID로 오브젝트를 참조하는 구조라, **두 사람이 같은 씬을 동시에 건드리면 git이 병합하다가 씬이 깨진다.** 순수 C# 스크립트(`.cs`)는 일반 코드처럼 잘 병합된다. 이 차이 때문에 아래 규칙을 따른다.

1. **같은 씬 파일을 동시에 편집하지 않는다.** 씬에 손대는 작업은 항상 한 세션만, 순차적으로.
2. **작업은 스크립트/프리팹 단위로 나눈다.** 프리팹은 별도 에셋 파일이라, **새 프리팹을 만드는 것**은 메인 씬이나 다른 세션의 프리팹과 충돌하지 않는다. 단 아래 두 가지는 예외이니 주의:
   - **이미 있는 프리팹을 두 세션이 같이 수정하면 씬과 똑같이 충돌한다.** 공용 프리팹 수정은 한 번에 한 세션만.
   - **두 세션이 각자 같은 이름의 폴더를 새로 만들면 그 폴더의 `.meta`가 add/add 충돌한다** — 폴더 GUID가 서로 다르게 생성되기 때문. 실제로 겪음(2026-08-25, `Assets/Scripts.meta`·`Assets/Prefabs.meta`). 충돌 나면 **`main` 쪽 GUID를 채택**하고, 새 공용 폴더(`Assets/Data/` 등)가 필요하면 각자 만들지 말고 **`main`에 폴더만 먼저 만드는 작은 커밋**을 올린 뒤 각자 받아 쓴다.
3. **새 기능/새 스프린트를 시작할 땐 `feature/xxx` 브랜치에서 작업하고 `main`에는 직접 푸시하지 않는다.** 작업 끝나면 병합 순서를 정해서(보통 씬 뼈대를 만든 쪽이 먼저) 한 명씩 `main`에 합친다.
   - **예외**: 이미 `main`에 병합된 기능에 대한 사용자 피드백 기반의 작은 후속 수정(버그 수정, 수치 조정 등 — 새 기능 추가가 아닌 것)은 혼자 이어서 고치는 상황이라면 `main`에 바로 커밋해도 된다. 두 세션이 동시에 같은 영역을 만지고 있을 땐 이 예외를 쓰지 말 것.
   - **다른 세션이 PROGRESS.md 로그에 "고쳤다"고 적어놨어도, 그 브랜치를 받는 쪽은 실제 파일 diff(`git show <branch>:<path>`)로 재확인한다.** 로그와 실제 커밋 내용이 다른 경우가 실제로 있었다(2026-08-25, Part B 병합 시도).
4. **다른 세션의 코드와 맞닿는 지점(태그 이름, 클래스/메서드 시그니처, static 클래스 등)은 미리 문서로 고정해둔다.** 그날그날의 분업 내용과 인터페이스 계약은 `PROGRESS.md`의 "오늘의 분업" 섹션에 적는다 — 없으면 만들 것.
5. 병합 직전엔 항상 `git pull` 또는 `git fetch && git rebase origin/main`으로 최신 상태를 받은 뒤 자기 변경을 얹는다.
6. **`PROGRESS.md`는 매 커밋마다 모든 세션이 고치므로 병합할 때 거의 항상 충돌한다** — 사고가 아니라 정상이다. 아래 해결 규칙을 따른다:
   - **"로그" 섹션은 양쪽을 모두 남긴다.** 한쪽으로 덮어쓰지 말 것. 날짜/최신순만 맞춰 두 항목을 나란히 둔다.
   - **"지금 상태"·"오늘의 분업"·"체크리스트"는 병합하는 쪽이 병합 후의 사실에 맞게 새로 쓴다.** 양쪽 문장을 기계적으로 이어붙이면 "Part B 진행 중"과 "Part B 완료"가 같이 남는 모순이 생긴다.
   - 충돌 마커(`<<<<<<<`)를 지운 뒤 **반드시 파일 전체를 다시 읽어** 앞뒤가 안 맞는 문장이 남았는지 확인한다.
7. **Unity 에디터를 열어둔 세션이 있으면 다른 세션은 배치모드 검증(`-batchmode`, 테스트 실행)을 할 수 없다** — 프로젝트 폴더가 잠겨서 크래시한다(실제로 겪음, 2026-08-25). 대응:
   - 배치 검증이 필요한데 잠겨 있으면, **에디터를 연 세션에게 닫아달라고 요청**하거나 **레포를 다른 폴더에 clone해서 거기서 실행**한다(`Library/`가 새로 생성돼 첫 실행은 느림).
   - 에디터를 장시간 열어둘 거면 `PROGRESS.md`의 "확인 필요"에 남겨 다른 세션이 헛수고하지 않게 한다.
   - **검증 못 한 채로 "됐다"고 커밋하지 않는다.** 검증이 막혔으면 그 사실을 커밋 메시지와 `PROGRESS.md`에 명시한다.

## 코딩/파일 정리 규칙

최종 목표(캐릭터 4종·가챠·아이템 35종·지역 9종·윤회까지 포팅)를 감안하면 파일 수가 많이 늘어난다. 지금(스크립트 10개 이하)은 아직 안 아프지만, 여기서 정한 기준은 **"지금 하기 귀찮아도 나중에 편한" 쪽으로 고른 것**이다 — 나중에 "번거롭다"며 되돌리지 말 것.

**새로 만드는 코드는 지금부터 이 기준을 따른다.** 기존 코드를 여기 맞추는 작업(폴더 이동·네임스페이스 추가·asmdef 분리)은 아래 절차로 한 번에 한다.

### 대규모 리팩터링 절차 (폴더 이동·네임스페이스·asmdef 분리)
이런 작업은 **거의 모든 파일을 건드리므로 진행 중인 다른 작업 전부와 충돌한다.** 조금씩 나눠서 하면 오히려 충돌이 여러 번 난다. 그래서:
1. **다른 세션의 미병합 브랜치가 전부 `main`에 병합된 상태에서 시작한다.** 미병합 작업이 있으면 그게 끝날 때까지 기다린다.
2. 착수 전 `PROGRESS.md`에 **"리팩터 진행 중 — 다른 세션은 커밋 금지"**를 명시하고 커밋해서 알린다.
3. 한 세션이 단독으로, **한 번의 커밋**으로 끝낸다(파일 이동은 `git mv`를 써서 rename으로 추적되게).
4. 끝나면 즉시 `main`에 병합·푸시하고 선언을 해제한다. 리팩터 브랜치를 오래 열어두지 않는다.
5. 반드시 **배치모드 컴파일 + PlayMode 테스트 전체 통과**를 확인하고 병합한다(Unity 에디터가 잠겨 있으면 착수하지 말 것 — 검증 없이 대규모 이동은 금지).

### 폴더 구조
- `Assets/Scripts/`는 도메인별 하위 폴더로 나눈다. **폴더 이름 = 네임스페이스 = asmdef 이름**으로 셋을 항상 일치시킨다(아래 asmdef 계층표와 같은 구분).
  - `Core/` — 여러 도메인이 같이 쓰는 것(`FieldBounds`, `ISpawnProtectable`, `GameInput`, `IDamageable`, 데이터 SO 기본 타입, `PlayerProfile` — 뒤 셋은 아직 미구현. `PlayerProfile`은 SO가 아님, 아래 "세이브 데이터 vs 설정 데이터" 참고)
  - `World/` — 필드·카메라·발판
  - `Combat/` — **무기에 종속되지 않는 전투 공용 인프라**(투사체 기반 클래스, 피해 판정 헬퍼)
  - `Characters/` — 플레이어 컨트롤러 + **캐릭터별 무기 구현**(`MageAttack`·`MageProjectile`은 마법사 무기이므로 여기)
  - `Enemies/` — 적 AI·이동
  - `Systems/` — 스포너·가챠·세이브 등 오케스트레이션
  - `UI/` — HUD·메뉴
- `Assets/Tests/PlayMode/`도 같은 도메인 이름으로 하위 폴더를 맞춘다.
- `Assets/Prefabs/`, `Assets/Sprites/`도 `Characters/`, `Enemies/`, `Items/` 등으로 나눈다 — 다만 그 종류가 실제로 2개 이상 생기는 시점에 폴더를 만들면 됨(지금 빈 폴더를 미리 만들 필요는 없음).

### 네임스페이스
- 모든 런타임 스크립트는 `YokaiFront.<도메인>` 네임스페이스를 쓴다(예: `YokaiFront.Combat`, `YokaiFront.Enemies`, `YokaiFront.World`). 폴더 구조와 네임스페이스 이름을 일치시킨다.
- Editor 스크립트는 `YokaiFront.Editor`, 테스트는 `YokaiFront.Tests.PlayMode`.
- 모든 스크립트가 이미 이 규칙대로 네임스페이스가 붙어 있다(2026-08-26 리팩터링으로 적용 완료).

### Assembly Definition 경계 — 계층 순서로 정의
"형제끼리 참조 금지"만으로는 실제 코드가 바로 위반된다(`EnemySpawner`는 적을 스폰해야 하고, 무기 스크립트는 투사체 인프라를 써야 함). 그래서 **단순 계층(낮은 층은 높은 층을 모른다)**으로 정의한다:

| 층 | asmdef | 참조 가능 대상 | 들어가는 것 |
|---|---|---|---|
| 0 | `YokaiFront.Core` | (없음) | `FieldBounds`, `ISpawnProtectable`, `GameInput`(미구현), `IDamageable`(미구현), 데이터 SO 기본 타입, `PlayerProfile`(미구현) |
| 1 | `YokaiFront.World` · `YokaiFront.Combat` | 0 | 필드/카메라/발판 · 투사체·피해판정 공용 인프라 |
| 2 | `YokaiFront.Characters` · `YokaiFront.Enemies` | 0~1 | 플레이어 컨트롤러·무기별 공격 · 몬스터 AI |
| 3 | `YokaiFront.Systems` | 0~2 | 스포너·가챠·세이브 등 오케스트레이션 |
| 4 | `YokaiFront.UI` | 0~3 | HUD·메뉴 |

- **같은 층끼리는 서로 참조 금지.** 특히 `Characters` ↔ `Enemies`는 절대 직접 참조하지 않는다 — 서로 때리는 건 `Core`의 태그(`CompareTag("Enemy")`)와 `ISpawnProtectable` 같은 `Core` 인터페이스로만 한다. `IDamageable`은 아직 없음(Health 시스템과 함께 나중에 `Core`에 추가 예정) — 그 전까지 공격 스크립트는 `Destroy()`를 직접 부른다.
- **낮은 층이 높은 층의 기능을 "요청"해야 하는 경우(예: 몹이 죽을 때 새 몹을 스폰)는 직접 참조하지 않고 `Core`의 이벤트로 방향을 뒤집는다.** 이 상황이 실제로 곧 온다 — 원본은 분열귀(splitter) 타입이 죽으면 `spawnEnemyAt()`을 직접 호출해 새끼 2마리를 낳는다(`project_test.html:1840`). 지금 저장소에서 `Assets/Scripts/Systems/YokaiFront.Systems.asmdef`를 실제로 열어보면 이미 `"YokaiFront.Enemies"`를 참조하고 있다(3층→2층, 스포너가 몹 프리팹을 다뤄야 하니 당연함) — 그런데 `splitter` 같은 몹을 만들려고 `Enemies`가 거꾸로 `Systems`(스포너)를 참조하면 두 asmdef가 서로를 참조하는 순환참조가 되어 Unity가 "circular assembly definition reference"로 **컴파일 자체를 거부한다**(스타일 위반이 아니라 빌드가 깨짐).

  **확정된 해결 패턴 — 이 시그니처 그대로 쓸 것**(`Combat`/`Characters`가 `Enemies`를 몰라도 스폰 무적 상태를 물을 수 있게 만든 `ISpawnProtectable`, `Core/ISpawnProtectable.cs`와 정확히 같은 구조 — 구체 타입 대신 `Core`의 추상화만 아래층이 참조):
  ```csharp
  // Core/EnemySpawnRequestBus.cs — namespace YokaiFront.Core
  public static class EnemySpawnRequestBus
  {
      public static event Action<Vector2, string> Requested; // (스폰 위치, 몹 타입 식별자 — 원본 'splitlet' 같은 문자열 키)
      public static void Request(Vector2 position, string enemyTypeId) => Requested?.Invoke(position, enemyTypeId);
  }
  ```
  - `Enemies`(예: 나중에 만들 `EnemySplitOnDeath.cs`)는 죽는 순간 `EnemySpawnRequestBus.Request(deathPos, "splitlet")`만 호출한다 — `Systems`를 몰라도 됨(`Core`만 참조, 이미 허용된 방향).
  - `Systems.EnemySpawner`는 `OnEnable()`에서 `EnemySpawnRequestBus.Requested += HandleSpawnRequest;`, **`OnDisable()`에서 반드시 `-=`로 구독 해제**(안 하면 오브젝트가 파괴된 뒤에도 정적 이벤트가 참조를 들고 있어 `MissingReferenceException`/메모리 누수로 이어진다). `HandleSpawnRequest(Vector2 pos, string enemyTypeId)`가 타입id→프리팹 매핑 후 실제 `Instantiate`를 담당한다(이미 `monsterPrefab` 필드로 프리팹을 들고 있는 클래스라 이 매핑을 갖기 자연스러운 위치).
  - **지금 이 파일을 만들지는 않는다** — splitter류 다종 몹은 HANDOFF.md 범위 밖이라 "HANDOFF.md에 없는 시스템은 손대지 않는다" 규칙대로, 실제로 몹 종류를 늘리는 스프린트가 시작될 때 위 시그니처 그대로 만든다(시그니처 자체는 이미 확정이니 그때 재설계하지 말 것).
- **위 표는 실제 적용 완료 상태다(2026-08-26 리팩터링).** `Assets/Scripts/`가 여섯 asmdef로 분리돼 있고 `YokaiFront.Runtime`은 더 이상 없다.

### 데이터(밸런스 값) — ScriptableObject로
- 무기·적 스탯 같은 튜닝 수치는 스크립트에 하드코딩하지 않고 ScriptableObject 데이터 에셋으로 관리한다(원본의 `CONFIG`/`WEAPONS`/`MONSTERS` 데이터 테이블 구조를 그대로 반영하는 것 — 원본 자체가 이미 데이터 우선 설계였다).
- 예: `WeaponData`(쿨다운·데미지·차지 배율), `EnemyData`(체력·공격력·이동속도). 스크립트는 데이터를 읽기만 하고 로직만 담당.
- **SO는 "수치"만 책임진다 — 타입마다 "행동"이 다르면 SO 하나로 안 끝난다.** 원본을 실제로 확인하면 몹 종류가 최소 6종(`wisp`/`oni`/`charger`/`shooter`/`splitter`/`bigOni`, `project_test.html:3933` `rollSpawnType`)이고 스탯만 다른 게 아니라 로직 자체가 다르다 — wisp는 날아다니고(`:3956`, y좌표를 지면이 아니라 `y-50`으로 스폰), charger는 돌진 상태머신(`:4061`), shooter는 투사체를 쏘고(`:4084`), splitter는 죽을 때 새끼를 낳는다(`:1840`, 위 "낮은 층이 높은 층을 요청" 항목 참고). `Enemies/`에 새 몹 종류를 추가할 때 `EnemyMove` 하나에 분기를 계속 늘리지 말고, `Characters/`가 무기마다 이미 하고 있는 패턴(별도 스크립트, `MageAttack`/`MageProjectile`) 그대로 **타입 전용 스크립트를 추가**하고 `EnemyData`는 그 스크립트들이 공통으로 읽는 수치만 담당하게 한다. 단순히 스탯만 다른 타입(예: `bigOni`는 `CONFIG.enemyBase`에 자기 stat row만 따로 있고(`:710`, hp/dmg/speed/w/h), 넉백 저항 배율 하나만 별도 분기(`:1678`)일 뿐 이동/AI 로직은 일반 오니와 완전히 같다 — `updateEnemies()`에 `e.type === 'bigOni'`로 갈리는 이동 로직이 전혀 없음, 색과 그리기 함수 분기(`:4797`)만 다름)은 기존 `EnemyMove` + 다른 `EnemyData` 값으로 충분 — 새 스크립트는 **행동 자체가 다를 때만** 만든다.
- **여기서 지금 못박는 건 이 원칙 하나뿐이다**("SO엔 수치만, 행동이 다르면 스크립트를 분리한다"). `wisp`/`charger`/`shooter`/`splitter`를 정확히 몇 개의 클래스로, 어떤 이름으로 나눌지·공통 인터페이스가 필요한지는 **몹 종류를 실제로 추가하는 스프린트가 시작될 때 그 스프린트의 HANDOFF.md에서 확정한다.** 지금 미리 스크립트 이름까지 정하면 그 스프린트가 실제로 몇 종을 어떤 순서로 다룰지(사용자가 아직 안 정함, `PROGRESS.md` "다음 할 일" 참고)와 어긋날 수 있다 — "HANDOFF.md에 없는 시스템은 손대지 않는다" 규칙과 같은 이유로, 이름/개수까지는 지금 정하지 않는다.
- 데이터 에셋 위치: `Assets/Data/Weapons/`, `Assets/Data/Enemies/`.
- 지금(`MageAttack`이 무기 1종)은 필드 하드코딩 그대로 둔다. **2번째 무기/적 종류를 만들기 전에, SO 전환을 먼저 끝내고 병합한다** — 2종을 하드코딩으로 만든 뒤 전환하면 두 배로 뜯어고쳐야 하므로 순서가 중요하다.
- **SO 전환은 한 세션이 단독으로 수행하고, 그동안 다른 세션은 해당 도메인 파일을 건드리지 않는다**(전환이 여러 파일을 동시에 바꾸므로). 착수 전 `PROGRESS.md`에 "SO 전환 진행 중 — 이 도메인 손대지 말 것"으로 선언한다.

### 세이브 데이터 vs 설정 데이터 (혼동 주의 — 위 SO 규칙과 다른 카테고리)
원본의 `meta` 오브젝트(`project_test.html:1129` `function defaultMeta()`)를 실제로 읽어보면 레벨·골드·**아이템 보유 개수**(`meta.items`, `:1148` "// { 아이템id: 보유 개수 }")·윤회 횟수·지역별 진행·업적·설정까지 전부 담겨 있고, `localStorage.setItem(SAVE_KEY, JSON.stringify(meta))`(`:1214`)로 저장된다 — **플레이어마다 다르고 런타임에 계속 바뀌는 진행 상태**다. 이건 위 "데이터(밸런스 값)" 규칙이 말하는 것과 다른 카테고리라 섞으면 안 된다:
- **설정 데이터**(정적, 모든 플레이어 동일, 기획자가 에디터에서 미리 튜닝) → ScriptableObject. 예: `WeaponData`, `EnemyData`, 그리고 아이템의 "정의"(`ItemDefinition` — 원본 `ITEMS` 테이블 `:759`의 `nm/icon/grade/kind/stat/per`처럼 등급·효과 공식 자체는 고정값이라 SO가 맞다).
- **세이브 데이터**(가변, 플레이어마다 다름, 파일로 직렬화) → SO가 아니다. `PlayerProfile` 같은 **순수 직렬화 가능 C# 클래스**(`[System.Serializable]`, `MonoBehaviour`도 `ScriptableObject`도 아님)로 관리하고 `Systems/`의 세이브 시스템이 파일 입출력을 담당한다. 예: 레벨·골드, **아이템 보유 개수**(정의가 아니라 "몇 개 갖고 있나"), 윤회 횟수, 지역별 진행도, 달성한 업적, 설정값.
- **SO를 세이브 데이터에 쓰면 안 되는 이유**: 에디터에서 SO 값이 바뀌면 그 변경이 에셋 파일에 영구 반영된다 — 런타임에 계속 바뀌는 플레이어 진행 상태를 SO로 관리하면 에디터에서 플레이 테스트하다가 실수로 에셋 자체를 오염시키기 쉽고, 애초에 "플레이어마다 다른 값 여러 벌"이라는 걸 SO 하나로 표현할 수 없다.
- `PlayerProfile`은 `Core/`에 둔다(모든 도메인이 자기 슬라이스를 읽어야 함 — `Characters`는 레벨/골드, `Systems`는 아이템/윤회/지역, `UI`는 표시용으로 전부). 실제 파일 입출력(직렬화·역직렬화)은 `Systems/`의 세이브 시스템이 담당 — `Core`는 데이터 형태만 정의하고 로직은 `Systems`(현재 계층표 그대로, 구조 변경 필요 없음).
- **여기서 지금 못박는 것도 카테고리 분류뿐이다**("SO 아님, `Core`에 위치하는 순수 직렬화 클래스"). `PlayerProfile`의 정확한 필드 목록(레벨·골드·아이템 보유량·윤회 횟수·지역 진행·업적 중 정확히 무엇이 언제 추가되는지)은 **Health/아이템/가챠/윤회 각 스프린트가 실제로 시작될 때 그 스프린트의 HANDOFF.md에서 그때그때 확정한다.** 원본 `meta`(`:1129`)를 지금 통째로 옮겨적지 않는다 — 그러면 아직 스코프에 없는 시스템(가챠·윤회 등)까지 미리 설계하게 되어 "HANDOFF.md에 없는 시스템은 손대지 않는다" 규칙과 어긋난다.

### 입력 처리
- 원본 `KEYMAP`처럼, 모든 키 입력은 `GameInput`이라는 중앙 정적 클래스를 통해서만 읽는다(`GameInput.Jump`, `GameInput.Attack`, `GameInput.Left` 등 named 프로퍼티). 스크립트에서 `Input.GetKey(KeyCode.X)`를 직접 호출하지 않는다.
- 새 액션(스킬키 등)이 추가될 때 이 클래스 하나만 고치면 되게.

### 데미지/피격 인터페이스
- 피격 가능한 대상(플레이어·적)은 `IDamageable`(`TakeDamage(float amount, GameObject source)`)을 구현한다.
- 공격 스크립트는 `Destroy()`를 직접 부르지 않고 `IDamageable.TakeDamage()`를 호출한다. v0(Health 시스템 없음)에서는 구현체가 그냥 `Destroy(gameObject)`만 해도 됨 — Health가 생기면 그 구현체 하나만 바꾸면 됨.

### 용어 통일 — `Enemy`로 완료(2026-08-26)
같은 대상을 `Monster`(클래스명)·`Enemy`(태그)·`Enemies`(폴더)로 따로 부르던 걸 **`Enemy`로 통일 완료**(원본 JS도 `enemies`를 씀).
- 태그 `Enemy` · 폴더 `Enemies/` · 네임스페이스 `YokaiFront.Enemies` · 클래스 `EnemySpawner`/`EnemyMove` · 프리팹 `Enemy_Oni.prefab` — 전부 적용 끝. 새 코드에서 `Monster`라는 이름을 다시 쓰지 말 것.
- 에셋 파일명은 `<도메인>_<고유이름>` 형식: `Enemy_Oni.prefab`, `Weapon_Bow.asset`, `Region_Forest.asset`.

### 번호/이름 예약표 (새로 추가하기 전에 여기 먼저 적고 커밋)
인덱스로 관리되는 Unity 설정은 두 세션이 같은 슬롯을 서로 다른 용도로 잡으면 병합해도 조용히 깨진다. **표에 먼저 예약하는 커밋을 올린 뒤 구현한다.**

**물리 레이어** (`ProjectSettings/TagManager.asset`)
| 번호 | 이름 | 용도 |
|---|---|---|
| 6 | Player | 플레이어. **Enemy와의 Physics2D 충돌만 끔**(Layer Collision Matrix), Ground와는 충돌 유지 |
| 7 | Enemy | 적 전체. **Player와의 Physics2D 충돌만 끔**(Layer Collision Matrix), Ground와는 충돌 유지 |
| 8 | Ground | 바닥·발판(접지 판정) |

> **2026-08-29, 사용자가 직접 플레이해보고 지적 — 플레이어·몬스터가 서로 겹치지 못하고 밀어냄.** 원본(`project_test.html:4148`)은 물리엔진 자체가 없어 `rectsOverlap()`로 접촉만 검사해 `damagePlayer()`를 부르고 끝이라, 겹쳐도 밀어내는 로직이 없다. Unity 쪽은 발판 착지를 위해 Player·Enemy 둘 다 `Rigidbody2D`(Dynamic)+막힌(non-trigger) `CircleCollider2D`를 쓰는데, 둘이 같은 물리 레이어(`Default`, 0번)에 있어서 겹치는 순간 Physics2D가 자동으로 밀어내기를 해버린 게 원인 — 원본에 없는, 물리엔진 도입 부작용이었다. 콜라이더를 트리거로 바꾸면 발판 충돌까지 깨지므로, 대신 위 `Player`(6)/`Enemy`(7) 레이어를 신설해 **Layer Collision Matrix에서 Player×Enemy만 비활성화**(Player×Ground, Enemy×Ground는 유지)하는 방식으로 해결. 접촉 데미지 판정 자체는 원래도 물리 충돌과 무관하게 `EnemyMove.OverlapsTarget()`의 수동 거리 계산으로 처리되고 있어서 별도 수정 불필요했음.

**태그**
| 이름 | 용도 |
|---|---|
| Player | 플레이어(Unity 기본 제공) |
| Enemy | 적 전체 — 공격 판정이 이 태그로 대상을 고른다 |

**렌더 정렬 순서(`sortingOrder`)** — 지금 코드에 매직넘버로 흩어져 있다(-1·3·4·5). 새 값을 쓰기 전에 여기 적을 것:
| 값 | 용도 |
|---|---|
| -1 | 바닥·발판 |
| 0 | 캐릭터·적(기본) |
| 3 | 투사체 |
| 4 | 차지 인디케이터 |
| 5 | 공격 판정 표시(링) |

### 테스트
- `Rigidbody2D`/`Collider2D`가 얽힌 로직(이동·공격 판정·발판 등)은 PlayMode 테스트 필수.
- 순수 계산/데이터 클래스는 EditMode 테스트로 충분하거나 생략 가능.
- **`main`에 병합하기 전에 전체 테스트를 돌려 통과를 확인한다.** 자기가 추가한 테스트만 보고 병합하지 말 것 — 다른 도메인 테스트가 깨졌는지는 전체 실행으로만 알 수 있다.
- 실행: `Unity.exe -batchmode -nographics -projectPath <경로> -runTests -testPlatform PlayMode -testResults <경로>\test_results.xml -logFile <경로>\test.log` (결과 xml/로그 파일명은 `.gitignore`에 걸리는 패턴을 쓸 것)

### 문서 구조
- `HANDOFF.md`는 스프린트가 늘어나면 `docs/sprints/01-combat-core.md`처럼 스프린트별 파일로 분리한다(다음 스프린트 시작 시점에 실행 — 지금 당장 옮기지 않음, 진행 중인 작업의 파일 경로를 바꾸지 않기 위해).
- 원본과의 의도적 편차는 항상 `> **의도적 편차 —`로 시작하는 콜아웃으로 통일해서 문서 전체에서 grep 가능하게 남긴다.

### 월드 스케일 — 원본 수치를 가져올 때 (실제 버그 발생 지점)
- **원본(`project_test.html`)의 픽셀 단위 수치는 Unity로 옮길 때 반드시 ÷100 한다** (100px = 1유닛). 이동속도 270→2.7, 점프 960→9.6, 중력 2600→26, 몹 이동속도 76→0.76.
- **원본 Y+는 화면 아래 방향, Unity Y+는 위 방향이라 세로 수치는 부호가 반대다.** 점프/중력 값을 옮길 때 주의.
- 이 축척을 빼먹은 버그가 실제로 두 번 났다(몹 `moveSpeed`를 76 그대로 써서 26유닛 필드를 0.34초에 주파). **원본 수치를 코드에 넣을 땐 주석에 원본값과 환산식을 같이 남긴다** — 예: `public float moveSpeed = 0.76f; // 원본 오니 76px/s ÷100`.

### 에셋 임포트 (PPU)
- **스프라이트의 `pixelsPerUnit`은 "텍스처 픽셀 크기 ÷ 그 스프라이트가 차지해야 할 유닛 크기"로 정한다.** 무조건 100이 아니다.
  - 일반 아트 리소스: 100px = 1유닛 규칙에 맞춰 그렸다면 `100`.
  - 절차 생성 프리미티브: 의도한 유닛 크기에 맞춘 값. 예) `Circle.png`는 128px 텍스처로 "스케일 1 = 지름 1유닛"을 의도했으므로 PPU = **128**(`BuildPartAScene.EnsureCircleSprite` 참고).
- **"전부 100" 같은 일괄 규칙으로 바꾸지 말 것** — Circle.png를 100으로 바꾸면 지름이 1.28유닛이 되어 `CircleCollider2D.radius = 0.5`(=1유닛)와 시각/물리 크기가 어긋난다. 기준은 PPU 숫자가 아니라 **"보이는 크기 == 콜라이더 크기"**다.

## 지금 프로젝트 상태

- Unity: 2D (URP) 템플릿
- 진행 단계: **1단계 전투 코어 프로토타입 완료 + 대규모 리팩터링 완료(2026-08-26).** Part A(필드/카메라/플레이어/공격) + Part B(몬스터/스폰) 전부 `main`에 병합·검증·`origin` push 완료. HANDOFF.md "개발 순서 제안" 1~5번 전부 체크 완료 — "핵심 루프"가 돌아가는 상태. 이어서 **"코딩/파일 정리 규칙"의 폴더/네임스페이스/asmdef 분리 + `Monster`→`Enemy` 리네임을 실제 코드에 적용 완료** — 더 이상 "규칙만 있고 코드는 평평한" 상태가 아니다. `Assets/Scripts/`는 `Core/World/Combat/Characters/Enemies/Systems` 여섯 폴더 = 여섯 네임스페이스(`YokaiFront.*`) = 여섯 asmdef로 실제 분리돼 있고, 계층 참조 규칙(하위 asmdef가 상위를 모름)도 강제된다. 클래스명은 `EnemyMove`/`EnemySpawner`, 프리팹은 `Enemy_Oni.prefab`. 자세한 건 `PROGRESS.md` 참조.
- **배치모드 `-executeMethod`는 이제 전체 네임스페이스 경로가 필요하다**: `YokaiFront.Editor.BuildPartAScene.Build` (리팩터 전엔 `BuildPartAScene.Build`였음 — 네임스페이스 없는 옛 명령어를 쓰면 "class could not be found"로 실패한다).
- **다음 스프린트(신규 기능)는 아직 미정이다.** HANDOFF.md 맨 아래 "여기까지 되면 핵심 루프 완성 — 이후 확장(체력바, 몹 종류 추가 등)은 따로 논의"라고 명시돼 있다 — 세션이 임의로 다음 스프린트 범위를 정하지 말고 사용자에게 확인할 것. 최종 목표(HANDOFF.md 맨 위) 기준 후보: 플레이어 Health/체력바, 몹 종류 추가, 무기 2종째(그리고 ScriptableObject 데이터 전환 — 위 "데이터(밸런스 값)" 절 참고, 이건 아직 안 함), 아이템, 가챠, 지역 등.
- 게임 로직 검증은 항상 PlayMode 테스트로 한다(`Assets/Tests/PlayMode/`, 도메인별 하위 폴더로 분리됨). Edit Mode 배치 실행에서 Physics2D 쿼리를 신뢰하지 말 것 — 이유는 `PROGRESS.md` 로그 참고.
