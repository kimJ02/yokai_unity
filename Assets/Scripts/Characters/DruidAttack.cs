using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 드루이드 0차 기본공격 — 클로 3타 콤보(원본 `druidClaw()`, project_test.html:3271~3321).
    /// 원본은 같은 함수에서 늑대 소환·맹금 변신까지 처리하지만 그건 전부 X 스킬/전문화 티어가
    /// 있어야 열리는 자원이라(`docs/worksplit.md` "0차" 정의 — 기본공격 + 이동 방식만), 여기선
    /// 콤보 판정 + 마나 획득만 옮긴다. 이동은 마법사·메카닉과 같은 `Instant` 모드 그대로다
    /// (맹금 변신의 관성 대시는 티어 스킬이라 범위 밖).
    ///
    /// **0차에 없는 것**: 늑대 소환·맹금 변신(둘 다 X 스킬, `druidCurTier() &gt;= 1`부터),
    /// 3차 이상 전용 효과(늑대 무리 지속시간 연장 등 tier 분기). 마나는 쌓이기만 하고 쓸 곳이
    /// 없다 — 원본 `resetPlayerForRun`도 드루이드 시작 마나를 `CONFIG.druid.mana.cost`(20,
    /// 곧 최대치)로 채워둔다(`:1526`). 화면 흔들림·히트스톱·타격 이펙트는 연출이라 범위 밖
    /// (`docs/worksplit.md` 9절)이라 원본의 `shake()`/`hitstop` 인자는 옮기지 않았다.
    /// </summary>
    public class DruidAttack : MonoBehaviour, ICharacterKit, IRunResettable
    {
        /// <summary>원본 `CONFIG.druid.claw.combo`(project_test.html:656~660) 중 판정에 쓰는 항만.
        /// `a0`/`a1`/`r`/`step`은 궤적 그리기용 연출 값이라 옮기지 않았다.</summary>
        struct ComboHit
        {
            public readonly float DamageMult;
            public readonly float Knockback;
            public ComboHit(float damageMult, float knockback) { DamageMult = damageMult; Knockback = knockback; }
        }

        // 원본 CONFIG.druid.claw.combo(:656~660). 넉백(kb)은 100px=1유닛으로 환산.
        static readonly ComboHit[] Combo =
        {
            new ComboHit(0.85f, 1.40f), // 1타
            new ComboHit(0.95f, 1.55f), // 2타
            new ComboHit(1.8f,  3.40f), // 3타 — 마무리
        };

        const float BaseCooldown = 0.24f;        // 원본 claw.cd(:654)
        const float ComboWindow = 0.5f;          // 원본 claw.comboWindow(:655)
        const float Range = 0.96f;               // 원본 claw.range 96px(:654)
        const float Back = 0.18f;                // 원본 claw.back 18px(:654)
        const float Height = 0.92f;              // 원본 claw.height 92px(:654)
        const float FinisherReachMult = 1.25f;   // 원본 3타째 reach 배율(:3284~3285)
        const float FinisherDurationMult = 1.5f; // 원본 attackDur 배율(:3279)
        const float CooldownPortion = 0.92f;     // 원본 `atkCds.druid = attackDur * 0.92 / statAs()`(:3280)

        const float ManaMax = 20f;        // 원본 CONFIG.druid.mana.base(:646) — 0차엔 티어가 없어 이 값 고정
        const float ManaOnHitPct = 0.08f; // 원본 mana.atkGainPct(:649)
        const float StartMana = 20f;      // 원본 resetPlayerForRun의 druid 분기: CONFIG.druid.mana.cost(:1526,:648)

        CharacterMover2D mover;
        float cdTimer;
        float comboTimer;
        int comboIndex;

        /// <summary>표시/디버그용 현재 마나. 0차엔 소비처(X 스킬)가 없어 쌓이기만 한다.</summary>
        public float Mana { get; private set; }

        /// <summary>표시/디버그용 — 방금 낸 콤보 타의 피해 배수(0.85/0.95/1.8, 원본 `CONFIG.druid.claw.combo[i].dmg`).
        /// `GunnerAttack.baseDamage`와 같은 목적 — 랜덤(치명타·±10%)이 섞이기 전 값을 테스트가 직접 볼 수 있게 한다.</summary>
        public float LastComboDamageMult { get; private set; }

        public CharacterId Character => CharacterId.Druid;
        /// <summary>드루이드는 마법사·메카닉과 같은 즉시-속도 이동(관성은 섬영 전용).</summary>
        public CharacterMover2D.MoveMode RequiredMoveMode => CharacterMover2D.MoveMode.Instant;

        public void OnSelected()
        {
            cdTimer = 0f;
            comboTimer = 0f;
            comboIndex = 0;
        }

        public void OnDeselected() { }

        /// <summary>새 사냥 시작 시 — 원본 `p.mana = meta.weapon === 'druid' ? CONFIG.druid.mana.cost : 0`(:1526).
        /// 0차는 마나를 쓸 스킬이 없어 겉으로 드러나진 않지만, 나중에 스킬이 붙었을 때 시작부터
        /// 어긋나지 않도록 원본 그대로 채워둔다.</summary>
        public void ResetForRun()
        {
            OnSelected();
            Mana = StartMana;
        }

        void Awake() => mover = GetComponent<CharacterMover2D>();

        void Update()
        {
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                // 콤보 창이 지나면 다음 입력은 1타부터(원본 `if ((p.druidComboT||0) <= 0) p.druidCombo = 0`, :3274).
                if (comboTimer <= 0f) comboIndex = 0;
            }

            cdTimer -= Time.deltaTime;

            // 원본(:3458): `if (held && p.atkCds.druid <= 0) druidClaw();` — Z를 누르고 있으면
            // 쿨다운마다 자동으로 다음 콤보 타를 낸다(마법사처럼 차지하지 않는다).
            if (GameInput.AttackHeld && cdTimer <= 0f) Claw();
        }

        void Claw()
        {
            int ci = Mathf.Min(comboIndex, Combo.Length - 1);
            var hit = Combo[ci];
            bool finisher = ci >= Combo.Length - 1;
            LastComboDamageMult = hit.DamageMult;

            // 원본(:3279~3281) — 콤보 진행/쿨다운 계산은 맞았는지와 무관하게 먼저 확정된다.
            float attackDur = BaseCooldown * (finisher ? FinisherDurationMult : 1f);
            float statAs = PlayerStatCalculator.ComputeAttackSpeedMultiplier(ProfileService.Current);
            cdTimer = attackDur * CooldownPortion / Mathf.Max(0.35f, statAs);
            comboTimer = attackDur + ComboWindow;

            int facing = mover != null ? mover.Facing : 1;
            float reachMult = finisher ? FinisherReachMult : 1f;
            float reach = (Range + Back) * reachMult;
            // 원본 x0(:3284) — facing 방향으로 back만큼 물러난 지점에서 reach만큼 뻗는 판정 상자.
            float x0 = facing > 0 ? transform.position.x - Back : transform.position.x - Range * reachMult;
            var boxCenter = new Vector2(x0 + reach / 2f, transform.position.y);
            var boxSize = new Vector2(reach, Height);

            float baseDamage = PlayerStatCalculator.ComputeAtk(ProfileService.Current) * hit.DamageMult;
            float critChance = PlayerStatCalculator.ComputeCritChance(ProfileService.Current);

            int hitCount = 0;
            var cols = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
            foreach (var col in cols)
            {
                if (!col.CompareTag("Enemy")) continue;
                // 원본은 스폰 무적 대상엔 판정 자체가 안 들어간다(project_test.html:1659와 같은 원칙).
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;

                var target = col.GetComponent<IDamageable>();
                if (target == null || target.IsDead) continue;

                int dmg = DamageCalculator.Roll(baseDamage, critChance, out _);
                target.TakeDamageWithKnockback(dmg, gameObject, facing, hit.Knockback);
                hitCount++;
            }

            // 콤보 인덱스는 맞았는지와 무관하게 넘어간다(원본 `p.druidCombo = finisher?0:ci+1`가
            // `if (!hit) return 0;`보다 먼저 실행된다, :3296~3298).
            comboIndex = finisher ? 0 : ci + 1;

            // 마나 획득(+늑대 관련 후속 로직)은 실제로 맞았을 때만(원본 `if (!hit) return 0;`, :3298).
            // 늑대 소환은 0차 범위 밖이라 마나만 옮긴다.
            if (hitCount > 0) Mana = Mathf.Clamp(Mana + ManaMax * ManaOnHitPct, 0f, ManaMax);
        }
    }
}
