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
/// 드루이드 0차 기본공격 검증 — 원본 `druidClaw()`(project_test.html:3271~3321)의 콤보 판정·마나 획득을
/// 그대로 옮겼는지 확인한다. 늑대 소환·맹금 변신은 0차 범위 밖이라 다루지 않는다(`docs/worksplit.md`).
/// </summary>
public class DruidAttackTests
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

    static GameObject NewDruid(Vector3 pos)
    {
        var go = new GameObject("TestDruid");
        go.transform.position = pos;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CharacterMover2D>();
        go.AddComponent<DruidAttack>();
        return go;
    }

    /// <summary>`GunnerAttackTests.NewEnemy`와 동일 — 반지름 0.5, 스폰 보호 해제.</summary>
    static GameObject NewEnemy(Vector3 pos, float maxHp = 1_000_000f)
    {
        var go = new GameObject("TestEnemy");
        go.tag = "Enemy";
        go.transform.position = pos;
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        var move = go.AddComponent<EnemyMove>();
        var health = go.AddComponent<EnemyHealth>();
        health.SetMaxHp(maxHp); // 죽어서 사라지면 안 되는 테스트(3연타 누적 등)를 위해 넉넉하게
        typeof(EnemyMove).GetField("spawnProtectTimer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(move, 0f);
        go.GetComponent<Rigidbody2D>().gravityScale = 0f;
        return go;
    }

    static void Claw(DruidAttack druid)
    {
        var m = typeof(DruidAttack).GetMethod("Claw", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Claw()가 없다 — 시그니처가 바뀌었나?");
        m.Invoke(druid, null);
    }

    static int ComboIndex(DruidAttack druid) =>
        (int)typeof(DruidAttack).GetField("comboIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(druid);

    static float CdTimer(DruidAttack druid) =>
        (float)typeof(DruidAttack).GetField("cdTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(druid);

    static float ComboTimer(DruidAttack druid) =>
        (float)typeof(DruidAttack).GetField("comboTimer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(druid);

    [UnityTest]
    public IEnumerator DruidKit_DeclaresInstantMoveMode()
    {
        var go = NewDruid(Vector3.zero);
        yield return null;

        var kit = (ICharacterKit)go.GetComponent<DruidAttack>();
        Assert.AreEqual(CharacterId.Druid, kit.Character);
        Assert.AreEqual(CharacterMover2D.MoveMode.Instant, kit.RequiredMoveMode,
            "드루이드는 마법사·메카닉과 같은 즉시-속도 이동이다(맹금 변신의 관성 대시는 0차 범위 밖)");
    }

    [UnityTest]
    public IEnumerator Claw_HitsEnemyAhead_DealsFirstComboDamage_AndAdvancesCombo()
    {
        var go = NewDruid(new Vector3(2f, 0.5f, 0f));
        var druid = go.GetComponent<DruidAttack>();
        var enemy = NewEnemy(new Vector3(2.5f, 0.5f, 0f)); // 기본 facing(+1) 앞쪽, 사거리 안
        yield return null;

        Claw(druid);

        var health = enemy.GetComponent<EnemyHealth>();
        Assert.Less(health.CurrentHp, health.MaxHp, "사거리 안 적이 안 맞았다");
        Assert.AreEqual(1, ComboIndex(druid), "1타를 냈는데 콤보가 2타로 안 넘어갔다");
        Assert.AreEqual(0.85f, druid.LastComboDamageMult, 0.001f, "1타 피해 배수가 원본(0.85, :657)과 다르다");
    }

    [UnityTest]
    public IEnumerator Claw_EnemyBehindOutOfBackReach_NotHit_ButComboStillAdvances()
    {
        var go = NewDruid(new Vector3(2f, 0.5f, 0f));
        var druid = go.GetComponent<DruidAttack>();
        // back(0.18)보다 훨씬 뒤라 판정 상자 밖 — 원본 x0 = p.x - back(:3284, facing>0)
        var enemy = NewEnemy(new Vector3(0.5f, 0.5f, 0f));
        yield return null;

        Claw(druid);

        var health = enemy.GetComponent<EnemyHealth>();
        Assert.AreEqual(health.MaxHp, health.CurrentHp, 0.001f, "사거리 밖 적이 맞았다 — 판정 상자 위치가 잘못됐다");
        // 원본 `p.druidCombo = finisher?0:ci+1`는 맞았는지와 무관하게 실행된다(:3296~3298, `if(!hit)`보다 먼저).
        Assert.AreEqual(1, ComboIndex(druid), "빗나갔는데 콤보가 안 넘어갔다 — 원본은 맞았는지와 무관하게 콤보를 진행시킨다");
        Assert.AreEqual(0f, druid.Mana, 0.001f, "빗나갔는데 마나가 올랐다 — 원본은 맞았을 때만 획득한다(:3298 `if(!hit)return`)");
    }

    [UnityTest]
    public IEnumerator Claw_ThirdHit_IsFinisher_DealsMoreDamage_AndResetsComboAfter()
    {
        var go = NewDruid(new Vector3(2f, 0.5f, 0f));
        var druid = go.GetComponent<DruidAttack>();
        NewEnemy(new Vector3(2.5f, 0.5f, 0f));
        yield return null;

        Claw(druid); // 1타
        Assert.AreEqual(0.85f, druid.LastComboDamageMult, 0.001f);
        Claw(druid); // 2타
        Assert.AreEqual(0.95f, druid.LastComboDamageMult, 0.001f);
        Claw(druid); // 3타 — 마무리
        Assert.AreEqual(1.8f, druid.LastComboDamageMult, 0.001f, "3타(마무리) 피해 배수가 원본(1.8, :659)과 다르다");
        Assert.AreEqual(0, ComboIndex(druid), "마무리 이후엔 다시 1타부터 시작해야 한다(원본 `finisher?0:ci+1`, :3297)");
    }

    [UnityTest]
    public IEnumerator Mana_GainsOnHit_ByOriginalPercentage_CappedAtMax()
    {
        var go = NewDruid(new Vector3(2f, 0.5f, 0f));
        var druid = go.GetComponent<DruidAttack>();
        NewEnemy(new Vector3(2.5f, 0.5f, 0f));
        yield return null;

        Claw(druid);
        // 원본 druidGainPct(atkGainPct=0.08) — 0차 manaMax는 티어가 없어 base(20) 고정(:646,:990~992).
        Assert.AreEqual(20f * 0.08f, druid.Mana, 0.001f, "1회 적중 마나 획득량이 원본(manaMax × 8%)과 다르다");

        for (int i = 0; i < 20; i++) Claw(druid);
        Assert.AreEqual(20f, druid.Mana, 0.001f, "마나가 원본 상한(20)을 넘었다 — druidAddMana의 clamp(:3004)");
    }

    [UnityTest]
    public IEnumerator ResetForRun_FillsManaToStartingValue()
    {
        var go = NewDruid(Vector3.zero);
        var druid = go.GetComponent<DruidAttack>();
        yield return null;

        ((IRunResettable)druid).ResetForRun();

        // 원본 `p.mana = meta.weapon === 'druid' ? CONFIG.druid.mana.cost : 0`(:1526) — 시작부터 만땅(20).
        Assert.AreEqual(20f, druid.Mana, 0.001f, "런 시작 시 드루이드 마나가 원본대로 채워지지 않았다");
    }

    [UnityTest]
    public IEnumerator Cooldown_And_ComboWindow_ScaleWithFinisherAndAttackSpeed()
    {
        var go = NewDruid(new Vector3(2f, 0.5f, 0f));
        var druid = go.GetComponent<DruidAttack>();
        NewEnemy(new Vector3(2.5f, 0.5f, 0f));
        yield return null;

        Claw(druid); // 1타 — attackDur = 0.24
        // 원본 `atkCds.druid = attackDur * 0.92 / max(0.35, statAs())`(:3280), statAs 기본 1.
        Assert.AreEqual(0.24f * 0.92f, CdTimer(druid), 0.001f, "1타 쿨다운이 원본 공식과 다르다");
        Assert.AreEqual(0.24f + 0.5f, ComboTimer(druid), 0.001f, "콤보 창(attackDur+0.5)이 원본과 다르다");

        Claw(druid); // 2타
        Claw(druid); // 3타 — attackDur = 0.24 × 1.5(마무리)
        Assert.AreEqual(0.24f * 1.5f * 0.92f, CdTimer(druid), 0.001f, "마무리 쿨다운이 원본(attackDur×1.5) 공식과 다르다");
    }
}
}
