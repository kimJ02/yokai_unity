# 분업안 (living document)

> **이 파일이 분업의 유일한 출처다.** 채팅에서 정한 분업은 여기 적히기 전까지 존재하지 않는 것으로 친다
> (2026-09-08에 채팅으로만 정했다가 통째로 날린 적 있음). 형식 규칙은 `CLAUDE.md` "분업안 — 작성 위치와 형식".
> 완료된 항목은 지우지 말고 `✅`로 표시한다.

## 1. 머리말

- **대상**: `docs/original-parity.md`(원본↔Unity 전수 대조표, 2026-09-08)에서 "✗ 없음"으로 잡힌 **남은 전부**
- **근거 문서**: [`docs/original-parity.md`](original-parity.md) · 원본 `reference/project_test.html`
- **확정 날짜**: 2026-09-08
- **참여**: 나(사용자 세션) / 팀원(다른 클로드 세션). 밸런스 수치 조정은 기획자 상윤이 나중에 담당 — **우리는 원본 수치 그대로 이식만 한다.**
- **원칙**: 축을 둘로 갈라 파일이 안 겹치게 한다. **나 = 게임 구조·플레이어·메타 진행 / 팀원 = 적·월드·나머지 캐릭터.**

이전 분업안 `docs/sprint2-handoff-split.md`(스프린트 2 트랙 A/B)는 **완료됐다** — 참고용으로만 남긴다.

---

## 2. 착수 전제 (선행 작업 — 내가 먼저 처리, 팀원은 대기)

팀원 항목 대부분이 "런 중인가?"와 "지금 몇 지역인가?"를 읽어야 하므로, 아래 3개 파일이 `main`에 올라간 뒤 착수한다.

1. `Core/GameState.cs` — 씬 상태(로비/런/일시정지/결과)
2. `Core/RunState.cs` — 현재 런의 지역·남은 시간·처치 수
3. `Core/CombatEvents.cs` — 도메인 간 신호(처치 알림, 성소 버프)

**팀원은 이 커밋이 `main`에 올라갔다는 연락을 받은 뒤 시작할 것.** (그 전에도 A·F·G·H는 위 3개와 무관하므로 먼저 시작 가능)

---

## 3. 공유 계약 (임의로 이름/타입 바꾸지 말 것)

### `Core/GameState.cs` — 내가 소유, 팀원은 **읽기만**
```csharp
namespace YokaiFront.Core
{
    public enum GameScene { Lobby, Run, Pause, Result }   // 원본 state.scene(:1466)

    public static class GameState
    {
        public static GameScene Current { get; private set; } = GameScene.Lobby;
        /// <summary>런이 실제로 굴러가는 중인지. false면 적·스폰·투사체 전부 갱신을 멈춘다.</summary>
        public static bool IsRunning => Current == GameScene.Run;
        public static void Set(GameScene scene);   // 내가 구현
    }
}
```
⚠️ **팀원 필수 대응**: `EnemySpawner`/`EnemyMove`/`EnemyHealth`/새 몹·보스·성소 전부 `Update()` 첫 줄에
`if (!GameState.IsRunning) return;`을 넣는다. 원본도 `frame()`에서 `state.scene !== 'run'`이면 `dt=0`으로
모든 갱신을 멈춘다(`:7169`).

### `Core/RunState.cs` — 내가 소유, 팀원은 **읽기만**
```csharp
namespace YokaiFront.Core
{
    public static class RunState
    {
        public static int Region { get; }      // 1~9. 원본 run.region
        public static float TimeLeft { get; }  // 남은 시간(초)
        public static int Kills { get; }       // 이번 런 처치 수
        public static int Fury { get; }        // 살기 스택(= 이번 런 처치 수, 원본 run.fury :1825)
    }
}
```
팀원은 `RunState.Region`으로 몹 종류(`rollSpawnType`)와 지역 스케일을 결정한다.
기존 `Systems/RunProgress`(누적 처치 100 = 가상 지역)는 **내가 1번에서 진짜 지역으로 교체**하므로 팀원은 손대지 말 것.

### `Core/CombatEvents.cs` — 내가 소유, **양쪽이 발행/구독**
```csharp
namespace YokaiFront.Core
{
    public static class CombatEvents
    {
        /// 적이 죽을 때. 콤보·살기·연쇄처치·경험치구슬(내 4·5번)이 구독한다.
        public static event System.Action<GameObject> EnemyKilled;
        /// 성소 파괴 시 플레이어가 받을 버프 지속시간(초). 팀원 C가 발행 → 내 4번이 구독.
        public static event System.Action<float> ShrineBuffGranted;
        /// 처치 보상 지급 후 결과 화면 집계용 알림 (gold, exp). 팀원이 발행 → 내 1번이 구독.
        public static event System.Action<int, int> RewardGranted;

        public static void RaiseEnemyKilled(GameObject enemy);
        public static void RaiseShrineBuffGranted(float duration);
        public static void RaiseRewardGranted(int gold, int exp);
    }
}
```
`Enemies`(2층)는 `Characters`(2층)를 참조할 수 없으므로 성소→플레이어 버프는 **반드시 이 이벤트로만** 넘긴다
(`IDamageable`과 같은 이유, `CLAUDE.md` asmdef 계층표).

