using NUnit.Framework;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 윤회(輪廻) — 원본 `doRebirth()`(project_test.html:6509)와 윤회 장벽(`:741`·`:973`~`:977`).
///
/// 입장료와 몹 체력이 지역당 지수로 오르는 구조라 **한 생의 강화만으로는 반드시 벽에 부딪히게**
/// 설계돼 있고, 그 벽을 넘는 유일한 수단이 윤회다. 보상 공식과 초기화 범위를 정확히 못 박는다.
/// </summary>
public class RebirthTests
{
    [SetUp]
    public void Setup() { ProfileService.Reset(); RunState.Reset(); CombatModifiers.Reset(); }

    [TearDown]
    public void Teardown() { ProfileService.Reset(); RunState.Reset(); CombatModifiers.Reset(); }

    /// <summary>
    /// 원본 `rpOfRegion(r) = max(1, floor(r*r*0.6))`(:6502) — 주석에 적힌 1·2·5·9·15·21·29·38·48과
    /// 정확히 맞는지 본다. 뒤로 갈수록 가파른 게 요점이라 선형으로 바꾸면 윤회 동기가 사라진다.
    /// </summary>
    [Test]
    public void RpOfRegion_MatchesOriginalCurve()
    {
        int[] expected = { 1, 2, 5, 9, 15, 21, 29, 38, 48 };
        for (int r = 1; r <= 9; r++)
            Assert.AreEqual(expected[r - 1], RebirthConfig.RpOfRegion(r), $"{r}지역 RP");
    }

    /// <summary>원본 `regionReqRebirth(r) = max(0, ceil((r-2)/2))`(:973).</summary>
    [Test]
    public void RequiredRebirths_MatchesOriginal()
    {
        int[] expected = { 0, 0, 1, 1, 2, 2, 3, 3, 4 };
        for (int r = 1; r <= 9; r++)
            Assert.AreEqual(expected[r - 1], RebirthConfig.RequiredRebirths(r), $"{r}지역 권장 윤회");
    }

    /// <summary>
    /// 윤회 장벽은 **세 개가 동시에** 걸린다(몹 체력↑·몹 피해↑·내 피해↓).
    /// 하나만 구현하면 "조금 어렵다" 수준이 되어 윤회를 강제하지 못한다.
    /// </summary>
    [Test]
    public void RebirthWall_AppliesAllThreeMultipliers()
    {
        // 5지역은 권장 2회. 0회면 2회 부족.
        Assert.AreEqual(2, RebirthConfig.Gap(5, 0));
        Assert.AreEqual(Mathf.Pow(2.8f, 2), RebirthConfig.WallEnemyHp(5, 0), 0.01f);
        Assert.AreEqual(Mathf.Pow(1.45f, 2), RebirthConfig.WallEnemyDamage(5, 0), 0.01f);
        Assert.AreEqual(Mathf.Pow(0.62f, 2), RebirthConfig.WallPlayerDamage(5, 0), 0.001f);

        // 권장을 채우면 벽이 사라진다.
        Assert.AreEqual(1f, RebirthConfig.WallEnemyHp(5, 2), 0.001f);
        Assert.AreEqual(1f, RebirthConfig.WallPlayerDamage(5, 2), 0.001f);
    }

    /// <summary>권장보다 많이 윤회해도 **추가 이득은 없다**(장벽이 음수로 안 간다).</summary>
    [Test]
    public void RebirthWall_DoesNotRewardOverRebirthing()
    {
        Assert.AreEqual(1f, RebirthConfig.WallPlayerDamage(3, 99), 0.001f);
        Assert.AreEqual(0, RebirthConfig.Gap(3, 99));
    }

    /// <summary>윤회 장벽이 실제 피해 배수에 반영되는지 — 계산만 하고 안 곱하면 의미가 없다.</summary>
    [Test]
    public void RebirthWall_ReachesDamageMultiplier()
    {
        RunState.Begin(5, RunMode.Normal); // 권장 2회 지역, 윤회 0회
        Assert.AreEqual(Mathf.Pow(0.62f, 2), CombatModifiers.DamageMultiplier, 0.001f,
            "윤회 장벽이 내 피해에 안 걸리고 있다");

        ProfileService.Current.rebirths = 2;
        Assert.AreEqual(1f, CombatModifiers.DamageMultiplier, 0.001f);
    }

