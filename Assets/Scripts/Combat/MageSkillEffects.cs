using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Combat
{
    /// <summary>
    /// 마법사 스킬트리의 1회성 광역 효과(폭발·중력 충돌·대붕괴) — 지속형 장판은
    /// <see cref="FireTrailZone"/>/<see cref="GravityWellZone"/>에 있다. 전부 순수 위치/파라미터만
    /// 받는 정적 함수라 `Characters.MageAttack`(마법사 자신)과 `GravityWellZone`(중력점 자체 폭발)
    /// 양쪽에서 재사용한다.
    ///
    /// `Combat`이 `Enemies`를 참조할 수 없어(asmdef 계층 규칙) 대상은 전부 `Physics2D` 질의 +
    /// `Core.IDamageable`/`IElementAfflictable`로만 건드린다.
    /// </summary>
    public static class MageSkillEffects
    {
        /// <summary>원본 `spawnFireExplosion`(project_test.html:1978). 넉백은 플레이어 위치 기준
        /// (원본 `dealDamage`가 kbDir 생략 시 쓰는 기본값 `sign(e.x-player.x)||player.facing`).</summary>
        public static bool SpawnFireExplosion(Vector2 pos, int tier, float power, Vector2 playerPos, int playerFacing)
        {
            float r = MageSpecConfig.FireExplosionRadius(tier);
            float dmgMult = MageSpecConfig.FireExplosionDamageMult(tier, power);
            int burnStacks = MageSpecConfig.FireExplosionBurnStacks(tier);
            var profile = ProfileService.Current;
            float atk = PlayerStatCalculator.ComputeAtk(profile);
            float critChance = PlayerStatCalculator.ComputeCritChance(profile);

            bool hit = false;
            var hits = Physics2D.OverlapCircleAll(pos, r);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Enemy")) continue;
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;
                var target = col.GetComponent<IDamageable>();
                if (target == null || target.IsDead) continue;

                hit = true;
                int dmg = DamageCalculator.Roll(atk * dmgMult, critChance, out _);
                float dir = Mathf.Sign(col.transform.position.x - playerPos.x);
                if (Mathf.Approximately(dir, 0f)) dir = playerFacing;
                target.TakeDamageWithKnockback(dmg, null, dir, 2.60f); // 원본 kb:260(:1987)

                var afflictable = col.GetComponent<IElementAfflictable>();
                afflictable?.ApplyBurn(burnStacks);
            }
            return hit;
        }

        /// <summary>
        /// 원본 `detonateGravityOrb`(project_test.html:2023) — 충돌 지점 원형 피해를 준 뒤
        /// 그 자리에 중력점을 만들거나 중첩시킨다(<see cref="GravityWellZone.SpawnOrMerge"/>).
        /// 넉백은 폭발 중심 기준(원본 `kbDir: sign(dx)||player.facing`, dx = e.x - 폭발x).
        ///
        /// **버그 수정(2026-09-28)**: 원본 `y = Math.min(y, groundY - 20)`은 원본이 **Y+가 아래인
        /// 캔버스 좌표계**라 "바닥보다 더 내려가지 못하게" 막는 상한 클램프였다. 이 포팅은 Y+가
        /// 위인 Unity 좌표계(`FieldBounds.GroundY = 0f`, 발판이 위로 갈수록 Y가 커짐)인데 부호를
        /// 안 뒤집고 그대로 `Mathf.Min`을 써서, `GroundY - clearance`(= -0.2)가 모든 유효 Y(≥0)보다
        /// 항상 작아 **폭발 지점이 몇 층에서 터지든 무조건 맵 맨 밑바닥 아래로 끌려 내려갔다**
        /// (중력장이 항상 1층 바닥에서만 생기는 것처럼 보이는 원인). 다른 파일들처럼 방향을
        /// 뒤집은 `Mathf.Max`로 고쳤다 — "바닥보다 아래로는 못 내려간다"는 의도는 그대로 유지된다.
        /// </summary>
        public static int DetonateGravityOrb(Vector2 pos, int tier, float chargeK, int playerFacing)
        {
            if (tier <= 0) return 0;
            pos.y = Mathf.Max(pos.y, FieldBounds.GroundY + MageSpecConfig.GravityGroundClearance);
            float r = MageSpecConfig.GravityImpactRadius(tier, chargeK);
            float dmgMult = MageSpecConfig.GravityImpactDamageMult(tier, chargeK);
            var profile = ProfileService.Current;
            float atk = PlayerStatCalculator.ComputeAtk(profile);
            float critChance = PlayerStatCalculator.ComputeCritChance(profile);

            int hitCount = 0;
            var hits = Physics2D.OverlapCircleAll(pos, r);
            foreach (var col in hits)
            {
                if (!col.CompareTag("Enemy")) continue;
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;
                var target = col.GetComponent<IDamageable>();
                if (target == null || target.IsDead) continue;

                hitCount++;
                int dmg = DamageCalculator.Roll(atk * dmgMult, critChance, out _);
                float dir = Mathf.Sign(col.transform.position.x - pos.x);
                if (Mathf.Approximately(dir, 0f)) dir = playerFacing;
                target.TakeDamageWithKnockback(dmg, null, dir, MageSpecConfig.GravityImpactKnockback);
            }

            GravityWellZone.SpawnOrMerge(pos, tier, chargeK);
            return hitCount;
        }

        /// <summary>
        /// 원본 `gravityCollapse`(project_test.html:2081) — X 스킬(중력 계열). 살아있는 중력점을
        /// 전부(플레이어 근처 범위 안에서) 터뜨린다. 중력점이 하나도 없으면 플레이어 정면(또는
        /// 가장 가까운 적 위치)에 한 번만 터뜨린다.
        /// </summary>
        public static (int count, bool critical, int bursts) GravityCollapse(int tier, Vector2 playerPos, int playerFacing, Vector2? nearestEnemyPos)
        {
            var wells = GravityWellZone.Active.FindAll(w =>
                w != null &&
                Mathf.Abs(w.transform.position.x - playerPos.x) < MageSpecConfig.CollapseSearchHalfWidth &&
                Mathf.Abs(w.transform.position.y - playerPos.y) < MageSpecConfig.CollapseSearchHalfHeight);
            wells.Sort((a, b) => b.Stack != a.Stack ? b.Stack - a.Stack : a.Age.CompareTo(b.Age));

            var centers = new System.Collections.Generic.List<(Vector2 pos, int stack, GravityWellZone well)>();
            if (wells.Count > 0)
            {
                foreach (var w in wells) centers.Add((w.transform.position, w.Stack, w));
            }
            else
            {
                Vector2 fallback = nearestEnemyPos ?? (playerPos + new Vector2(playerFacing * MageSpecConfig.CollapseFallbackDistance, 0f));
                centers.Add((fallback, 0, null));
            }

            var profile = ProfileService.Current;
            float atk = PlayerStatCalculator.ComputeAtk(profile);
            float critChance = PlayerStatCalculator.ComputeCritChance(profile);

            int totalCount = 0;
            bool anyCritical = false;

            for (int idx = 0; idx < centers.Count; idx++)
            {
                var (cx, stack, well) = centers[idx];
                float r = MageSpecConfig.CollapseRadius(tier, stack);

                var primaryTargets = new System.Collections.Generic.List<(Collider2D col, IDamageable dmg)>();
                float distSum = 0f;
                foreach (var col in Physics2D.OverlapCircleAll(cx, r))
                {
                    if (!col.CompareTag("Enemy")) continue;
                    var protectable = col.GetComponent<ISpawnProtectable>();
                    if (protectable != null && protectable.IsSpawnProtected) continue;
                    var target = col.GetComponent<IDamageable>();
                    if (target == null || target.IsDead) continue;
                    primaryTargets.Add((col, target));
                    distSum += Vector2.Distance(col.transform.position, cx);
                }

                int count = primaryTargets.Count;
                totalCount += count;
                float avgDist = count > 0 ? distSum / count : r;
                bool tight = MageSpecConfig.CollapseIsTight(count, avgDist, stack);
                bool critical = MageSpecConfig.CollapseIsCritical(tier, count, tight, stack);
                anyCritical = anyCritical || critical;

                float massBonus = MageSpecConfig.CollapseMassBonus(count, stack, critical);
                float dmgMult = MageSpecConfig.CollapseDamageMult(tier, massBonus, idx);
                float kb = MageSpecConfig.CollapseKnockback(critical);

                foreach (var (col, target) in primaryTargets)
                {
                    int dmg = DamageCalculator.Roll(atk * dmgMult, critChance, out _);
                    float dir = Mathf.Sign(col.transform.position.x - cx.x);
                    if (Mathf.Approximately(dir, 0f)) dir = playerFacing;
                    target.TakeDamageWithKnockback(dmg, null, dir, kb);
                }

                // 원본 `zones.push({ kind: 'gravityBurst', x: cx, y: cy, r, life: 0.42 + idx * 0.04, critical })`(:2114) — 그림만.
                GravityBurstRing.Spawn(cx, r, MageSpecConfig.CollapseBurstLife + idx * MageSpecConfig.CollapseBurstLifeStep,
                                       critical, ringOnly: false);

                if (critical)
                {
                    float r2 = r * MageSpecConfig.CollapseCriticalRingMult(tier);
                    foreach (var col in Physics2D.OverlapCircleAll(cx, r2))
                    {
                        if (!col.CompareTag("Enemy")) continue;
                        if (primaryTargets.Exists(p => p.col == col)) continue;
                        var protectable = col.GetComponent<ISpawnProtectable>();
                        if (protectable != null && protectable.IsSpawnProtected) continue;
                        var target = col.GetComponent<IDamageable>();
                        if (target == null || target.IsDead) continue;

                        int dmg2 = DamageCalculator.Roll(atk * dmgMult * MageSpecConfig.CollapseCriticalRingDamageMult, critChance, out _);
                        // 원본은 이 2차 링에 kbDir을 지정하지 않는다 — 기본값(플레이어 위치 기준)을 그대로 쓴다(:2120).
                        float dir = Mathf.Sign(col.transform.position.x - playerPos.x);
                        if (Mathf.Approximately(dir, 0f)) dir = playerFacing;
                        target.TakeDamageWithKnockback(dmg2, null, dir, MageSpecConfig.CollapseCriticalRingKnockback);
                    }
                    // 원본 `zones.push({ kind: 'gravityBurst', r: r2, life: 0.58, critical: true, ringOnly: true })`(:2123).
                    GravityBurstRing.Spawn(cx, r2, MageSpecConfig.CollapseCriticalRingLife, critical: true, ringOnly: true);
                }

                if (well != null)
                {
                    GravityWellZone.Active.Remove(well);
                    Object.Destroy(well.gameObject);
                }
            }

            return (totalCount, anyCritical, centers.Count);
        }
    }
}
