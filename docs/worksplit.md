# 분업안 (living document)

> **이 파일이 분업의 유일한 출처다.** 채팅에서 정한 분업은 여기 적히기 전까지 존재하지 않는 것으로 친다.
> 형식 규칙은 `CLAUDE.md` "분업안 — 작성 위치와 형식". 완료 항목은 지우지 말고 `✅`로 표시한다.

## 1. 머리말

- **확정 날짜**: 2026-09-09 (회의 결과로 **순서 전면 재편** — 이전 판은 아래 "9. 이전 계획" 참고)
- **근거 문서**: [`docs/original-parity.md`](original-parity.md) · 원본 `reference/project_test.html`
- **참여**: 나(사용자 세션) / 팀원(다른 클로드 세션). 밸런스 조정은 기획자 상윤 — **우리는 원본 수치 그대로 이식만 한다.**

### 새 진행 방식 (회의 확정)

```
단계 1  모든 캐릭터를 "0차"까지 + 모든 몬스터를 프로토타입 그대로   ← 지금 여기
단계 2  캐릭터 하나씩 깊게 5차까지
단계 3  완성된 캐릭터부터 하나씩 밸런스 조정
```

**"0차"란 = 기본공격만 있는 상태.** 전문화(스킬트리)를 아직 안 찍은 상태로, 원본에서 `tier <= 0`이면
X 스킬이 막히고(`tryMageSkill` `:2169`, `tryBladeSkill` `:2741`, `tryDruidSkill` `:3385`)
**Z 기본공격 + 그 캐릭터의 이동 방식**만 남는다. 티어 보너스도 전부 빠진다.
(기본공격이 채우는 자원 — 메카닉 스택·드루이드 마나 — 은 기본공격의 일부라 같이 이식하되,
소비처가 전부 X 스킬이라 0차에선 쌓이기만 한다. 원본도 동일.)

> **마법사는 이미 5차까지 끝났다**(폭발·중력 두 갈래 전부). 되돌리지 말고 그대로 둔다 —
> 단계 2의 "첫 번째 깊게 판 캐릭터"가 이미 마법사인 셈이다.

---

## 2. 착수 전제 (선행 작업 — 내가 먼저 처리, 팀원은 대기)

캐릭터를 4종으로 늘리려면 **플레이어 오브젝트 뼈대**가 먼저 있어야 한다. 팀원의 섬영/드루이드는
이 뼈대에 컴포넌트를 얹는 방식이라, 아래가 `main`에 올라간 뒤 착수한다.

1. `Core/CharacterId.cs` — 캐릭터 4종 enum + 캐릭터별 스탯 배수(원본 `CHAR_STAT_MULT` `:1282`)
2. `Characters/CharacterMover2D`에 **관성 이동 모드** 추가 (섬영용, 원본 `bladeMove` `:2454`)
3. `Characters/PlayerRig.cs` — 선택된 캐릭터의 키트만 켜는 전환기
4. `Core/PlayerStatCalculator`에 캐릭터 배수 반영

**팀원은 이 커밋이 `main`에 올라갔다는 연락을 받은 뒤 시작할 것.**
(몹 작업은 이 뼈대와 무관하므로 언제든 시작 가능 — 다만 이번 판에서 몹은 내 담당으로 옮겼다, 4절 참고)

---

## 3. 공유 계약 (임의로 이름/타입 바꾸지 말 것)

### `Core/CharacterId.cs` — 내가 소유
```csharp
namespace YokaiFront.Core
{
    /// 원본 CHARACTERS(:848) / WEAPONS(:841). 캐릭터 = 무기 1:1이라 하나로 합쳤다.
    public enum CharacterId { Mage, Gunner, Blade, Druid }

    public static class CharacterStats
    {
        /// 원본 CHAR_STAT_MULT(:1282) — 섬영만 1.5. statAtk·statMaxHp에만 곱한다(ms/as/crit엔 안 곱함).
        public static float StatMultiplier(CharacterId id) => id == CharacterId.Blade ? 1.5f : 1f;
    }
}
```

