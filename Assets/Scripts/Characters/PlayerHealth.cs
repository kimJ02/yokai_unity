using System;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 플레이어 체력·무적시간·피격 반동. 원본 `damagePlayer()`(project_test.html:1879)를 옮긴 것이다.
    ///
    /// **적 쪽(`EnemyHealth`)과 비대칭인 점**: 원본은 피해 ±10% 난수를 적에게는 공격자 쪽(`dealDamage`)에서,
    /// 플레이어에게는 피격자 쪽(`damagePlayer` :1904)에서 적용한다. 그 구조를 그대로 따랐다 —
    /// 그래서 여기서 `amount`를 받은 뒤 다시 난수를 굴린다(적을 때릴 때처럼 공격자가 미리 굴려 오지 않는다).
    ///
    /// v0에서 빠진 것(HANDOFF.md "범위 밖"): 아이템 관련 전부(피해감소 `drMult`, 피격무효 `guard`,
    /// 부활 `revive`, 무적연장 `iframe`), 섬영 전용 회피, 화면 붉은 플래시·흔들림·사운드.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("스탯 (원본 CONFIG.player — project_test.html:605~606)")]
        [Tooltip("최대 체력. 원본 baseHp 100.")]
        public float maxHp = 100f;
        [Tooltip("피격 후 무적시간(초). 원본 invulnTime 0.9 — 접촉 반복 피해를 막는 건 전적으로 이 값이다.")]
        public float invulnTime = 0.9f;

        [Header("피격 반동 (원본 그대로, 100px=1유닛)")]
        [Tooltip("가해자 반대 방향으로 밀리는 속도. 원본 260px/s ÷100(project_test.html:1908).")]
        public float knockbackSpeedX = 2.6f;
        [Tooltip("위로 튕기는 최소 속도. 원본 `vy = min(vy, -220)`인데 원본 Y+가 아래라 부호를 뒤집었다(:1909).")]
        public float knockbackSpeedY = 2.2f;

        /// <summary>현재 체력.</summary>
        public float CurrentHp { get; private set; }
        public float MaxHp => maxHp;
        public bool IsDead => CurrentHp <= 0f;
        /// <summary>남은 무적시간(초). 0보다 크면 피해를 받지 않는다.</summary>
        public float InvulnRemaining { get; private set; }

        /// <summary>
        /// 사망 시 1회 발생. **지금은 아무도 구독하지 않는다** — 런 종료 처리는 스프린트 3(런 사이클) 범위라
        /// 신호만 노출해둔다(HANDOFF.md "범위 밖" 참고). 원본은 여기서 `endRun('dead')`를 부른다(:1927).
        /// </summary>
        public event Action Died;

        Rigidbody2D rb;

        void Awake()
        {
            CurrentHp = maxHp;
            rb = GetComponent<Rigidbody2D>();
            // 원본은 레벨업 때만 최대체력을 다시 계산하고 풀피로 채운다(project_test.html:1850)
            // — 골드 강화(hp)를 사더라도 다음 레벨업 전까지는 즉시 반영되지 않는, 원본 그대로의
            // 비직관적 동작이다(임의로 "매 프레임 갱신"으로 바꾸지 말 것).
            ProfileService.Current.LeveledUp += HandleLeveledUp;
        }

        void OnDestroy()
        {
            // 정적 싱글턴(ProfileService.Current) 구독이라 해제 안 하면 파괴된 오브젝트를 향한
            // 호출이 남는다 — EnemyHealth 테스트 오염 때 겪은 것과 같은 함정.
            ProfileService.Current.LeveledUp -= HandleLeveledUp;
        }

        void HandleLeveledUp()
        {
            maxHp = PlayerStatCalculator.ComputeMaxHp(ProfileService.Current);
            CurrentHp = maxHp;
        }

        void Update()
        {
            if (InvulnRemaining > 0f) InvulnRemaining -= Time.deltaTime;
        }

        /// <summary>
        /// 스프린트 2 임시 사망 처리(`docs/sprint2-handoff-split.md` 확정: "정지 + R키 재시작").
        /// 원본은 런 종료+결과화면(:1927 `endRun('dead')`)이지만 런 사이클이 스프린트 3 범위라
        /// 그때까지 테스트가 끊기지 않게 최소한만 만든다 — `PlayerDeathHandler`가 R키 입력 시 호출한다.
        /// </summary>
        public void Revive()
        {
            CurrentHp = maxHp;
            InvulnRemaining = 0.5f; // 재시작 직후 바로 다시 안 맞게 하는 안전장치(원본에 없는 실무적 편의)
        }

        /// <summary>
        /// 남은 무적시간을 최소 이 값까지 늘린다(줄이지는 않는다). 원본 `p.invuln = Math.max(p.invuln, 0.22)`
        /// (project_test.html:2149, 불길 이동 텔레포트 직후 무적)에 대응 — 다른 무적(피격 직후 0.9초 등)이
        /// 이미 더 길게 남아있으면 그대로 둔다.
        /// </summary>
        public void GrantInvuln(float duration) => InvulnRemaining = Mathf.Max(InvulnRemaining, duration);

        public void TakeDamage(float amount, GameObject source)
        {
            // 원본 `if (p.invuln > 0 ...) return;`(project_test.html:1881).
            // 적 쪽에서도 겹침 판정 전에 무적을 확인하지만(:4144), Enemies 도메인은 Characters를 참조할 수
            // 없으므로(asmdef 같은 층) 우리 구조에선 이 검사 하나가 그 역할을 전부 맡는다 — 결과는 동일하다.
            if (IsDead || InvulnRemaining > 0f) return;

            // 원본 `dmg = max(1, round(dmg * mult * drMult() * rand(0.9,1.1)))`(:1904)에서
            // 아이템 피해감소(drMult)만 빠진 형태. 난수를 **여기서** 굴리는 게 원본 구조다(클래스 주석 참고).
            int applied = Mathf.Max(1, Mathf.RoundToInt(amount * UnityEngine.Random.Range(0.9f, 1.1f)));
            CurrentHp -= applied;
            InvulnRemaining = invulnTime;

            // 원본 `p.vx = sign(p.x - srcX) * 260; p.vy = min(p.vy, -220); p.onGround = false;`(:1908~1910)
            // 주의: 원본에서 이 수평 속도는 **다음 프레임에 이동 입력으로 통째로 덮어써진다**
            // (`p.vx = mx * moveSpeed ...`, :1548 — 피격 중 이동 잠금이 없다). 우리 CharacterMover2D도
            // FixedUpdate마다 vx를 덮어쓰므로 별도 처리 없이 같은 동작이 된다 — 일부러 넉백을 유지시키지 말 것.
            // 반면 수직 속도는 CharacterMover2D가 보존하므로(종단속도 클램프만 함) 튕겨 오르는 건 남는다.
            if (rb != null)
            {
                float dirX = 1f;
                if (source != null)
                {
                    dirX = Mathf.Sign(transform.position.x - source.transform.position.x);
                    if (Mathf.Approximately(dirX, 0f)) dirX = 1f;
                }
                rb.linearVelocity = new Vector2(dirX * knockbackSpeedX, Mathf.Max(rb.linearVelocity.y, knockbackSpeedY));
            }

            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                Died?.Invoke();
            }
        }
    }
}
