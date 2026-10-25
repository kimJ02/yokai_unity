using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Combat;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>블랙홀 마법사 착탄 폭발(2026-10-25) — 10프레임 15fps, 지름 = 반경 × 2, 끝나면 사라진다.</summary>
public class GravityImpactVfxTests
{
    [TearDown]
    public void Teardown()
    {
        foreach (var v in Object.FindObjectsByType<GravityImpactVfx>(FindObjectsSortMode.None)) Object.DestroyImmediate(v.gameObject);
    }

    [Test]
    public void FrameAt_Is15Fps_AndClamps()
    {
        Assert.AreEqual(0, GravityImpactVfx.FrameAt(0f, 10));
        Assert.AreEqual(3, GravityImpactVfx.FrameAt(0.21f, 10));
        Assert.AreEqual(9, GravityImpactVfx.FrameAt(5f, 10));
    }

    [UnityTest]
    public IEnumerator Spawn_ScalesToRadius_AndEnds()
    {
        var v = GravityImpactVfx.Spawn(new Vector2(3f, 1f), 1.5f);
        Assert.IsNotNull(v, "프레임 10장이 Resources에 있어야 한다");
        float width = v.Current.bounds.size.x * v.transform.localScale.x;
        Assert.AreEqual(3f, width, 1e-3f, "그림 한 변 = 지름");
        yield return new WaitForSeconds(GravityImpactVfx.Life + 0.15f);
        Assert.IsTrue(v == null, "0.67초 뒤 사라진다");
    }
}

}
