using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 섬영 0차 — 관성 이동(원본 `bladeMove`, project_test.html:2454) + 회전베기 기본공격
    /// (`bladeSpinTick`/`bladeSpinHit`, `:2510`/`:2480`) + 속도→피해 배수(`bladeDmgMult`, `:2439`) +
    /// 평타 흡혈(`bladeLifesteal`, `:2498`) + 방향키 무적(`bladeDirInvuln`, `:2448`).
    ///
    /// **이동 방식 자체(`CharacterMover2D.MoveMode.Inertial`)는 공용 계약**이라 이 키트는 손대지
    /// 않는다 — `PlayerRig`가 전환 시 `RequiredMoveMode`를 보고 자동으로 세팅한다
    /// (`docs/worksplit.md` 3절). 이 키트가 매 프레임 하는 일은 관성 파라미터의 **배율**
    /// (`CharacterMover2D.MoveScale`)을 이속 스탯에 맞춰 계속 갱신하는 것뿐이다.
    ///
    /// **0차에 없는 것**: 집중(focus)·칼날폭풍(storm) 등 X 스킬 전부(`docs/worksplit.md` "0차" 정의).
    /// 최고 속도 충돌 무적(`bladeCrashImmune`)도 그 스킬들의 티어가 있어야 켜지므로 범위 밖이다.
    /// 화면 흔들림·타격 이펙트·SFX는 연출이라 옮기지 않았다(9절, 범위 밖).
    /// </summary>
    public class BladeCombat : MonoBehaviour, ICharacterKit, IRunResettable
    {
        // ---- 원본 CONFIG.blade 그대로(project_test.html:613~625, 100px=1유닛) ----
        const float SpinCooldown = 0.13f;     // spin.cd
        const float SpinDamageMult = 0.42f;   // spin.dmg
        const float SpinRadius = 1.05f;       // spin.r 105px
        const float SpinKnockback = 1.10f;    // 원본 kb:110(:2489) ÷100
        const float LifestealPct = 0.15f;     // lifesteal
        const float SpeedDamageAtMax = 1.4f;      // spdDmg — 최고 속도에서 피해 +140%
        const float SpeedDamagePerExtraMs = 1.4f; // spdDmgPerMs — 100% 넘긴 이속 1.0당 추가 계수

        CharacterMover2D mover;
        Rigidbody2D rb;
        PlayerHealth playerHealth;
        float cdTimer;

        public CharacterId Character => CharacterId.Blade;
        /// <summary>섬영만 관성 이동을 쓴다 — 나머지 셋은 즉시-속도.</summary>
        public CharacterMover2D.MoveMode RequiredMoveMode => CharacterMover2D.MoveMode.Inertial;

        public void OnSelected() => cdTimer = 0f;
        public void OnDeselected() { }

        /// <summary>새 사냥 시작 시 쿨다운 초기화 — 원본 `resetPlayerForRun()`의 `p.atkCds`(:1516).</summary>
        public void ResetForRun() => OnSelected();

        void Awake()
        {
            mover = GetComponent<CharacterMover2D>();
            rb = GetComponent<Rigidbody2D>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            var profile = ProfileService.Current;
            // 원본 `bladeMsK()`(:2426) — statMs를 100%에서 자른 값. 넘친 분(surplus)은 속도가 아니라
            // 아래 SpeedDamageMultiplier의 피해 계수로 간다. CharacterMover2D는 이 배율을 accel/decel에도
            // 제곱으로 자동 적용한다(ComputeInertialVx 내부) — 여기서 다시 제곱할 필요 없다.
            float rawMs = PlayerStatCalculator.ComputeMoveSpeedMultiplier(profile);
            ApplySpeedScale(rawMs);

            // 원본 `bladeDirInvuln()`(:2448) — 스킬·티어 무관, 방향키를 누르는 동안 상시 적용.
            ApplyDirectionalInvuln(GameInput.Left || GameInput.Right || GameInput.Up || GameInput.Down);

            cdTimer -= Time.deltaTime;
            // 원본 `bladeSpinTick(dt, held)`(:2510) — Z를 누르고 있으면 쿨다운마다 자동으로 벤다.
            if (GameInput.AttackHeld && cdTimer <= 0f)
            {
                float statAs = PlayerStatCalculator.ComputeAttackSpeedMultiplier(profile);
                cdTimer = SpinCooldown / Mathf.Max(0.35f, statAs);
                SpinAttack(rawMs);
            }
        }

        void ApplySpeedScale(float rawMs)
        {
            if (mover != null) mover.MoveScale = Mathf.Min(1f, rawMs);
        }

        /// <summary>
        /// 방향키 무적. 원본은 매 프레임 "지금 방향키가 눌려 있는가"만 즉시 확인하는 상태 없는 조건인데
        /// (`meta.weapon==='blade' && Input.has(...)`), 우리는 그 확인 결과를 `PlayerHealth.InvulnRemaining`
        /// 타이머 하나로 대신 표현한다 — 매 프레임 "이번 프레임만큼은 무적"을 계속 갱신해서 원본과
        /// 같은 결과를 낸다. ⚠️ 키를 뗀 직후 최대 한 프레임(≈0.03초)의 잔여 무적이 남을 수 있는데
        /// (다음 `Update`가 갱신을 멈추기 전까지), 원본의 프레임 단위 즉시 판정과 실질적으로 구분되지
        /// 않는 오차라 그대로 둔다.
        /// </summary>
        void ApplyDirectionalInvuln(bool anyDirectionHeld)
        {
            if (anyDirectionHeld && playerHealth != null)
                playerHealth.GrantInvuln(Time.deltaTime * 2f);
        }

        /// <summary>
        /// 속도→피해 배수. 원본 `bladeDmgMult()`(:2439): `1 + speedRatio × (spdDmg + surplus × spdDmgPerMs)`.
        /// `rawMs`는 100%에서 안 잘린 원본 이속 배수(`ApplySpeedScale`이 잘라서 `MoveScale`에 넣은 값과는
        /// 다르다) — 100%를 넘긴 몫(surplus)이 여기서 피해로 환산되는 게 원본 설계의 핵심이다.
        /// </summary>
        float SpeedDamageMultiplier(float rawMs)
        {
            float moveScale = Mathf.Min(1f, rawMs);
            float maxSpeed = mover != null ? mover.inertialMaxSpeed * moveScale : 0f;
            float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            float ratio = maxSpeed > 0f ? Mathf.Clamp01(speed / maxSpeed) : 0f;
            float surplus = Mathf.Max(0f, rawMs - 1f);
            return 1f + ratio * (SpeedDamageAtMax + surplus * SpeedDamagePerExtraMs);
        }

        /// <summary>표시/디버그용 — 방금 낸 공격의 속도 피해 배수(정지 1.0 → 최고속 2.4).</summary>
        public float LastSpeedDamageMultiplier { get; private set; }

        void SpinAttack(float rawMs)
        {
            float speedMult = SpeedDamageMultiplier(rawMs);
            LastSpeedDamageMultiplier = speedMult;

            var profile = ProfileService.Current;
            float baseDamage = PlayerStatCalculator.ComputeAtk(profile) * SpinDamageMult * speedMult;
            float critChance = PlayerStatCalculator.ComputeCritChance(profile);
            int facing = mover != null ? mover.Facing : 1;

            float totalDealt = 0f;
            var cols = Physics2D.OverlapCircleAll(transform.position, SpinRadius);
            foreach (var col in cols)
            {
                if (!col.CompareTag("Enemy")) continue;
                // 스폰 무적 대상은 원본처럼 판정에서 아예 제외한다(project_test.html:1659와 같은 원칙).
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;

                var target = col.GetComponent<IDamageable>();
                if (target == null || target.IsDead) continue;

                // 원본 `kbDir: sign(e.x - p.x) || p.facing`(:2489) — 밀어내는 방향은 플레이어 기준
                // 좌우, 정확히 겹쳐서 부호가 0이면 바라보는 방향으로 대신한다.
                float dx = col.transform.position.x - transform.position.x;
                float knockbackDir = dx > 0f ? 1f : dx < 0f ? -1f : facing;

                int dmg = DamageCalculator.Roll(baseDamage, critChance, out _);
                target.TakeDamageWithKnockback(dmg, gameObject, knockbackDir, SpinKnockback);
                totalDealt += dmg;
            }

            // 원본 `bladeLifesteal`(:2498) — 회전베기(기본공격)로 입힌 피해의 15%를 체력으로 되돌린다.
            // 스킬(칼날폭풍) 재사용 중의 회전베기는 흡혈 대상이 아니지만, 그건 X 스킬이라 0차 범위 밖.
            if (totalDealt > 0f && playerHealth != null)
                playerHealth.Heal(Mathf.Round(totalDealt * LifestealPct));
        }
    }
}