### `Characters/CharacterMover2D` — 내가 소유, **섬영이 모드만 바꿔 쓴다**
```csharp
public enum MoveMode { Instant, Inertial }   // Instant=마법사/메카닉, Inertial=섬영

public MoveMode moveMode = MoveMode.Instant;

// --- 관성 모드 파라미터 (원본 CONFIG.blade :613, 100px=1유닛) ---
public float inertialBaseSpeed = 3.0f;   // baseSpeed 300
public float inertialMaxSpeed  = 9.0f;   // maxSpeed 900
public float inertialAccel     = 5.2f;   // accel 520
public float inertialDecel     = 9.0f;   // decel 900

/// 관성 모드에서 이동 파라미터 전체에 곱해지는 배율. 섬영은 min(1, statMs)를 넣는다
/// (원본 bladeMsK() — 이속 100%를 넘는 분은 속도가 아니라 피해로 간다).
public float MoveScale = 1f;
/// 가속에만 추가로 곱해지는 배율. 원본은 가속을 이속의 **제곱**으로 키우고(bladeAccelK),
/// 집중(focus) 중엔 여기에 더 곱한다 — 티어 구현 시 섬영 키트가 매 프레임 세팅한다.
public float AccelMultiplier = 1f;

/// 마지막으로 달리던 방향(원본 p.runDir). 관성 모드의 가속/반전 판정에 쓰인다.
public int RunDir { get; }
```
⚠️ **관성 모드의 방향 전환은 감속이 아니라 "속도를 유지한 채 즉시 반전"이다**(원본 `:2469-2474`).
`CONFIG.blade.brake`(2200)는 정의만 있고 실제로 안 쓰이는 죽은 값이니 이식하지 말 것.

### `Characters/PlayerRig.cs` — 내가 소유, **팀원은 자기 키트를 등록만 한다**
```csharp
/// 선택된 캐릭터의 키트 컴포넌트만 켜고 나머지는 끈다(원본 updatePlayer의 무기별 분기 :3509~3511).
public class PlayerRig : MonoBehaviour
{
    public CharacterId Current { get; }
    public void Select(CharacterId id);   // 키트 enable/disable + 이동 모드 + 스탯 배수 갱신
}

/// 캐릭터 키트가 구현하는 계약. PlayerRig가 이걸로 켜고 끈다.
public interface ICharacterKit
{
    CharacterId Character { get; }
    void OnSelected();     // 이 캐릭터로 전환됐을 때(자원 초기화 등)
    void OnDeselected();
}
```
팀원은 `BladeCombat`/`DruidAttack`이 `ICharacterKit`을 구현하고 플레이어 오브젝트에 붙이기만 하면 된다.

### 이미 있는 것 (다시 만들지 말 것)
| 파일 | 용도 | 소유 |
|---|---|---|
| `Core/GameInput` | **모든 키 입력.** `Input.GetKey`를 스크립트에서 직접 부르지 말 것 | 공용 |
| `Core/IDamageable`(+`TakeDamageWithKnockback`) | 피해 전달 | 공용 |
| `Core/IElementAfflictable` | 화상(`ApplyBurn`). 출혈은 섬영 담당자가 추가 | 공용 |
| `Core/PlayerStatCalculator` | 파생 스탯 5종 | 나 |
| `Core/GameState`·`RunState`·`CombatEvents`·`IRunResettable` | 런 사이클 계약(구현은 나중) | 나 |
| `Combat/DamageCalculator` | 치명타·±10% 난수 | 공용 |
| `Enemies/EnemyHealth.SetMaxHp`/`Died` | 스탯 주입·처치 알림 | 공용 |

⚠️ **단골 함정**: `Instantiate`/`AddComponent` 직후 필드만 바꾸면 이미 실행된 `Awake()`에 반영이 안 된다.
적 체력은 반드시 `EnemyHealth.SetMaxHp()`로 바꿀 것(이 함정으로 이미 3번 깨졌다).

---

## 4·5·6. 트랙별 담당 / 건드리는 파일 / 완료 정의

> **이번 판에서 몹 담당이 바뀌었다.** 팀원이 캐릭터 2종(섬영·드루이드)을 맡으므로 균형상 몹 7종·엘리트를
> 내가 가져온다. 도메인이 갈려서(나=`Enemies/`, 팀원=`Characters/Blade*`·`Druid*`) 오히려 충돌이 없다.

### 나 — 뼈대 · 메카닉 · 몬스터 전부