    /// <summary>
    /// 보상은 **이번 생에 정복한 지역**만으로 정해진다(원본 `rpPreview` :6505).
    /// 원본 주석 그대로 "갈아넣은 시간이 아니라 어디까지 뚫었나가 보상이다".
    /// </summary>
    [Test]
    public void RebirthPreview_CountsOnlyClearedRegions()
    {
        var p = ProfileService.Current;
        Assert.AreEqual(0, p.RebirthPointPreview());

        p.MarkBossCleared(1);
        p.MarkBossCleared(2);
        Assert.AreEqual(RebirthConfig.RpOfRegion(1) + RebirthConfig.RpOfRegion(2), p.RebirthPointPreview());
        Assert.AreEqual(2, p.ClearedRegionCount());
    }

    /// <summary>
    /// 윤회하면 이번 생 진행은 전부 초기화되고 **포인트와 윤회 횟수만 남는다**(원본 :6519~:6531).
    /// 여기서 무엇이 남고 무엇이 사라지는지가 게임의 성장 구조 그 자체다.
    /// </summary>
    [Test]
    public void DoRebirth_ResetsLifeButKeepsPoints()
    {
        var p = ProfileService.Current;
        p.level = 20; p.exp = 500; p.gold = 99999;
        p.upgrades.atk = 30; p.upgrades.crit = 12;
        p.spUsed = 6; p.mageBranch = MageBranch.Gravity; p.mageTier = 3;
        p.MarkBossCleared(1); p.MarkBossCleared(2); p.MarkBossCleared(3);
        for (int i = 0; i < 40; i++) p.RegisterRegionKill(2);

        int expected = RebirthConfig.RpOfRegion(1) + RebirthConfig.RpOfRegion(2) + RebirthConfig.RpOfRegion(3);
        int gained = p.DoRebirth();

        Assert.AreEqual(expected, gained);
        // 남는 것
        Assert.AreEqual(expected, p.rp);
        Assert.AreEqual(1, p.rebirths);
        // 사라지는 것
        Assert.AreEqual(1, p.level);
        Assert.AreEqual(0, p.exp);
        Assert.AreEqual(0, p.gold);
        Assert.AreEqual(0, p.upgrades.atk);
        Assert.AreEqual(0, p.upgrades.crit);
        Assert.AreEqual(0, p.spUsed);
        Assert.AreEqual(MageBranch.None, p.mageBranch);
        Assert.AreEqual(0, p.mageTier);
        Assert.IsFalse(p.IsBossCleared(1));
        Assert.AreEqual(0, p.RegionKills(2));
        Assert.IsFalse(p.IsRegionOpen(2), "지역 진행이 초기화됐으면 2지역은 다시 잠겨야 한다");
    }

    /// <summary>
    /// 정복한 지역이 없으면 윤회가 **일어나지 않는다**(원본 `if (gain &lt; 1) return`).
    /// 얻을 것 없이 진행만 날리는 사고를 막는 안전장치다.
    /// </summary>
    [Test]
    public void DoRebirth_RefusesWhenNothingCleared()
    {
        var p = ProfileService.Current;
        p.level = 15; p.gold = 500;

        Assert.AreEqual(0, p.DoRebirth());
        Assert.AreEqual(15, p.level, "윤회가 거부됐는데 진행이 날아갔다");
        Assert.AreEqual(500, p.gold);
        Assert.AreEqual(0, p.rebirths);
    }

    /// <summary>윤회를 거듭하면 포인트가 누적된다.</summary>
    [Test]
    public void DoRebirth_AccumulatesPointsAcrossLives()
    {
        var p = ProfileService.Current;
        p.MarkBossCleared(1);
        int first = p.DoRebirth();

        p.MarkBossCleared(1);
        p.MarkBossCleared(2);
        int second = p.DoRebirth();

        Assert.AreEqual(first + second, p.rp);
        Assert.AreEqual(2, p.rebirths);
    }
}

}