⚠️ **팀원 필수 대응 2건** — `Systems/EnemySpawner.HandleEnemyDied`에서 보상을 지급한 직후:
```csharp
CombatEvents.RaiseEnemyKilled(enemy.gameObject);   // 콤보·살기·구슬이 여기 붙는다
CombatEvents.RaiseRewardGranted(gold, exp);        // 결과 화면 집계
```
지급 자체(`ProfileService.Current.AddGold/AddExp`)는 지금 코드 그대로 두고, **알림 두 줄만** 추가하면 된다.
(`RunState.RegisterKill`은 내가 이 이벤트를 구독해서 부르므로 팀원이 직접 부르지 않는다.)

### 이미 있는 것 (그대로 쓸 것, 다시 만들지 말 것)
| 파일 | 용도 | 소유 |
|---|---|---|
| `Core/PlayerProfile` + `ProfileService` | 레벨·경험치·골드·강화·마법사 빌드 | 나 |
| `Core/DifficultyScalingConfig` | 몹 체력/피해/보상 스케일 상수 | 팀원(E에서 지역 기준으로 확장) |
| `Core/IDamageable` (+`TakeDamageWithKnockback`) | 피해 전달 | 공용 |
| `Core/IElementAfflictable` (`ApplyBurn`) | 화상 | 팀원이 F에서 `ApplyBleed` 추가 |
| `Core/IGravityAffectable` | 중력점 끌림 | 공용 |
| `Core/GameInput` | 모든 키 입력 | 공용. **`Input.GetKey`를 스크립트에서 직접 부르지 말 것** |
| `Enemies/EnemyHealth.Died` | 처치 알림 | 공용 |

⚠️ **이 프로젝트 단골 함정**: `Instantiate`/`AddComponent` 직후 필드만 바꾸면 이미 실행된 `Awake()`에 반영이 안 된다.
체력은 반드시 `EnemyHealth.SetMaxHp()`를 통해 바꿀 것(이 함정으로 이미 3번 깨졌음).

---

## 4·5·6. 트랙별 담당 / 건드리는 파일 / 완료 정의

### 나 — 게임 구조 · 플레이어 · 메타 진행

**건드리는 파일**: `Core/`(GameState·RunState·CombatEvents·PlayerProfile·SaveData·아이템 정의), `Characters/` 전체,
`Combat/DamageCalculator`·`MageSkillEffects`·`MageSpecConfig`, `Systems/RunProgress`(지역으로 교체), 새 `UI/` 도메인,
**`Assets/Scenes/CombatCore.unity` + `Assets/Editor/BuildPartAScene.cs`(씬 소유권 = 나)**

**건드리면 안 되는 파일**: `Enemies/` 전체, `Systems/EnemySpawner`, `Enemy_Oni.prefab` 및 팀원이 새로 만드는 몹/보스/성소 프리팹

