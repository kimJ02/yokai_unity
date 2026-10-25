using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Characters;
using YokaiFront.Core;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 마법사 차지 — 2026-10-25 사용자: "차지 단계마다 플레이어가 깜빡이게, 마법진은 없애"와 "로비 들어갔을 때 스킬이 한 번 써진다".
/// 단계는 원본 강화탄(50%)·완충(99%) 문턱. 사냥 중이 아니면 차지를 쏘지 않는다(메뉴의 Z "확인"이 로비에서 탄으로 나가던 버그).
/// </summary>
public class MageChargeFlashTests
{
    static readonly FieldInfo Charging = typeof(MageAttack).GetField("charging", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo ChargeT = typeof(MageAttack).GetField("chargeT", BindingFlags.NonPublic | BindingFlags.Instance);

    [SetUp]
    public void Setup()
    {
        ProfileService.Reset();
        GameState.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go != null && go.transform.parent == null && (go.name.StartsWith("Test") || go.GetComponent<MageProjectile>() != null))
                Object.DestroyImmediate(go);
        ProfileService.Reset();
        GameState.Reset();
    }

    static MageAttack NewMage()
    {
        var go = new GameObject("TestMage");
        var tex = new Texture2D(8, 8);
        go.AddComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
        go.AddComponent<CircleCollider2D>().radius = 0.25f;
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CharacterMover2D>();
        go.AddComponent<PlayerHealth>();
        return go.AddComponent<MageAttack>();
    }

    [Test]
    public void Stages_AreOriginalChargedAndFullThresholds()
    {
        Assert.AreEqual(0, MageChargeFlash.StageAt(0.49f));
        Assert.AreEqual(1, MageChargeFlash.StageAt(0.5f), "원본 강화탄 `chargeK >= 0.5`");
        Assert.AreEqual(1, MageChargeFlash.StageAt(0.98f));
        Assert.AreEqual(2, MageChargeFlash.StageAt(0.99f), "원본 완충 `chargeK >= 0.99`");
    }

    [Test]
    public void FlashesOncePerStage_NotEveryFrame()
    {
        var mage = NewMage();
        var flash = mage.GetComponent<MageChargeFlash>();
        Assert.IsNotNull(flash, "MageAttack이 깜빡임을 붙인다");
        Charging.SetValue(mage, true);

        ChargeT.SetValue(mage, 0.3f);
        flash.Refresh(0.016f);
        Assert.AreEqual(0f, flash.FlashAmount, "아직 한 단계도 안 찼다");

        ChargeT.SetValue(mage, 0.55f);
        flash.Refresh(0.016f);
        Assert.AreEqual(1f, flash.FlashAmount, "50%를 넘는 순간 번쩍인다");
        var sr = mage.transform.Find("ChargeFlash").GetComponent<SpriteRenderer>();
        Assert.IsTrue(sr.enabled);
        Assert.AreSame(mage.GetComponent<SpriteRenderer>().sprite, sr.sprite, "몸 모양 그대로 하얗게");

        for (int i = 0; i < 20; i++) flash.Refresh(0.016f);
        Assert.AreEqual(0f, flash.FlashAmount, "한 번 번쩍이고 사라진다(같은 단계에선 다시 안 번쩍)");
        Assert.IsFalse(sr.enabled);

        ChargeT.SetValue(mage, 1f);
        flash.Refresh(0.016f);
        Assert.AreEqual(1f, flash.FlashAmount, "완충(99%)에서 한 번 더");
    }

    [Test]
    public void ChargeCircle_IsGone()
    {
        var mage = NewMage();
        Assert.IsNull(System.Type.GetType("YokaiFront.Characters.MageChargeCircle, YokaiFront.Characters"), "마법진은 뺐다");
        Assert.IsNull(mage.transform.Find("MageChargeCircle"));
        Assert.IsNull(mage.transform.Find("ChargeIndicator"), "예전 하늘색 구체도 없다");
    }

    [UnityTest]
    public IEnumerator OutsideRun_HeldCharge_DoesNotFire_InsideRun_ItDoes()
    {
        var mage = NewMage();
        yield return null;
        Charging.SetValue(mage, true);
        ChargeT.SetValue(mage, 0.6f);

        // 로비(시간 멈춤) — Z를 안 누른 상태로 읽히지만 쏘면 안 된다
        yield return null;
        yield return null;
        Assert.AreEqual(0, Object.FindObjectsByType<MageProjectile>(FindObjectsSortMode.None).Length, "로비에서 탄이 나가면 안 된다");
        Assert.IsTrue(mage.IsCharging, "차지는 그대로 멈춰 둔다");

        GameState.Set(GameScene.Run);
        yield return null;
        Assert.IsFalse(mage.IsCharging, "사냥 중엔 Z를 뗀 것이라 쏜다");
        Assert.Greater(Object.FindObjectsByType<MageProjectile>(FindObjectsSortMode.None).Length, 0);
    }

    [Test]
    public void GameState_CountsChanges_AndRemembersPrevious()
    {
        int v = GameState.Version;
        GameState.Set(GameScene.Run);
        GameState.Set(GameScene.Pause);
        Assert.AreEqual(v + 2, GameState.Version);
        Assert.AreEqual(GameScene.Run, GameState.Previous, "일시정지에서 돌아올 땐 키를 안 잠근다 — 전 화면을 기억해야 안다");
        GameState.Set(GameScene.Pause);
        Assert.AreEqual(v + 2, GameState.Version, "같은 화면이면 안 바뀐다");
    }
}

}
