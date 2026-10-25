using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Characters;
using YokaiFront.Core;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>마법사 차지 마법진(2026-10-25) — 갈래별 종류, 차지할수록 커지다 차지가 차면 멈춘다, 차지 중에만 보인다.</summary>
public class MageChargeCircleTests
{
    [SetUp]
    public void Setup()
    {
        ProfileService.Reset();
        GameState.Set(GameScene.Run);
        ProfileService.Current.character = CharacterId.Mage;
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go != null && go.transform.parent == null && go.name.StartsWith("Test")) Object.DestroyImmediate(go);
        ProfileService.Reset();
    }

    [Test]
    public void CircleFor_GravityIsArcane_ExplosionIsFire_NoBranchIsNone()
    {
        Assert.AreEqual("Arcane", MageChargeCircle.CircleFor(MageBranch.Gravity, 1));
        Assert.AreEqual("Fire", MageChargeCircle.CircleFor(MageBranch.Explosion, 1));
        Assert.IsNull(MageChargeCircle.CircleFor(MageBranch.None, 0));
        Assert.IsNull(MageChargeCircle.CircleFor(MageBranch.Gravity, 0), "갈래만 골랐고 층을 안 배웠으면 없다");
    }

    [Test]
    public void ScaleAt_GrowsWithCharge_AndStopsWhenFull()
    {
        Assert.AreEqual(MageChargeCircle.MinScale * MageChargeCircle.MaxScale, MageChargeCircle.ScaleAt(0f), 1e-4f);
        Assert.Less(MageChargeCircle.ScaleAt(0.3f), MageChargeCircle.ScaleAt(0.6f));
        Assert.AreEqual(MageChargeCircle.MaxScale, MageChargeCircle.ScaleAt(1f), 1e-4f);
        Assert.AreEqual(MageChargeCircle.ScaleAt(1f), MageChargeCircle.ScaleAt(5f), "차지가 다 되면 더 안 커진다");
    }

    [Test]
    public void Frames_BothCircles_Have16()
    {
        Assert.AreEqual(16, Resources.LoadAll<Sprite>("Art/Figma/Vfx/MagicCircle/Arcane").Length);
        Assert.AreEqual(16, Resources.LoadAll<Sprite>("Art/Figma/Vfx/MagicCircle/Fire").Length);
    }

    [UnityTest]
    public IEnumerator ShowsOnlyWhileCharging_WithBranch()
    {
        ProfileService.Current.mageBranch = MageBranch.Gravity;
        ProfileService.Current.mageTier = 1;
        var go = new GameObject("TestMage");
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CharacterMover2D>();
        go.AddComponent<PlayerHealth>();
        var mage = go.AddComponent<MageAttack>();
        go.AddComponent<SpriteRenderer>();
        var circle = go.GetComponent<MageChargeCircle>();
        Assert.IsNotNull(circle, "MageAttack이 마법진을 붙인다");
        yield return null;
        Assert.IsFalse(circle.Visible, "차지 전엔 안 보인다");

        // Z를 흉내 낼 수 없어서 차지 상태를 직접 놓고 마법진만 갱신한다(프레임을 넘기면 MageAttack이 "안 누르고 있다"며 쏴 버린다).
        var charging = typeof(MageAttack).GetField("charging", BindingFlags.NonPublic | BindingFlags.Instance);
        var chargeT = typeof(MageAttack).GetField("chargeT", BindingFlags.NonPublic | BindingFlags.Instance);
        charging.SetValue(mage, true);
        chargeT.SetValue(mage, 0.5f);
        circle.Refresh();
        Assert.IsTrue(circle.Visible, "차지 중엔 보인다");
        float half = circle.transform.Find("MageChargeCircle").localScale.x;
        chargeT.SetValue(mage, 1.0f);
        circle.Refresh();
        float full = circle.transform.Find("MageChargeCircle").localScale.x;
        Assert.Greater(full, half, "차지가 차오를수록 커진다");
        chargeT.SetValue(mage, 5f);
        circle.Refresh();
        Assert.AreEqual(full, circle.transform.Find("MageChargeCircle").localScale.x, 1e-5f, "다 차면 더 안 커진다");

        charging.SetValue(mage, false);
        circle.Refresh();
        Assert.IsFalse(circle.Visible, "쏘면 사라진다");
    }
}

}