| # | 항목 | 원본 | 완료 정의(DoD) |
|---|---|---|---|
| 1 | **런 사이클** — 씬 상태머신 4종, 런 타이머(일반 90초/보스 180초), `startRun`/`endRun`(dead·timeout·bossdead)/`toLobby`, ESC 일시정지, **R키 부활 삭제** | `:1466`, `:4293`, `:4354`, `:4332`, `:4382`, `:4409`, ESC `:1026`, 부활은 아이템 전용 `:1917` | 죽으면 결과 화면 → 로비로 나가진다. 90초 지나면 timeout으로 끝난다. PlayMode 테스트로 상태 전이 검증 |
| 2 | **로비 UI + 사냥 HUD** — 캐릭터/지역 선택, 강화 탭, 전문화 탭(마법사 빌드), 결과 화면 / HUD는 체력·시간·콤보·스킬 쿨다운 | 로비 `:6367`, 전문화 탭 `:6974`, HUD `:6216` | 숫자키 1~7 디버그 조작·OnGUI 표시 **전부 삭제**하고 UI로 대체 |
| 3 | **세이브/로드** — `meta` 전체 직렬화(레벨·골드·강화·빌드·지역진행·아이템·윤회) | `saveMeta` `:1213` / `loadMeta` `:1216` | 껐다 켜도 진행이 남는다 |
| 4 | **전투 배수 체인** — 콤보(3초, +1.2%/스택), 살기(처치당 +0.8% 피해·+0.4% 공속), 연쇄처치(0.8초 내 3킬), 레벨 페널티, 성소 버프 수신, 취약(vulnT) | 콤보 `:1621`/`:742`/`:703`, 살기 `:1825`/`:702`, 연쇄 `:1829`/`:704`, `levelFactor` `:1651`/`:729`, `dmgMultAll` `:1296` | `DamageCalculator`가 원본 피해식의 모든 항을 계산한다(아이템 항은 6번에서) |
| 5 | **경험치 구슬** — 50킬마다 드랍, 자석 1.5유닛, 획득 시 필요경험치 25% | `:4450`, `CONFIG.orb` `:697` | 구슬이 떨어지고 빨려와 경험치가 오른다 |
| 6 | **아이템 · 가챠 · 윤회 · 업적** — 아이템 정의/효과 훅(`itemAdd`/`itemMul`/`itemPow`), 가챠(천장), 윤회 리셋, 윤회 장벽, 업적 | 아이템 `:747`~/`:1189`, 가챠 `:6538`, 윤회 `:6495`/`:741`, 업적 `:819` | 뽑고 → 스탯에 반영되고 → 윤회하면 초기화된다 |
| 7 | **메카닉(gunner) 캐릭터** — 레이저 / 설치기 3개 빌드 5티어 | `SPEC.gunner` `:915`, 구현 `:2180`~`:2419`, 기하 계산 `:1327`~`:1411` | 마법사와 같은 수준으로 원본 대조 + PlayMode 테스트 |

### 팀원 — 적 · 월드 · 나머지 캐릭터

**건드리는 파일**: `Enemies/` 전체, `Systems/EnemySpawner`, `Core/DifficultyScalingConfig`, `Core/EnemyData`(신규 SO),
`Core/IElementAfflictable`(`ApplyBleed` 추가만), 새로 만드는 몹/보스/성소 **프리팹**(새 프리팹은 충돌 안 남),
섬영·드루이드용 새 파일(`Characters/Blade*`, `Characters/Druid*` — 파일을 새로 만드는 한 충돌 없음)

**건드리면 안 되는 파일**: `Assets/Scenes/CombatCore.unity`, `Assets/Editor/BuildPartAScene.cs`(씬은 내 소유 — 씬에 뭔가 배치해야 하면 나한테 요청),
`Core/GameState`·`RunState`·`CombatEvents`(읽기만), `Core/PlayerProfile`, `Characters/Mage*`·`CharacterMover2D`·`PlayerHealth`, `Combat/DamageCalculator`

| # | 항목 | 원본 | 완료 정의(DoD) |
|---|---|---|---|
| A | **몹 6종 추가** — wisp(공중 부유·수직추적), bigOni(넉백 저항 0.4), charger(조준→돌진→탈진 상태기계), shooter(거리유지+투사체), splitter(죽으면 새끼 2), splitlet. **+ `EnemyData` ScriptableObject 전환** | `CONFIG.enemyBase` `:707`~`:714`, charger `:716`, shooter `:717`, 분열 `:1840`, wisp 수직속도 `:700` | 6종이 각자 원본 AI대로 움직인다. 종류별 PlayMode 테스트 |
| B | **엘리트** — 8% 확률, 체력×4·피해×1.5·보상×5·크기×1.35 | `:696`, `:3949` | 엘리트가 섞여 나오고 보상이 5배 |
| C | **성소** — 18초 후 첫 등장·35초 간격, 살아있는 동안 적 강화(피해×1.3 이속×1.25), 파괴 시 `CombatEvents.RaiseShrineBuffGranted(15초)` 발행 + 경험치 30·골드 45 | `CONFIG.shrine` `:698`, `spawnShrine` `:3996`, 파괴 처리 `:1797` | 성소가 뜨고, 부수면 이벤트가 발행된다(플레이어 쪽 수신은 내 4번) |
| D | **보스** — 지역별 1체, 슬램·돌진·3연발 투사체, 체력 800×2.15^(지역-1), 보스 스테이지 180초 | `CONFIG.boss` `:718`, 구현 `:4154`~`:4291` | 보스 패턴 3종이 원본대로 돈다 |
| E | **지역별 스폰·스케일** — `rollSpawnType(region)` 해금 테이블, 지역당 체력×2.15·피해×1.4·보상×1.42. `DifficultyScalingConfig`를 `RunState.Region` 기준으로 확장 | `:3933`, `CONFIG.scale` `:727`, `enemyLv` `:3924` | 지역이 올라가면 몹 종류·강도가 원본대로 바뀐다 |
| F | **출혈(bleed) + 처형** — `IElementAfflictable.ApplyBleed`, 최대 10스택·6초, 스택×1.5% 이하 체력 즉시 처형 | `CONFIG.elem.bleed` `:693`, `applyElement` `:1699`, `checkBleedExecute` `:1712` | 출혈이 쌓이고 처형이 터진다. (섬영이 쓰는 선행 작업) |
| G | **섬영(blade)** — 관성 이동(가속/브레이크/최고속도=화력), 회전베기, 집중/칼날폭풍 5티어 | `SPEC.blade` `:933`, `CONFIG.blade` `:613`, 구현 `:2420`~`:2976` | 원본 대조 + PlayMode 테스트 |
| H | **드루이드(druid)** — 마나, 늑대 무리 / 맹금 변신 5티어 | `SPEC.druid` `:951`, `CONFIG.druid` `:644`, 구현 `:2985`~`:3494` | 원본 대조 + PlayMode 테스트 |
| I | **비기(ult) 공용 트리** — 캐릭터와 별개인 공용 스킬트리 | `meta.skills.ult` `:1143`, `CONFIG.ult` `:685` | 5티어 습득·발동이 원본대로 |

