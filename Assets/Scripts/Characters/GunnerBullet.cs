using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 메카닉이 쏘는 총알 하나. 원본 `projectiles`의 `kind:'bullet'`(project_test.html:2198 생성,
    /// `:3695`~`:3730` 갱신)에 대응한다.
    ///
    /// 마법사의 마법탄(<see cref="MageProjectile"/>)과 판정 크기·넉백·원소 적용이 전부 다르다:
    /// - 판정: 원본 `rectsOverlap(x-13*size, y-5*size, 26*size, 10*size, ...)`(`:3719`) — 26×10px(0.26×0.10유닛)
    /// - 넉백: `kb: 95`, 방향은 **총알 진행 방향**(`kbDir: sign(w.vx)`, `:3721`)
    /// - `skill: false`라 원소(화상 등)를 묻히지 않는다
    ///
    /// 0차(전문화 미선택)에선 레이저·유도·다중관통이 전부 꺼진 기본 총알만 나간다.
    /// </summary>
    public class GunnerBullet : MonoBehaviour
    {
        // 원본 판정 절반 크기(13px, 5px → 0.13, 0.05유닛). size 배수를 곱해 실제 콜라이더를 만든다.
        const float BaseHalfWidth = 0.13f;
        const float BaseHalfHeight = 0.05f;
        /// <summary>원본 `kb: 95`(project_test.html:3721) ÷100.</summary>
        const float HitKnockback = 0.95f;

        float damage;
        int pierceLeft;
        float life;
        float knockbackDirSign;
        readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        public static GunnerBullet Spawn(Vector3 pos, Vector2 velocity, float damage, int pierce, float life,
            float sizeMul, Sprite sprite, Color color)
        {
            var go = new GameObject("GunnerBullet");
            go.transform.position = pos;

            // 판정과 비주얼을 분리해서 스케일이 이중으로 곱해지는 걸 막는다(MageProjectile과 같은 이유).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(BaseHalfWidth * 2f * sizeMul, BaseHalfHeight * 2f * sizeMul, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(BaseHalfWidth * 2f * sizeMul, BaseHalfHeight * 2f * sizeMul);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = velocity;

            var bullet = go.AddComponent<GunnerBullet>();
            bullet.damage = damage;
            bullet.pierceLeft = pierce;
            bullet.life = life;
            bullet.knockbackDirSign = velocity.x >= 0f ? 1f : -1f; // 원본 kbDir: sign(w.vx)
            return bullet;
        }

        void Update()
        {
            // 원본 `w.t >= w.life || w.x < -60 || w.x > mapW + 60`(project_test.html:3729).
            life -= Time.deltaTime;
            if (life <= 0f
                || transform.position.x < FieldBounds.MinX - 0.6f
                || transform.position.x > FieldBounds.MaxX + 0.6f)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Enemy")) return;
            // 원본은 스폰 무적 대상을 hitSet에 아예 안 넣는다 — 관통도 안 깎이고 무적이 풀리면 다시 걸린다.
            var protectable = other.GetComponent<ISpawnProtectable>();
            if (protectable != null && protectable.IsSpawnProtected) return;
            if (!alreadyHit.Add(other)) return;

            var target = other.GetComponent<IDamageable>();
            if (target != null && !target.IsDead)
            {
                // 치명타·±10% 난수는 적중할 때마다 새로 굴린다(원본 dealDamage).
                float critChance = PlayerStatCalculator.ComputeCritChance(ProfileService.Current);
                int dmg = DamageCalculator.Roll(damage, critChance, out _);
                target.TakeDamageWithKnockback(dmg, null, knockbackDirSign, HitKnockback);
            }

            // 원본 `w.pierceLeft--; if (w.pierceLeft <= 0) remove = true;`(project_test.html:3723-3725).
            pierceLeft--;
            if (pierceLeft <= 0) Destroy(gameObject);
        }
    }
}
