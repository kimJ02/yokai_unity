using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Core;
using YokaiFront.UI;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>적 타격 소리(2026-10-25) — 직접 타격 이벤트마다 한 번, 너무 붙으면 건너뛴다, 효과음 0이면 안 난다.</summary>
public class HitSoundTests
{
    int savedSfx;

    [SetUp]
    public void Setup() { savedSfx = GameSettings.SfxVolume; GameSettings.SfxVolume = 8; CombatEvents.Reset(); }

    [TearDown]
    public void Teardown()
    {
        GameSettings.SfxVolume = savedSfx;
        CombatEvents.Reset();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go != null && go.name == "TestHitSound") Object.DestroyImmediate(go);
    }

    [Test]
    public void ClipIsInResources() => Assert.IsNotNull(Resources.Load<AudioClip>(HitSound.ClipPath));

    [UnityTest]
    public IEnumerator EnemyDamaged_PlaysOnce_AndCloseHitsAreSkipped()
    {
        var hs = new GameObject("TestHitSound").AddComponent<HitSound>();
        yield return null;
        CombatEvents.RaiseEnemyDamaged(null);
        CombatEvents.RaiseEnemyDamaged(null); // 같은 프레임 — 건너뜀
        Assert.AreEqual(1, hs.Played);
        yield return new WaitForSecondsRealtime(HitSound.MinGap + 0.05f);
        CombatEvents.RaiseEnemyDamaged(null);
        Assert.AreEqual(2, hs.Played);
    }

    [UnityTest]
    public IEnumerator SfxVolumeZero_IsSilent()
    {
        GameSettings.SfxVolume = 0;
        var hs = new GameObject("TestHitSound").AddComponent<HitSound>();
        yield return null;
        CombatEvents.RaiseEnemyDamaged(null);
        Assert.AreEqual(0, hs.Played);
    }
}

}