> G·H는 사용자 확인(2026-09-08): **팀원이 자기 방식대로 구현한다 — 내가 미리 맞출 필요 없음.**

---

## 7. 확정된 세부 결정 (다시 묻지 말 것)

- **R키 부활은 삭제한다.** 원본에 없다 — 죽으면 결과 화면 → 로비다(`:1927`). 부활은 아이템 '최후의 발악' 보유 시 자동 발동뿐(`:1917`).
- **강화·전문화는 로비에서 한다.** 인게임 숫자키(1~5 강화, 6~7 빌드)와 `OnGUI` 표시는 로비 UI가 생기면 **삭제**한다. 지금은 임시방편일 뿐이다.
- **밸런스 수치는 원본 그대로 옮긴다.** 조정은 기획자(상윤)가 나중에. 단 **숫자는 로직에 박지 말고 설정 클래스/SO 한 곳에 모은다**(`DifficultyScalingConfig`·`MageSpecConfig` 패턴).
- **`RunProgress`(가상 지역 레벨)는 임시 대체품이다.** 내 1번에서 진짜 지역(`RunState.Region`)으로 교체된다.
- 캐릭터별 레벨/SP 분리(원본 `meta.charProgress` `:1249`)는 **내 7번(메카닉 추가) 시점에** 처리한다. 그 전까지 `PlayerProfile`의 단일 레벨은 곧 마법사의 레벨이다.

## 8. 병합 순서 + 씬/프리팹 소유권

```
[나] GameState + RunState + CombatEvents → main      ← 선행. 팀원에게 연락
        ↓ (여기서부터 동시 진행)
[나] 1 런사이클 → 2 UI → 3 세이브 → 4 배수체인 → 5 구슬 → 6 아이템 → 7 메카닉
[팀원] A 몹6종 → B 엘리트 → C 성소 → D 보스 → E 지역스폰 → F 출혈 → G 섬영 → H 드루이드 → I 비기
        ↓
먼저 끝난 쪽이 main 병합 → 나중 쪽이 main 받아서 자기 브랜치에 병합
```

- **씬(`CombatCore.unity`)과 `BuildPartAScene.cs`는 나만 만진다.** 팀원이 씬에 뭔가 배치해야 하면 나한테 요청할 것(협업 규칙 1번).
- **새 프리팹을 만드는 건 자유**(충돌 안 남). 단 **기존 `Enemy_Oni.prefab` 수정은 팀원만**.
- 새 공용 폴더(`Assets/Scripts/UI/` 등)가 필요하면 각자 만들지 말고 **`main`에 폴더만 먼저 만드는 작은 커밋**을 올린다(`.meta` add/add 충돌 방지, 실제로 겪음).
- `PROGRESS.md`는 병합 때 거의 항상 충돌한다 — 정상이다. 로그는 양쪽 다 남기고 상태 섹션만 새로 쓴다(협업 규칙 6번).

## 9. 범위 밖 (이번에 하지 않는다)

- **렌더링·비주얼 전부**(원본 `:4482`~`:6215`, 1734줄의 캔버스 벡터 아트). 원형 스프라이트 유지 — 사용자 확인 완료
- 사운드(`Audio_`, `:1042`), 가챠 연출(`:6595`), 파티클·화면 흔들림·데미지 숫자 팝업
- 밸런스 재조정 (기획자 담당)