**건드리는 파일**: `Core/` 전체, `Characters/CharacterMover2D`·`PlayerRig`·`Gunner*`·`MageAttack`,
`Enemies/` 전체, `Systems/EnemySpawner`, **`Assets/Scenes/CombatCore.unity` + `Assets/Editor/BuildPartAScene.cs`(씬 소유권 = 나)**

**건드리면 안 되는 파일**: `Characters/Blade*`, `Characters/Druid*`(팀원이 새로 만드는 파일)

| # | 항목 | 원본 | 완료 정의(DoD) |
|---|---|---|---|
| 1 | **착수 전제** — `CharacterId`+스탯배수, 관성 이동 모드, `PlayerRig`+`ICharacterKit`, 캐릭터 전환(임시 디버그 키) | `:1282`, `:2454`, `:3509` | 팀원이 키트를 붙일 수 있는 상태로 `main` 병합 + 연락 |
| 2 | **메카닉 0차** — Z 총알(속도 13.2, 수명 0.62, 피해 0.82, 관통 1, cd 0.24/statAs), 적중 시 스택 +1 | `gunnerFire` `:2188` | 마법사와 전환해가며 둘 다 쏴진다 |
| 3 | **몹 6종 추가** — wisp(비행·수직추적), bigOni(넉백저항 0.4), charger(조준→돌진→탈진 상태기계), shooter(거리유지+투사체), splitter(죽으면 새끼 2), splitlet | `CONFIG.enemyBase` `:707`~`:714`, charger `:716`, shooter `:717`, 분열 `:1840`, wisp `:700` | 6종이 각자 원본 AI대로 움직인다. 종류별 PlayMode 테스트 |
| 4 | **`EnemyData` ScriptableObject 전환** | — | ⚠️ **3번보다 먼저.** CLAUDE.md 규칙: "2번째 적 종류를 만들기 전에 SO 전환을 끝낸다" |
| 5 | **엘리트** — 8% 확률, 체력×4·피해×1.5·보상×5·크기×1.35 | `:696`, `:3949` | 엘리트가 섞여 나오고 보상이 5배 |
| 6 | **지역별 스폰 테이블** — `rollSpawnType(region)` 해금표 | `:3933`, `enemyLv` `:3924` | 지역이 오르면 몹 구성이 원본대로 바뀐다 |

### 팀원 — 섬영 · 드루이드

**건드리는 파일**: `Characters/Blade*`, `Characters/Druid*`(전부 신규 파일), `Core/IElementAfflictable`(출혈 추가 시)

**건드리면 안 되는 파일**: `Assets/Scenes/CombatCore.unity`, `Assets/Editor/BuildPartAScene.cs`(씬은 내 소유 —
키트를 씬에 붙여야 하면 나한테 요청), `Enemies/` 전체, `Systems/EnemySpawner`, `Core/` 전체(읽기만),
`Characters/CharacterMover2D`·`PlayerRig`·`MageAttack`·`Gunner*`

| # | 항목 | 원본 | 완료 정의(DoD) |
|---|---|---|---|
| A | **섬영 0차** — 관성 이동(`CharacterMover2D.MoveMode.Inertial` 사용, `MoveScale`에 `min(1,statMs)`), 회전베기 Z(cd 0.13, 피해 0.42, 반경 1.05), 속도→피해 배수(정지 ×1 → 최고속 ×2.4), 평타 흡혈 15%, 방향키 누르는 동안 피격 무적 | `CONFIG.blade` `:613`, `bladeMove` `:2454`, `bladeSpinTick` `:2510`, `bladeDmgMult` `:2439`, `bladeDirInvuln` `:2448` | 달릴수록 세지고, 방향 전환이 감속 없이 즉시 뒤집힌다. PlayMode 테스트 |
| B | **드루이드 0차** — 클로 3타 콤보 Z(콤보창 0.5초, 3타째가 마무리), 마나(최대 20, 평타 적중 시 8% 회복) | `CONFIG.druid` `:644`, `druidClaw` `:3272` | 3타 콤보가 돌고 마나가 찬다. PlayMode 테스트 |

> A·B는 사용자 확인(2026-09-08): **팀원이 자기 방식대로 구현한다 — 내가 미리 맞출 필요 없음.**
> 단 위 "공유 계약"의 이름·시그니처만 지켜줄 것.

