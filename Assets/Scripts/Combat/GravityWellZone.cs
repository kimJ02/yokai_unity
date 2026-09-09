using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Combat
{
    /// <summary>
    /// 중력 계열 마법사의 중력점. 원본 `spawnGravityWell`(생성/중첩, project_test.html:2044)과
    /// `updateZones()`의 `gravityWell` 분기(끌어당김·틱 피해, project_test.html:3806-3843)를 옮겼다.
    ///
    /// 활성 인스턴스를 <see cref="Active"/>에 정적으로 등록해 두 곳에서 참조한다: (1) 새 중력탄이
    /// 착탄했을 때 "근처에 살아있는 중력점이 있으면 중첩만 늘린다"는 검색(<see cref="SpawnOrMerge"/>),
    /// (2) X 스킬 대붕괴(`MageSkillEffects.GravityCollapse`)가 터뜨릴 대상 목록을 얻을 때.
    /// `ProfileService`와 같은 정적 상태라 테스트 간 오염을 막으려면 <see cref="ClearAllForTests"/>를
    /// 반드시 `[TearDown]`에서 불러야 한다.
    /// </summary>
    public class GravityWellZone : MonoBehaviour
    {
        public static readonly List<GravityWellZone> Active = new List<GravityWellZone>();
        const int RingSegments = 24;

        public int Tier { get; private set; }
        public int Stack { get; private set; }
        public float Radius { get; private set; }
        public float Pull { get; private set; }
        /// <summary>스폰(또는 마지막 중첩) 이후 지난 시간. 원본 `z.t` — 대붕괴가 "가장 최근 것 우선"으로 정렬할 때 쓴다.</summary>
        public float Age { get; private set; }

        float life;
        float dmgTickTimer;
        LineRenderer ring;

        public static void ClearAllForTests()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] != null) Destroy(Active[i].gameObject);
            Active.Clear();
        }

        public static GravityWellZone SpawnOrMerge(Vector2 pos, int tier, float chargeK)
        {
            if (tier <= 0) return null;
            pos.y = Mathf.Min(pos.y, FieldBounds.GroundY - MageSpecConfig.GravityGroundClearance);
            int stackGain = MageSpecConfig.GravityStackGain(tier, chargeK);

            // 원본: 2티어부터만 "근처 기존 중력점에 중첩"이 가능하다(project_test.html:2050).
            if (tier >= 2)
            {
                foreach (var well in Active)
                {
                    if (well == null) continue;
                    float mergeR = MageSpecConfig.GravityWellMergeRadiusBase + well.Stack * MageSpecConfig.GravityWellMergeRadiusPerStack;
                    if (Vector2.Distance(well.transform.position, pos) < mergeR)
                    {
                        well.Stack = Mathf.Clamp(well.Stack + stackGain, 1, 5);
                        well.Age = 0f;
                        well.life = Mathf.Max(well.life, MageSpecConfig.GravityWellLife(tier, chargeK));
                        well.Radius = MageSpecConfig.GravityWellRadius(tier, well.Stack, chargeK);
                        well.Pull = MageSpecConfig.GravityWellPull(tier, well.Stack, chargeK);
                        well.UpdateRingRadius();
                        return well;
                    }
                }
            }

            int limit = MageSpecConfig.GravityWellLimit(tier);
            if (Active.Count >= limit)
            {
                GravityWellZone oldest = null;
                float oldestAge = -1f;
                foreach (var w in Active)
                {
                    if (w != null && w.Age > oldestAge) { oldestAge = w.Age; oldest = w; }
                }
                if (oldest != null)
                {
                    Active.Remove(oldest);
                    Destroy(oldest.gameObject);
                }
            }

            var go = new GameObject("GravityWellZone");
            // 런이 끝나거나 새로 시작하면 사라져야 한다(원본 `projectiles = []`/`zones = []`, :4318).
            go.AddComponent<RunTransient>();
            go.transform.position = pos;
            var zone = go.AddComponent<GravityWellZone>();
            zone.Tier = tier;
            zone.Stack = stackGain;
            zone.life = MageSpecConfig.GravityWellLife(tier, chargeK);
            zone.Radius = MageSpecConfig.GravityWellRadius(tier, stackGain, chargeK);
            zone.Pull = MageSpecConfig.GravityWellPull(tier, stackGain, chargeK);
            zone.SetupRing();
            Active.Add(zone);
            return zone;
        }

        void SetupRing()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.widthMultiplier = 0.04f;
            ring.material = new Material(Shader.Find("Sprites/Default"));
            ring.startColor = ring.endColor = new Color(0.78f, 0.42f, 1f, 0.55f);
            ring.sortingOrder = 3;
            ring.positionCount = RingSegments;
            UpdateRingRadius();
        }

        void UpdateRingRadius()
        {
            if (ring == null) return;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i / (float)RingSegments * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * Radius, Mathf.Sin(a) * Radius, 0f));
            }
        }

        void OnDestroy() => Active.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            Age += dt;

            float pullR = Radius + MageSpecConfig.GravityPullRangeExtra(Stack);
            var affected = new List<(IDamageable target, float nearBonus)>();

            foreach (var col in Physics2D.OverlapCircleAll(transform.position, pullR))
            {
                if (!col.CompareTag("Enemy")) continue;
                var protectable = col.GetComponent<ISpawnProtectable>();
                if (protectable != null && protectable.IsSpawnProtected) continue;

                Vector2 toWell = (Vector2)transform.position - (Vector2)col.transform.position;
                float d = Mathf.Max(toWell.magnitude, 0.01f);

                var pullable = col.GetComponent<IGravityAffectable>();
                if (pullable != null)
                {
                    float nearEase = Mathf.Clamp(1f - d / pullR, 0.12f, 1f);
                    float force = Pull * (0.45f + nearEase) * (1f + Stack * 0.18f);
                    Vector2 dir = toWell / d;
                    pullable.ApplyGravityWellPull(new Vector2(dir.x * force * dt, dir.y * force * dt * 0.28f));
                }

                // 원본 `e.gravityExpose += dt * (1 + stack*0.16)` → 1.25 넘으면 취약 3초(`:3817`~`:3819`).
                // 끌어당김·틱 피해와 달리 **범위 안에 있기만 하면** 누적된다(보스 예외 없음).
                var vulnerable = col.GetComponent<IVulnerable>();
                vulnerable?.AddGravityExposure(dt * (1f + Stack * MageSpecConfig.GravityExposureRatePerStack));

                var target = col.GetComponent<IDamageable>();
                if (target != null && !target.IsDead)
                    affected.Add((target, Mathf.Clamp(1f - d / pullR, 0f, 1f) * 0.05f));
            }

            dmgTickTimer += dt;
            float tickInterval = MageSpecConfig.GravityDamageTickInterval(Stack);
            if (dmgTickTimer >= tickInterval)
            {
                dmgTickTimer -= tickInterval;
                float atk = PlayerStatCalculator.ComputeAtk(ProfileService.Current);
                foreach (var (target, nearBonus) in affected)
                {
                    if (target.IsDead) continue;
                    float mult = MageSpecConfig.GravityTickDamageMult(Tier, Stack, nearBonus);
                    int dmg = DamageCalculator.Roll(atk * mult, 0f, out _); // 원본 noCrit
                    target.TakeDamageWithKnockback(dmg, null, 1f, 0f); // 원본 kb:0
                }
            }

            if (Age >= life)
            {
                Active.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}
