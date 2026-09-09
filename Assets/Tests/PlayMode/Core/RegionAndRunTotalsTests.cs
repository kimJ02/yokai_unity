using NUnit.Framework;
using UnityEngine;
using YokaiFront.Core;
using YokaiFront.Systems;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 로비가 읽는 값들 — 지역 표(입장료·해금)와 이번 런 집계(결과 화면에 뜨는 골드/경험치/처치수).
/// UI 자체(IMGUI)는 테스트하지 않고, **UI가 부르는 계산과 그 계산에 값을 넣어주는 배선**을 본다.
/// </summary>
public class RegionAndRunTotalsTests
{
    [SetUp]
    public void Setup() => ResetStatics();

    [TearDown]
    public void Teardown()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name.StartsWith("Test")) Object.DestroyImmediate(go);
        }
        ResetStatics();
    }

    static void ResetStatics()
    {
        RunState.Reset();
        RunProgress.Reset();
        RunTransient.Reset();
        ProfileService.Current = new PlayerProfile();
    }

    // ────────────────────────── 지역 표 ──────────────────────────

    /// <summary>
    /// 원본 `entryFeeOf(r) = r <= 1 ? 0 : floor(300 × 2.2^(r-2))`(project_test.html:6438).
    /// **지역당 지수로 오르는 게 요점**이다 — 결국 감당이 안 되고 그게 윤회를 강제하는 구조라
    /// 완만하게 고치면 게임의 진행 압력이 통째로 사라진다.
    /// </summary>
    [Test]
    public void EntryFee_MatchesOriginalExponentialCurve()
    {
        Assert.AreEqual(0, RegionConfig.EntryFee(1), "1지역은 무료다");
        Assert.AreEqual(300, RegionConfig.EntryFee(2));
        Assert.AreEqual(Mathf.FloorToInt(300f * 2.2f), RegionConfig.EntryFee(3));
        Assert.AreEqual(Mathf.FloorToInt(300f * Mathf.Pow(2.2f, 7)), RegionConfig.EntryFee(9));
    }

    /// <summary>원본 `regionBaseLv(r) = (r-1)*3 + 1`(project_test.html:972).</summary>
    [Test]
    public void RecommendedLevel_MatchesOriginal()
    {
        Assert.AreEqual(1, RegionConfig.RecommendedLevel(1));
        Assert.AreEqual(4, RegionConfig.RecommendedLevel(2));
        Assert.AreEqual(25, RegionConfig.RecommendedLevel(9));
    }

    /// <summary>
    /// 원본 `regionOpen(r) = r === 1 || regions[r-1].bossCleared`(project_test.html:6434).
    /// 임시 전체 해금 스위치를 끄면 원본 규칙이 그대로 나와야 한다 — 보스를 구현할 때
    /// 그 스위치만 지우면 되도록 여기서 못 박아 둔다.
    /// </summary>
    [Test]
    public void RegionOpen_RequiresPreviousBossCleared()
    {
        var profile = new PlayerProfile { debugUnlockAllRegions = false };

        Assert.IsTrue(profile.IsRegionOpen(1), "1지역은 항상 열려 있다");
        Assert.IsFalse(profile.IsRegionOpen(2), "1지역 보스를 안 잡았는데 2지역이 열렸다");

        profile.regionBossCleared[0] = true; // 1지역 보스 격파
        Assert.IsTrue(profile.IsRegionOpen(2));
        Assert.IsFalse(profile.IsRegionOpen(3), "2지역 보스는 아직인데 3지역이 열렸다");
    }

    // ────────────────────────── 런 집계 ──────────────────────────

    /// <summary>원본 `run.fury++`는 보스 포함 전부, `run.kills++`는 보스 제외(:1825·:1857).</summary>
    [Test]
    public void BossKill_CountsFuryButNotKills()
    {
        RunState.Begin(1, RunMode.Boss);

        RunState.RegisterKill(isBoss: false);
        RunState.RegisterKill(isBoss: true);

        Assert.AreEqual(1, RunState.Kills, "보스는 처치 수에 안 들어간다");
        Assert.AreEqual(2, RunState.Fury, "살기는 보스도 센다");
    }

    /// <summary>새 런을 시작하면 지난 런 집계가 남아 있으면 안 된다(원본 `startRun`의 초기화, :4307).</summary>
    [Test]
    public void BeginRun_ClearsPreviousTotals()
    {
        RunState.Begin(1, RunMode.Normal);
        RunState.RegisterKill(isBoss: false);
        RunState.RegisterReward(500, 900);

        RunState.Begin(2, RunMode.Normal);

        Assert.AreEqual(0, RunState.Kills);
        Assert.AreEqual(0, RunState.GoldEarned);
        Assert.AreEqual(0, RunState.ExpEarned);
        Assert.AreEqual(2, RunState.Region);
        Assert.AreEqual(RunState.NormalTime, RunState.TimeLeft, 0.01f);
    }
}

}
