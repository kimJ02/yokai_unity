using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 섬영(閃影) 0차 — 관성 이동(가속) + 회전베기(기본공격, Z) + 흡혈 + 방향키 피격무적.
    /// 원본 `bladeMove`/`bladeSpinTick`/`bladeLifesteal`/`bladeDirInvuln`
    /// (project_test.html:2420~2517)을 옮겼다.
    ///
    /// 이동 자체(가속/감속/방향전환)는 <see cref="CharacterMover2D"/>의 Inertial 모드가 맡는다 —
    /// 파일 소유권 규칙상(`docs/worksplit.md`) 이 키트는 그 파일을 고치지 않고, 매 프레임
    /// <see cref="CharacterMover2D.MoveScale"/>(이속 캡)만 넣어준다.
    ///
    /// **0차에 없는 것**(전부 `docs/worksplit.md`/`HANDOFF.md` "0차" 정의 밖 — 전문화 티어·X스킬 필요):
    /// - 집중(X, `bladeFocusStart` :2521)·칼날폭풍 — 둘 다 이동 티어 1 이상이 있어야 켜진다.
    /// - 최고 속도 충돌 무적(`bladeCrashImmune` :2444) — 이동 티어 2 또는 칼날폭풍 티어 3 필요.
    /// - `p.bladeSpin` 회전 연출 타이머(:2512, 순수 시각효과) — 프로젝트 관례상 연출은 범위 밖.
    /// - `CONFIG.blade.brake`(:615, 2200) — 원본 `bladeMove`도 실제로는 안 쓰는 죽은 값이다
    ///   (`CharacterMover2D` 쪽 주석 참고). 여기서도 새로 만들어 넣지 않는다.
    ///
    /// **상시 적용**(티어 무관 — 그래서 0차에도 있다): 속도→피해 배수(`bladeDmgMult`)와 방향키
    /// 피격무적(`bladeDirInvuln`) — 원본이 조건절에 티어 체크를 안 넣은 것만 확인하고 그대로 옮겼다.
    /// </summary>
    public class BladeCombat : MonoBehaviour, ICharacterKit, IRunResettable
    {
        [Header("원본 CONFIG.blade.spin 그대로 (거리는 100px=1유닛, project_test.html:624)")]
        [Tooltip("표시용 현재 쿨다운. 매 프레임 SpinCd ÷ 공격속도 배수로 다시 계산된다.")]
        public float cooldown = SpinCd;
        [Tooltip("회전베기 판정 반경. 원본 spin.r 105px ÷100.")]
        public float spinRadius = SpinRadius;
        [Tooltip("적 판정용 레이어. 기본값은 전체 — Enemy 전용 레이어를 쓰기 전까지는 태그로 한 번 더 거른다(PlayerAttack과 동일한 관례).")]
        public LayerMask enemyMask = ~0;

        const float SpinCd = 0.13f;             // CONFIG.blade.spin.cd (:624)
        const float SpinDmgMult = 0.42f;        // CONFIG.blade.spin.dmg (:624)
        const float SpinRadius = 1.05f;         // CONFIG.blade.spin.r 105px÷100 (:624)
        const float SpinKnockback = 1.10f;      // bladeSpinHit 호출부 kb:110 (:2489) ÷100
        const float SpeedDmgAtTop = 1.4f;       // CONFIG.blade.spdDmg — 최고 속도에서 피해 +140% (:619)
        const float SpeedDmgPerMsSurplus = 1.4f;// CONFIG.blade.spdDmgPerMs — 100% 넘긴 이속 1.0당 추가 (:620)
        const float LifestealRatio = 0.15f;     // CONFIG.blade.lifesteal (:625)
        const float DirInvulnRefresh = 2f;      // 방향키 유지 중 매 프레임 갱신하는 무적 여유분(근사 — 아래 ApplyDirectionalInvuln 참고)

        CharacterMover2D mover;
        Rigidbody2D rb;
        PlayerHealth playerHealth;
        float cdTimer;

        /// <summary>bladeMsSurplus — 100%를 넘겨 속도로 못 쓰는 이속 초과분(project_test.html:2428). Update에서 매 프레임 갱신.</summary>
        float msSurplus;

        public CharacterId Character => CharacterId.Blade;
        /// <summary>섬영만 관성 가속 이동을 쓴다(원본 `bladeMove`, 나머지 셋은 즉시-속도).</summary>
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

            // bladeMsRaw() = statMs() * 성소 버프(project_test.html:2422~2423).
            // MoveScale은 100%에서 잘리고(bladeMsK, :2426) — CharacterMover2D가 실제 속도·가속에 반영한다.
            // 넘친 분(bladeMsSurplus, :2428)은 여기서 들고 있다가 SpeedDamageMultiplier에서 피해로 환산된다.
            float msRaw = PlayerStatCalculator.ComputeMoveSpeedMultiplier(profile) * CombatModifiers.MoveSpeedMultiplier;
            if (mover != null) mover.MoveScale = Mathf.Min(1f, msRaw);
            msSurplus = Mathf.Max(0f, msRaw - 1f);

            // 원본 `p.atkCds.blade = S.cd / Math.max(0.35, statAs())`(:2514) — 표시용으로 매 프레임 재계산.
            cooldown = SpinCd / Mathf.Max(0.35f, PlayerStatCalculator.ComputeAttackSpeedMultiplier(profile));

            cdTimer -= Time.deltaTime;
            if (GameInput.AttackHeld && cdTimer <= 0f)
            {
                cdTimer = cooldown;
                float dmgMult = SpinDmgMult * SpeedDamageMultiplier(ComputeSpeedRatio());
                SpinAttack(dmgMult);
            }

            // bladeDirInvuln(:2448~2451) — 방향키를 누르고 있는 동안(어느 방향이든) 상시 피격 무적.
            ApplyDirectionalInvuln(GameInput.Left || GameInput.Right || GameInput.Up || GameInput.Down);
        }

        /// <summary>
        /// bladeSpeedRatio (project_test.html:2435) — 실제 수평 속도 ÷ 현재 최고 속도.
        /// `CharacterMover2D`는 관성 속도(`inertialVx`)를 캡슐화해서 밖으로 안 내보낸다(그 파일을
        /// 고치는 건 팀장 담당이라 여기서 필드를 늘려달라고 하지 않는다) — 대신 같은 오브젝트의
        /// `Rigidbody2D`에서 실제로 적용된 속도를 그대로 읽는다. `CharacterMover2D.FixedUpdate`가
        /// 매 스텝 그 값을 `rb.linearVelocity.x`에 쓰기 때문에 결과는 동일하다.
        /// </summary>
        float ComputeSpeedRatio()
        {
            if (mover == null || rb == null) return 0f;
            float maxSpeed = mover.inertialMaxSpeed * mover.MoveScale; // bladeMaxSpeed (:2429)
            if (maxSpeed <= 0f) return 0f;
            return Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / maxSpeed);
        }

        /// <summary>
        /// bladeDmgMult (project_test.html:2439~2442) — 정지 ×1 → 최고 속도 ×(1+spdDmg)=×2.4.
        /// 100%를 넘긴 이속(<see cref="msSurplus"/>)은 속도로 못 나가는 대신 여기서 추가로 곱해진다.
        /// </summary>
        float SpeedDamageMultiplier(float speedRatio)
        {
            return 1f + speedRatio * (SpeedDmgAtTop + msSurplus * SpeedDmgPerMsSurplus);
        }

        /// <summary>
        /// 회전베기. 원본 `bladeSpinHit(dmgMult, r, skipGrabbed=false, lifesteal=true)`
        /// (project_test.html:2480~2496) + `bladeLifesteal`(:2498~2508)을 한 함수로 합쳤다.
        /// `dmgMultiplier`는 이미 SpinDmgMult × SpeedDamageMultiplier가 곱해진 값(= 원본 `dmgMult * bladeDmgMult()`).
        /// </summary>
        void SpinAttack(float dmgMultiplier)
        {
            var profile = ProfileService.Current;
            float baseDamage = PlayerStatCalculator.ComputeAtk(profile) * dmgMultiplier;
            float critChance = PlayerStatCalculator.ComputeCritChance(profile);

            var hits = Physics2D.OverlapCircleAll(transform.position, spinRadius, enemyMask);
            float totalDealt = 0f;
            bool anyHit = false;

            foreach (var col in hits)
            {
                if (col == null || !col.CompareTag("Enemy")) continue;

                // 원본 `e.spawnInvuln > 0` 스킵(:2485) — Characters는 Enemies를 직접 참조할 수 없어
                // (asmdef 계층 규칙) Core의 ISpawnProtectable로만 상태를 묻는다.
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;

                // 원본 `e.dead` 스킵(:2485).
                var target = col.GetComponent<IDamageable>();
                if (target == null || target.IsDead) continue;

                int dmg = DamageCalculator.Roll(baseDamage, critChance, out _);

                // 원본 kbDir: sign(e.x - p.x) || p.facing (:2489) — 정확히 겹친 경우만 facing으로 대체.
                float dirSign = Mathf.Sign(col.transform.position.x - transform.position.x);
                if (Mathf.Approximately(dirSign, 0f)) dirSign = mover != null ? mover.Facing : 1f;

                target.TakeDamageWithKnockback(dmg, gameObject, dirSign, SpinKnockback);

                totalDealt += dmg;
                anyHit = true;
            }

            // bladeLifesteal(:2498~2508) — 이번 회전베기로 입힌 피해 "합계"의 15%를 한 번에 회복한다
            // (적중마다가 아니라 틱 전체 총합 기준 — 원본 `totalDmg` 누적과 동일).
            if (anyHit && totalDealt > 0f && playerHealth != null)
            {
                float heal = Mathf.Round(totalDealt * LifestealRatio);
                if (heal > 0f) playerHealth.Heal(heal);
            }
        }

        /// <summary>
        /// bladeDirInvuln (project_test.html:2448~2451) — 원본은 "이 프레임에 방향키가 눌려
        /// 있는가"를 매 프레임 새로 평가하는 상태 없는(stateless) 조건이다. Unity 쪽 무적은
        /// 감소하는 타이머(<see cref="PlayerHealth.InvulnRemaining"/>)라 외부에서 "이번 프레임만"
        /// 무적을 끼워 넣을 지점이 없다 — 그래서 방향키가 눌려 있는 동안 매 프레임 "최소 이번
        /// 프레임 두 배만큼"을 계속 갱신하는 방식으로 근사했다. `GrantInvuln`은 값을 줄이지 않고
        /// 올리기만 하므로 이미 더 긴 무적(피격 직후 0.9초 등)을 갖고 있으면 그대로 둔다.
        ///
        /// 한계: 키를 뗀 직후 최대 한 프레임 분량의 잔여 무적이 남을 수 있다 — 원본의 프레임 단위
        /// 판정과 사실상 구분되지 않는 오차라 범위 안에서 받아들였다.
        /// </summary>
        void ApplyDirectionalInvuln(bool anyDirectionHeld)
        {
            if (!anyDirectionHeld || playerHealth == null) return;
            playerHealth.GrantInvuln(Time.deltaTime * DirInvulnRefresh);
        }
    }
}
