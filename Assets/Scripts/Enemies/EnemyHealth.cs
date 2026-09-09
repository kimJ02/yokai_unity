using UnityEngine;
using YokaiFront.Core;
using YokaiFront.Combat;

namespace YokaiFront.Enemies
{
    /// <summary>
    /// 적의 체력·피격 반응·사망. 원본 `dealDamage()`(project_test.html:1657)에서 **대상 쪽 처리**에 해당하는 부분
    /// (`e.hp -= dmg` / `e.flash = 1` / `e.kbx = ...` / `e.hp <= 0 → killEnemy`)을 옮긴 것이다.
    /// 피해량 계산(치명타·난수 변동) 자체는 공격 쪽(`Combat.DamageCalculator`)이 담당한다 — 원본도
    /// `dealDamage` 안에서 공격자 스탯으로 먼저 계산한 뒤 대상 체력을 깎는 순서다.
    ///
    /// 아직 빠진 것(`docs/original-parity.md` 참고): 콤보 적립, 출혈·처형, 분열귀 분열,
    /// 데미지 숫자 팝업·파티클·hitstop. 사망 시 골드·경험치 지급은 <see cref="Died"/> 이벤트로
    /// 트랙 A(`Systems`)가 구독해서 처리한다(`docs/sprints/03-growth-curve-worksplit.md` 참고) — 이 클래스 자체는
    /// 보상을 계산하지 않는다(Enemies 도메인이 Core.ProfileService 이상을 몰라야 하므로).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyHealth : MonoBehaviour, IDamageable, IElementAfflictable, IVulnerable
    {
        [Header("스탯 (원본 CONFIG.enemyBase.oni — project_test.html:709)")]
        [Tooltip("최대 체력. 원본 오니 38. 몹 종류가 늘면 EnemyData(SO)로 옮긴다(스프린트 4).")]
        public float maxHp = 38f;

        [Header("피격 반응 (원본 그대로)")]
        [Tooltip("기본 넉백 속도(무기별 kb 지정이 없을 때). 원본 kbBase 240px/s ÷100(project_test.html:1678).")]
        public float knockbackSpeed = 2.4f;
        [Tooltip("종류별 넉백 배율. 원본 :1678 — 대오니 0.4, 나머지 1. 스폰 시 EnemyData에서 주입된다.")]
        public float knockbackMultiplier = 1f;
        [Tooltip("피격 플래시 감쇠 속도. 원본 `e.flash -= dt*6`(project_test.html:4020) — 1에서 0까지 약 0.167초.")]
        public float flashDecay = 6f;
        [Tooltip("플래시 최대 강도. 원본은 흰색을 alpha `flash*0.75`로 덧그린다(project_test.html:4799).")]
        public float flashStrength = 0.75f;

        [Header("화상 (원본 CONFIG.elem.burn — project_test.html:692)")]
        public int maxBurnStacks = 10;
        public float burnDuration = 4.0f;   // 원본 life: 4.0 — 매 적용마다 리필(누적 아님)
        public float burnTickInterval = 0.5f;
        public float burnDamagePerStackPct = 0.08f; // statAtk × 이 값 × 스택 수, 0.5초마다

        /// <summary>현재 체력. UI/테스트가 읽는다.</summary>
        public float CurrentHp { get; private set; }
        public float MaxHp => maxHp;
        public bool IsDead => CurrentHp <= 0f;

        /// <summary>사망 직전(Destroy 전) 1회 발생 — 트랙 A(처치 보상)가 여기 구독해서 골드/EXP를 지급한다.</summary>
        public event System.Action<EnemyHealth> Died;

        SpriteRenderer sr;
        Color baseColor;
        ISpawnProtectable spawnProtect;
        EnemyMove mover;
        float flash;

        int burnStacks;
        float burnRemaining;
        float burnTickTimer;
        // 원본 `e.vulnT` / `e.gravityExpose`(project_test.html:3968에서 0으로 시작).
        float vulnRemaining;
        float gravityExpose;

        void Awake()
        {
            CurrentHp = maxHp;
            sr = GetComponent<SpriteRenderer>();
            baseColor = sr.color;
            // 같은 오브젝트의 EnemyMove가 둘 다 제공한다. 인터페이스로 받는 건 Core 규칙 때문이 아니라
            // (같은 도메인이라 직접 참조해도 되지만) 나중에 스폰 무적을 다른 컴포넌트가 맡아도 되게.
            spawnProtect = GetComponent<ISpawnProtectable>();
            mover = GetComponent<EnemyMove>();
        }

        void Update()
        {
            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - Time.deltaTime * flashDecay);
                sr.color = Color.Lerp(baseColor, Color.white, flash * flashStrength);
            }
            UpdateBurn(Time.deltaTime);
            // 원본 `e.vulnT = Math.max(0, e.vulnT - dt)`(project_test.html:1726).
            if (vulnRemaining > 0f) vulnRemaining = Mathf.Max(0f, vulnRemaining - Time.deltaTime);
        }

        // ---- IVulnerable (원본 vulnT, project_test.html:1663·:1726·:3818) ----

        public bool IsVulnerable => vulnRemaining > 0f;

        public void ApplyVulnerable(float duration) => vulnRemaining = Mathf.Max(vulnRemaining, duration);

        /// <summary>
        /// 원본 `e.gravityExpose += dt * (1 + stack*0.16); if (>= 1.25) e.vulnT = max(vulnT, 3.0)`(`:3817`).
        /// 누적치는 **줄어들지 않는다** — 한 번 임계치를 넘긴 적은 이후 중력점에 닿을 때마다 곧바로 다시 취약해진다.
        /// </summary>
        public void AddGravityExposure(float exposure)
        {
            if (IsDead) return;
            gravityExpose += exposure;
            if (gravityExpose >= MageSpecConfig.GravityVulnerableThreshold)
                ApplyVulnerable(MageSpecConfig.VulnerableDuration);
        }

        /// <summary>원본 `updateElements()`의 burn 분기(project_test.html:1729-1741) 그대로.</summary>
        void UpdateBurn(float dt)
        {
            if (burnStacks <= 0 || IsDead) return;
            burnRemaining -= dt;
            burnTickTimer += dt;
            if (burnTickTimer >= burnTickInterval)
            {
                burnTickTimer -= burnTickInterval;
                float atk = PlayerStatCalculator.ComputeAtk(ProfileService.Current);
                float raw = atk * burnDamagePerStackPct * burnStacks;
                // 화상 틱은 원본이 noCrit·noHitstop·kb:0으로 넘긴다 — 여기선 크리티컬 확률 0, 넉백 0.
                int dmg = DamageCalculator.Roll(raw, 0f, out _);
                TakeDamageWithKnockback(dmg, null, 1f, 0f);
            }
            if (burnRemaining <= 0f) burnStacks = 0; // 원본 `if (e.burnT <= 0) e.burnS = 0`
        }

        public void ApplyBurn(int stacks)
        {
            if (IsDead) return;
            burnStacks = Mathf.Min(maxBurnStacks, burnStacks + stacks);
            burnRemaining = burnDuration; // 원본 `e.burnT = E.life` — 누적이 아니라 리필
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (source == null)
            {
                TakeDamageWithKnockback(amount, null, 1f, knockbackSpeed);
                return;
            }
            // 원본 `sign(e.x - player.x)`(project_test.html:1679) — 가해자 반대 방향으로 밀린다.
            // 정확히 같은 X에 겹쳐 있으면 sign이 0이라 원본은 `|| player.facing`으로 대체하는데,
            // 우리는 가해자 방향 정보가 없어 오른쪽(+1)으로 고정한다(겹친 순간에만 생기는 예외).
            float dir = Mathf.Sign(transform.position.x - source.transform.position.x);
            if (Mathf.Approximately(dir, 0f)) dir = 1f;
            TakeDamageWithKnockback(amount, source, dir, knockbackSpeed);
        }

        public void TakeDamageWithKnockback(float amount, GameObject source, float knockbackDirSign, float knockbackSpeedOverride)
        {
            if (IsDead) return;
            // 원본은 `dealDamage` 진입 즉시 스폰 무적을 확인하고 0을 반환한다(project_test.html:1659).
            // 공격 스크립트(MageProjectile)도 별도로 확인하는데, 그건 **관통 카운트를 소모하지 않기 위해서**라
            // 역할이 다르다 — 여기 검사는 "어떤 경로로 들어온 피해든 무적이면 무효"를 보장한다.
            if (spawnProtect != null && spawnProtect.IsSpawnProtected) return;

            // 원본 `vulnMult`(project_test.html:1663) — 취약한 적은 모든 경로의 피해를 1.2배로 받는다.
            // 여기서 곱하는 이유와 반올림 편차는 `DamageCalculator.VulnerableMultiplier` 주석 참고.
            if (IsVulnerable)
                amount = Mathf.Max(1f, Mathf.Round(amount * DamageCalculator.VulnerableMultiplier));

            CurrentHp -= amount;
            flash = 1f; // 원본 `e.flash = 1`(project_test.html:1666)

            // 원본 `kbBase * (e.type === 'bigOni' ? 0.4 : 1)`(project_test.html:1678) — 종류별 저항.
            if (mover != null && knockbackSpeedOverride > 0f)
                mover.ApplyKnockback(knockbackDirSign * knockbackSpeedOverride * knockbackMultiplier);

            if (CurrentHp <= 0f) Die();
        }

        void Die()
        {
            CurrentHp = 0f;
            // 원본 killEnemy(project_test.html:1793)의 골드·경험치는 Died 구독자(트랙 A)가 처리.
            // 연쇄처치 보너스·분열귀 분열은 아직 범위 밖 — 나중 스프린트에서 붙인다.
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// 난이도 스케일링(트랙 A, "가상 지역 레벨")이 스폰 직후 호출한다. <c>maxHp</c> 필드만 바꾸면
        /// 이미 실행된 <see cref="Awake"/>가 세팅한 <c>CurrentHp</c>엔 반영되지 않는다 — 이 프로젝트에서
        /// 반복적으로 겪은 함정(RequireComponent 자동보충, AddComponent의 동기 Awake 실행)과 같은 종류라
        /// 전용 메서드로 묶어 실수를 막는다.
        /// </summary>
        public void SetMaxHp(float newMaxHp)
        {
            maxHp = newMaxHp;
            CurrentHp = newMaxHp;
        }
    }
}