---

## 7. 확정된 세부 결정 (다시 묻지 말 것)

- **0차엔 X 스킬이 없다.** 원본이 `tier < 1`이면 스킬을 막고 메시지만 띄운다 — 그대로 이식한다.
- **마법사는 이미 5차까지 완료.** 0차로 되돌리지 않는다.
- **캐릭터별 레벨/SP 분리(원본 `charProgress` `:1249`)는 단계 2로 미룬다.** 0차 단계에선 지금의 단일
  `PlayerProfile`을 공유해도 검증에 지장이 없고, 갈라면 기존 코드·테스트가 광범위하게 바뀐다.
  단계 2(캐릭터별 5차 심화)에 들어갈 때 `branch`/`tier` 일반화와 **같이** 처리한다.
- **캐릭터 전환은 임시로 디버그 키**(`PlayerDebugController` 확장). 정식 캐릭터 선택 UI는 로비 몫 —
  로비 UI를 만들 때 이 디버그 조작을 **삭제**한다(이미 그 파일 주석에 삭제 예정으로 표시돼 있다).
- **런 사이클은 단계 3(밸런스) 직전으로 미룬다.** 계약(`GameState`/`RunState`/`CombatEvents`/`IRunResettable`)은
  이미 `main`에 있으니 그때 구현만 하면 된다. `HANDOFF.md`가 그 스펙을 그대로 보관하고 있다.
- **`R키 부활`·`숫자키 강화`는 여전히 삭제 예정**이다(원본에 없음). 각각 런 사이클·로비 UI 때 지운다.
- 밸런스 수치는 원본 그대로. 숫자는 로직에 박지 말고 설정 클래스/SO 한 곳에 모은다.

## 8. 병합 순서 + 씬/프리팹 소유권

```
[나] 착수 전제(1번) → main       ← 선행. 팀원에게 연락
        ↓ (여기서부터 동시 진행)
[나]   2 메카닉 0차 → 4 EnemyData SO → 3 몹 6종 → 5 엘리트 → 6 지역 스폰표
[팀원] A 섬영 0차 → B 드루이드 0차
        ↓
먼저 끝난 쪽이 main 병합 → 나중 쪽이 main 받아서 자기 브랜치에 병합
        ↓
단계 2(캐릭터 하나씩 5차) → 런 사이클 → 단계 3(밸런스)
```

- **씬(`CombatCore.unity`)과 `BuildPartAScene.cs`는 나만 만진다.** 팀원이 키트를 씬에 붙여야 하면 요청할 것.
- **새 프리팹을 만드는 건 자유**(충돌 안 남). 기존 `Enemy_Oni.prefab` 수정은 이번 판에선 나.
- 새 공용 폴더가 필요하면 각자 만들지 말고 **`main`에 폴더만 먼저 만드는 작은 커밋**을 올린다(`.meta` add/add 충돌 방지).
- `PROGRESS.md`는 병합 때 거의 항상 충돌한다 — 정상이다. 로그는 양쪽 다 남기고 상태 섹션만 새로 쓴다.

## 9. 범위 밖 (이번 단계에서 하지 않는다)

- **X 스킬·전문화 티어 전부**(마법사 제외 — 이미 완료). 단계 2에서.
- 런 사이클·로비·HUD·세이브 — 단계 3 직전
- 성소·보스·지역 진행/입장료·경험치 구슬·전투 배수 체인·아이템/가챠/윤회
- **렌더링·비주얼 전부**(원본 `:4482`~`:6215`). 원형 스프라이트 유지 — 사용자 확인 완료
- 사운드·파티클·화면 흔들림·데미지 숫자 팝업

### 이전 계획 (2026-09-08, 회의로 폐기)
"나 1~7(런사이클→UI→세이브→배수체인→구슬→아이템→메카닉) / 팀원 A~I(몹→엘리트→성소→보스→지역→출혈→섬영→드루이드→비기)"
순서였다. **메타 시스템을 먼저** 쌓는 방식이었는데, 회의에서 **캐릭터·몬스터를 먼저 전부 세우고
캐릭터 단위로 깊게 파는** 방식으로 바꿨다. 폐기된 항목이 아니라 **순서만 뒤로 밀린 것**이다.
