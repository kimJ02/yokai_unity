using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Enemies
{
    /// <summary>
    /// 적의 체력·피격 반응·사망. 원본 `dealDamage()`(project_test.html:1657)에서 **대상 쪽 처리**에 해당하는 부분
    /// (`e.hp -= dmg` / `e.flash = 1` / `e.kbx = ...` / `e.hp <= 0 → killEnemy`)을 옮긴 것이다.
    /// 피해량 계산(치명타·난수 변동) 자체는 공격 쪽(`Combat.DamageCalculator`)이 담당한다 — 원본도
    /// `dealDamage` 안에서 공격자 스탯으로 먼저 계산한 뒤 대상 체력을 깎는 순서다.
    ///
    /// v0에서 빠진 것(HANDOFF.md "범위 밖"): 골드·경험치 드랍, 콤보 적립, 원소(화상/출혈), 처형,
    /// 분열귀 분열, 데미지 숫자 팝업·파티클·hitstop. 사망은 지금은 그냥 파괴한다(원본 `killEnemy`는
    /// 보상 정산까지 하지만 그건 스프린트 3 범위).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("스탯 (원본 CONFIG.enemyBase.oni — project_test.html:709)")]
        [Tooltip("최대 체력. 원본 오니 38. 몹 종류가 늘면 EnemyData(SO)로 옮긴다(스프린트 4).")]
        public float maxHp = 38f;

        [Header("피격 반응 (원본 그대로)")]
        [Tooltip("넉백 속도. 원본 kbBase 240px/s ÷100(project_test.html:1678).")]
        public float knockbackSpeed = 2.4f;
        [Tooltip("피격 플래시 감쇠 속도. 원본 `e.flash -= dt*6`(project_test.html:4020) — 1에서 0까지 약 0.167초.")]
        public float flashDecay = 6f;
        [Tooltip("플래시 최대 강도. 원본은 흰색을 alpha `flash*0.75`로 덧그린다(project_test.html:4799).")]
        public float flashStrength = 0.75f;

        /// <summary>현재 체력. UI/테스트가 읽는다.</summary>
        public float CurrentHp { get; private set; }
        public float MaxHp => maxHp;
        public bool IsDead => CurrentHp <= 0f;

        SpriteRenderer sr;
        Color baseColor;
        ISpawnProtectable spawnProtect;
        EnemyMove mover;
        float flash;

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
            if (flash <= 0f) return;
            flash = Mathf.Max(0f, flash - Time.deltaTime * flashDecay);
            sr.color = Color.Lerp(baseColor, Color.white, flash * flashStrength);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            if (IsDead) return;
            // 원본은 `dealDamage` 진입 즉시 스폰 무적을 확인하고 0을 반환한다(project_test.html:1659).
            // 공격 스크립트(MageProjectile)도 별도로 확인하는데, 그건 **관통 카운트를 소모하지 않기 위해서**라
            // 역할이 다르다 — 여기 검사는 "어떤 경로로 들어온 피해든 무적이면 무효"를 보장한다.
            if (spawnProtect != null && spawnProtect.IsSpawnProtected) return;

            CurrentHp -= amount;
            flash = 1f; // 원본 `e.flash = 1`(project_test.html:1666)

            if (mover != null && source != null)
            {
                // 원본 `sign(e.x - player.x)`(project_test.html:1679) — 가해자 반대 방향으로 밀린다.
                // 정확히 같은 X에 겹쳐 있으면 sign이 0이라 원본은 `|| player.facing`으로 대체하는데,
                // 우리는 가해자 방향 정보가 없어 오른쪽(+1)으로 고정한다(겹친 순간에만 생기는 예외).
                float dir = Mathf.Sign(transform.position.x - source.transform.position.x);
                if (Mathf.Approximately(dir, 0f)) dir = 1f;
                mover.ApplyKnockback(dir * knockbackSpeed);
            }

            if (CurrentHp <= 0f) Die();
        }

        void Die()
        {
            CurrentHp = 0f;
            // v0: 보상 정산 없이 파괴만 한다. 원본 killEnemy(project_test.html:1793)의 골드·경험치·
            // 연쇄처치 보너스·분열귀 분열은 각각 나중 스프린트에서 붙인다.
            Destroy(gameObject);
        }
    }
}
