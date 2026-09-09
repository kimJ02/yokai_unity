using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
/// <summary>
/// MageAttack이 쏘는 마법탄 하나. 원본 `projectiles` 배열의 화살 오브젝트(kind:'arrow',
/// project_test.html:1964-1975)와 그 명중/충돌 처리(`updateProjectiles`, project_test.html:3601-3693)를
/// 옮겼다.
///
/// 판정 크기: 원본은 `rectsOverlap(w.x-20*size, w.y-15*size, 40*size, 30*size, ...)` — 40×30px
/// (=0.4×0.3유닛, 차지 0이면) 직사각형이고 `size=1+chargeK*0.9(+티어5 폭발보너스)`로 차지할수록 커진다.
/// </summary>
public class MageProjectile : MonoBehaviour
{
    // 원본 hitRect 절반 크기(20px, 15px → 0.2, 0.15유닛). sizeMul을 곱해서 실제 콜라이더 크기를 만든다.
    const float BaseHalfWidth = 0.2f;
    const float BaseHalfHeight = 0.15f;

    float damage;
    int pierceLeft;
    float life;
    bool infinitePierce;
    bool charged;

    // 원본 `explosive`/`gravityOrb` 분기(project_test.html:3624-3691)에 필요한 상태 전부.
    bool explosive;
    float explosionPower;
    bool gravityOrb;
    float gravityCharge;
    // 폭발/중력 중 실제로 선택된 갈래의 티어. 한 발사체는 둘 중 하나만 해당하므로(gravityOrb와
    // explosive는 상호 배타) 필드 하나로 충분하다.
    int tier;

    // 넉백 방향(원본 `kbDir: sign(w.vx)`, project_test.html:3679 — 시전자 위치가 아니라 투사체
    // 자신의 이동 방향)과, 폭발 효과가 "플레이어 기준 기본 넉백"을 계산하려고 필요한 시전자 정보.
    float hitKnockbackDirSign;
    Vector2 casterPos;
    int casterFacing;

    readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    public static MageProjectile Spawn(Vector3 pos, Vector2 velocity, float damage, int pierce, float life, float sizeMul,
        Sprite sprite, Color color, bool infinitePierce, bool charged, bool explosive, float explosionPower,
        bool gravityOrb, float gravityCharge, int tier, Vector2 casterPos, int casterFacing)
    {
        var go = new GameObject("MageBolt");
        // 런이 끝나거나 새로 시작하면 사라져야 한다(원본 `projectiles = []`/`zones = []`, :4318).
        go.AddComponent<RunTransient>();
        go.transform.position = pos;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * (BaseHalfWidth * 2f * sizeMul);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = gravityOrb ? new Color(0.78f, 0.42f, 1f) : color;
        sr.sortingOrder = 3;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(BaseHalfWidth * 2f * sizeMul, BaseHalfHeight * 2f * sizeMul);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = velocity;

        var proj = go.AddComponent<MageProjectile>();
        proj.damage = damage;
        proj.pierceLeft = pierce;
        proj.life = life;
        proj.infinitePierce = infinitePierce;
        proj.charged = charged;
        proj.explosive = explosive;
        proj.explosionPower = explosionPower;
        proj.gravityOrb = gravityOrb;
        proj.gravityCharge = gravityCharge;
        proj.tier = tier;
        proj.casterPos = casterPos;
        proj.casterFacing = casterFacing;
        proj.hitKnockbackDirSign = Mathf.Approximately(velocity.x, 0f) ? casterFacing : Mathf.Sign(velocity.x);
        return proj;
    }

    void Update()
    {
        // 원본 맵 좌우 경계 충돌(project_test.html:3626-3639) — 벽에 닿으면 관통 여부와 무관하게 터진다.
        if (transform.position.x <= FieldBounds.MinX || transform.position.x >= FieldBounds.MaxX)
        {
            ResolveWallImpact();
            return;
        }

        if (!infinitePierce)
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                // 원본 `!w.inf && w.t >= w.life` (project_test.html:3688) — 중력탄만 수명이 다하면 자동 폭발한다.
                if (gravityOrb) MageSkillEffects.DetonateGravityOrb(transform.position, tier, gravityCharge, casterFacing);
                Destroy(gameObject);
            }
        }
    }

    void ResolveWallImpact()
    {
        if (gravityOrb) MageSkillEffects.DetonateGravityOrb(transform.position, tier, gravityCharge, casterFacing);
        else if (explosive) MageSkillEffects.SpawnFireExplosion(transform.position, tier, explosionPower, casterPos, casterFacing);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ResolveWallImpact();
            return;
        }

        if (!other.CompareTag("Enemy")) return;
        // 원본은 `if (e.spawnInvuln > 0) continue`로 스폰 직후 무적 대상을 hitSet에 아예 안 넣는다
        // — pierce도 안 깎이고, 무적이 풀린 뒤 다시 판정에 걸릴 수 있다. 그대로 이식.
        var protectable = other.GetComponent<ISpawnProtectable>();
        if (protectable != null && protectable.IsSpawnProtected) return;
        if (!alreadyHit.Add(other)) return;

        if (gravityOrb)
        {
            // 원본(project_test.html:3673-3677) — 중력탄은 직격 피해가 없다. 명중 즉시 폭발+중력점 생성.
            MageSkillEffects.DetonateGravityOrb(transform.position, tier, gravityCharge, casterFacing);
            Destroy(gameObject);
            return;
        }

        var target = other.GetComponent<IDamageable>();
        if (target != null && !target.IsDead)
        {
            // 치명타·±10% 난수는 적중할 때마다 새로 굴린다(원본 dealDamage, 관통 시 각 대상 별도 판정).
            float critChance = PlayerStatCalculator.ComputeCritChance(ProfileService.Current);
            int dmg = DamageCalculator.Roll(damage, critChance, out _);
            // 원본 kbDir: sign(w.vx), kb: 140 (project_test.html:3679) — 시전자 위치가 아니라 투사체 진행 방향.
            target.TakeDamageWithKnockback(dmg, null, hitKnockbackDirSign, 1.40f);

            if (explosive)
            {
                int stacks = charged ? 2 : 1; // 원본 `const stacks = w.charged ? 2 : 1`(:3678)
                other.GetComponent<IElementAfflictable>()?.ApplyBurn(stacks);

                // 원본(project_test.html:3680) — 충전됐거나 3티어 이상이면 관통 여부와 무관하게 착탄마다 폭발.
                if (charged || tier >= 3)
                    MageSkillEffects.SpawnFireExplosion(transform.position, tier, explosionPower, casterPos, casterFacing);
            }
        }

        // 원본 `w.pierceLeft--; if (w.pierceLeft <= 0) remove = true;`(project_test.html:3683-3684).
        pierceLeft--;
        if (pierceLeft <= 0) Destroy(gameObject);
    }
}
}
