using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Characters;
using YokaiFront.Core;
using YokaiFront.Enemies;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 섬영 0차 검증 — 원본 `bladeSpinTick`/`bladeSpinHit`(project_test.html:2510/:2480), 속도→피해
/// (`bladeDmgMult`, `:2439`), 흡혈(`bladeLifesteal`, `:2498`), 방향키 무적(`bladeDirInvuln`, `:2448`).
/// 관성 이동 자체(`CharacterMover2D.MoveMode.Inertial`)는 `CharacterKitTests`가 이미 검증한다 —
/// 여기서는 이 키트가 그 계약을 올바르게 쓰는지만 본다.
/// </summary>
public class BladeCombatTests
{
    [SetUp]
    public void Setup() => ProfileService.Reset();

    [TearDown]
    public void Teardown()
    {
        ProfileService.Reset();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name.StartsWith("Test")) Object.DestroyImmediate(go);
        }
    }

    static GameObject NewBlade(Vector3 pos)
    {
        var go = new GameObject("TestBlade");
        go.transform.position = pos;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CharacterMover2D>();
        go.AddComponent<PlayerHealth>();
        go.AddComponent<BladeCombat>();
        return go;
    }

    static GameObject NewEnemy(Vector3 pos, float maxHp = 1_000_000f)
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.transform.position = pos;
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        var move = go.AddComponent<EnemyMove>();
        var health = go.AddComponent<EnemyHealth>();
        health.SetMaxHp(maxHp);
        typeof(EnemyMove).GetField("spawnProtectTimer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(move, 0f);
        go.GetComponent<Rigidbody2D>().gravityScale = 0f;
        return go;
    }

    static void SpinAttack(BladeCombat blade, float rawMs)
    {
        var m = typeof(BladeCombat).GetMethod("SpinAttack", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "SpinAttack()이 없다 — 시그니처가 바뀌었나?");
        m.Invoke(blade, new object[] { rawMs });
    }

    static float SpeedDamageMultiplier(BladeCombat blade, float rawMs)
    {
        var m = typeof(BladeCombat).GetMethod("SpeedDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "SpeedDamageMultiplier()가 없다");
        return (float)m.Invoke(blade, new object[] { rawMs });
    }

    static void ApplyDirectionalInvuln(BladeCombat blade, bool held)
    {
        var m = typeof(BladeCombat).GetMethod("ApplyDirectionalInvuln", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "ApplyDirectionalInvuln()이 없다");
        m.Invoke(blade, new object[] { held });
    }

    [UnityTest]
    public IEnumerator BladeKit_DeclaresInertialMoveMode()
    {
        var go = NewBlade(Vector3.zero);
        yield return null;

        var kit = (ICharacterKit)go.GetComponent<BladeCombat>();
        Assert.AreEqual(CharacterId.Blade, kit.Character);
        Assert.AreEqual(CharacterMover2D.MoveMode.Inertial, kit.RequiredMoveMode,
            "섬영만 관성 이동이어야 한다 — 나머지 셋은 즉시-속도");
    }

    [UnityTest]
    public IEnumerator SpinAttack_HitsEnemyInRadius_ButNotOutside()
    {
        var go = NewBlade(new Vector3(2f, 0.5f, 0f));
        var blade = go.GetComponent<BladeCombat>();
        var near = NewEnemy(new Vector3(2.8f, 0.5f, 0f));  // 반경 1.05 안(거리 0.8)
        var far = NewEnemy(new Vector3(4f, 0.5f, 0f));     // 반경 밖(거리 2.0)
        yield return null;

        SpinAttack(blade, 1f); // 정지 상태(속도 배수 1.0)로 호출

        Assert.Less(near.GetComponent<EnemyHealth>().CurrentHp, near.GetComponent<EnemyHealth>().MaxHp,
            "반경 안 적이 안 맞았다");
        Assert.AreEqual(far.GetComponent<EnemyHealth>().MaxHp, far.GetComponent<EnemyHealth>().CurrentHp, 0.001f,
            "반경 밖 적이 맞았다 — 판정 반경(1.05, spin.r 105px)이 잘못됐다");
    }

    [UnityTest]
    public IEnumerator SpeedDamageMultiplier_IsOneAtRest_AndApproachesOriginalCapAtTopSpeed()
    {
        var go = NewBlade(Vector3.zero);
        var blade = go.GetComponent<BladeCombat>();
        var rb = go.GetComponent<Rigidbody2D>();
        var mover = go.GetComponent<CharacterMover2D>();
        yield return null;

        rb.linearVelocity = Vector2.zero;
        Assert.AreEqual(1f, SpeedDamageMultiplier(blade, 1f), 0.001f, "정지 상태의 피해 배수는 원본대로 ×1이어야 한다");

        // statMs 100%(rawMs=1)에서 최고 속도로 달리는 중 — 원본 spdDmg=1.4 → ×2.4
        rb.linearVelocity = new Vector2(mover.inertialMaxSpeed, 0f);
        Assert.AreEqual(2.4f, SpeedDamageMultiplier(blade, 1f), 0.001f,
            "최고 속도 피해 배수가 원본(1 + spdDmg, :2439)과 다르다");
    }

    [UnityTest]
    public IEnumerator SpeedDamageMultiplier_AddsSurplusFromMsOverCap()
    {
        var go = NewBlade(Vector3.zero);
        var blade = go.GetComponent<BladeCombat>();
        var rb = go.GetComponent<Rigidbody2D>();
        var mover = go.GetComponent<CharacterMover2D>();
        yield return null;

        // rawMs 1.3 — 100%를 넘긴 0.3만큼이 속도 대신 피해로 간다(원본 bladeMsSurplus, :2428).
        // MoveScale은 1로 잘리므로 실제 최고 속도는 그대로다.
        rb.linearVelocity = new Vector2(mover.inertialMaxSpeed, 0f);
        float mult = SpeedDamageMultiplier(blade, 1.3f);
        // 1 + 1.0 × (1.4 + 0.3 × 1.4) = 1 + 1.82 = 2.82
        Assert.AreEqual(2.82f, mult, 0.001f, "이속 100% 초과분이 피해로 환산되지 않았다(spdDmgPerMs, :2441)");
    }

    [UnityTest]
    public IEnumerator SpinAttack_HealsPlayerByLifestealPercent_OfTotalDamageDealt()
    {
        var go = NewBlade(new Vector3(2f, 0.5f, 0f));
        var blade = go.GetComponent<BladeCombat>();
        var health = go.GetComponent<PlayerHealth>();
        health.TakeDamage(50f, null); // 풀피에선 회복 여부를 못 보므로 먼저 깎아둔다
        float hpBeforeAttack = health.CurrentHp;
        NewEnemy(new Vector3(2.8f, 0.5f, 0f));
        yield return null;

        SpinAttack(blade, 1f);

        Assert.Greater(health.CurrentHp, hpBeforeAttack,
            "적을 벴는데 체력이 안 늘었다 — 원본 흡혈(lifesteal 0.15, :2498)이 안 걸렸다");
    }

    [UnityTest]
    public IEnumerator DirectionalInvuln_BlocksDamage_WhileHeld()
    {
        var go = NewBlade(Vector3.zero);
        var blade = go.GetComponent<BladeCombat>();
        var health = go.GetComponent<PlayerHealth>();
        yield return null;

        ApplyDirectionalInvuln(blade, true);
        Assert.Greater(health.InvulnRemaining, 0f, "방향키를 누르고 있는데 무적이 안 걸렸다(bladeDirInvuln, :2448)");

        float hpBefore = health.CurrentHp;
        health.TakeDamage(10f, null);
        Assert.AreEqual(hpBefore, health.CurrentHp, 0.001f, "방향키 무적 중인데 피해를 받았다");
    }

    [UnityTest]
    public IEnumerator DirectionalInvuln_DoesNothing_WhenNoDirectionHeld()
    {
        var go = NewBlade(Vector3.zero);
        var blade = go.GetComponent<BladeCombat>();
        var health = go.GetComponent<PlayerHealth>();
        yield return null;

        ApplyDirectionalInvuln(blade, false);
        Assert.AreEqual(0f, health.InvulnRemaining, 0.001f, "방향키를 안 눌렀는데 무적이 걸렸다");
    }
}
}
