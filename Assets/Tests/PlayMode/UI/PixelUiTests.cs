using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Core;
using YokaiFront.UI;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// Figma UI 판(2026-10-24) — 정수배 배율, 버튼 상태 그림, 일시정지·설정 창이 게임 상태를 따라 켜지는지, 설정 창이 ESC를 먼저 쓰는지.
/// 그림이 Figma와 맞는지는 `CaptureUiPreview`로 찍어 Figma 그림과 나란히 본다.
/// </summary>
public class PixelUiTests
{
    PixelCanvas pc;

    [SetUp]
    public void Setup()
    {
        UiModal.Reset();
        GameState.Set(GameScene.Run);
        pc = PixelCanvas.Create();
    }

    [TearDown]
    public void Teardown()
    {
        if (pc != null) Object.DestroyImmediate(pc.gameObject);
        foreach (var go in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go.gameObject);
        UiModal.Reset();
        GameState.Set(GameScene.Run);
    }

    [Test]
    public void Scale_IsLargestWholeMultiple_ShrinksOnlyBelow640x360()
    {
        Assert.AreEqual(3f, PixelCanvas.ScaleFor(1920, 1080), "1080p는 3배");
        Assert.AreEqual(2f, PixelCanvas.ScaleFor(1280, 720));
        Assert.AreEqual(2f, PixelCanvas.ScaleFor(1366, 768), "정수배만 — 2.13배가 아니라 2배");
        Assert.AreEqual(4f, PixelCanvas.ScaleFor(2560, 1440));
        Assert.AreEqual(1f, PixelCanvas.ScaleFor(916, 548), "작은 Game 창");
        Assert.AreEqual(600f / 640f, PixelCanvas.ScaleFor(600, 400), 1e-5f, "640×360보다 작으면 맞춰 줄인다");
    }

    [Test]
    public void ButtonTones_ChangeBrightnessOnly_KeepOutline()
    {
        var fill = new Color(0.58f, 0.63f, 0.72f, 1f);  // Figma 버튼 청회색
        var ink = Px.Ink;                               // 테두리
        Assert.Greater(PxArt.Apply(fill, PxArt.Tone.Hover).r, fill.r, "마우스 올림은 밝게");
        Assert.Less(PxArt.Apply(fill, PxArt.Tone.Pressed).r, fill.r, "눌림은 어둡게");
        Assert.AreEqual(ink, PxArt.Apply(ink, PxArt.Tone.Hover), "테두리는 그대로");
        var off = PxArt.Apply(fill, PxArt.Tone.Disabled);
        Assert.Less(Mathf.Abs(off.r - off.b), Mathf.Abs(fill.r - fill.b), "비활성은 회색 쪽으로");
        Assert.AreEqual(0f, PxArt.Apply(new Color(1f, 1f, 1f, 0f), PxArt.Tone.Hover).a, "투명은 투명");
    }

    [UnityTest]
    public IEnumerator PauseMenu_ShowsOnlyWhilePaused_AndReplacesOldPause()
    {
        var pause = pc.GetComponent<PauseMenu>();
        yield return null;
        Assert.IsFalse(pause.IsShown, "사냥 중엔 안 보인다");
        Assert.IsTrue(PxScreens.Has<PauseMenu>(), "예전 IMGUI 일시정지는 이걸 보고 안 그린다");

        GameState.Set(GameScene.Pause);
        yield return null;
        Assert.IsTrue(pause.IsShown, "일시정지면 보인다");
        Assert.AreEqual(0, pause.NavIndex, "처음엔 ▶(계속)를 고른다");

        GameState.Set(GameScene.Run);
        yield return null;
        Assert.IsFalse(pause.IsShown);
    }

    [UnityTest]
    public IEnumerator Settings_TakesEscapeFirst_SoPauseIsNotResumed()
    {
        GameState.Set(GameScene.Pause);
        var settings = pc.GetComponent<SettingsWindow>();
        SettingsWindow.Open();
        yield return null;
        Assert.IsTrue(settings.IsShown);
        Assert.IsTrue(UiModal.OwnsEscape, "설정 창이 떠 있으면 ESC는 창 몫(RunController가 일시정지를 안 푼다)");

        SettingsWindow.Close(); // ESC로 닫을 때처럼 — 창은 이 프레임 끝(LateUpdate)까지 남아 있다
        Assert.IsTrue(UiModal.OwnsEscape, "닫는 그 프레임의 ESC는 아직 창 몫");
        yield return null;
        yield return null;
        Assert.IsFalse(settings.IsShown);
        Assert.IsFalse(UiModal.OwnsEscape, "닫힌 뒤엔 일시정지 ESC가 다시 돈다");
    }

    [UnityTest]
    public IEnumerator UiModal_StillOwnsEscape_OnTheFrameItClosed()
    {
        UiModal.Push();
        Assert.IsTrue(UiModal.Open);
        UiModal.Pop();
        Assert.IsFalse(UiModal.Open);
        Assert.IsTrue(UiModal.OwnsEscape, "닫은 그 프레임 — Update 순서와 상관없이 같은 ESC로 일시정지가 안 풀리게");
        yield return null;
        Assert.IsFalse(UiModal.OwnsEscape);
    }

    [Test]
    public void FigmaArt_AndFont_AreInResources()
    {
        Assert.IsNotNull(Px.Font, "DNF Bit Bit v2 글꼴");
        foreach (var path in new[] { "Pause/band", "Common/title_plate", "Common/icon_frame", "Common/window_frame",
                                     "Common/button", "Common/button_small", "Icons/play", "Icons/gear", "Icons/exit",
                                     "Icons/close" })
        {
            var s = PxArt.Get(path);
            Assert.IsNotNull(s, path);
            Assert.AreEqual(FilterMode.Point, s.texture.filterMode, path + " — 도트가 번지면 안 된다");
            Assert.IsTrue(s.texture.isReadable, path + " — 버튼 상태 그림을 만들려면 읽을 수 있어야 한다");
        }
        Assert.AreEqual(new Vector2(640f, 128f), PxArt.Get("Pause/band").rect.size, "Figma 원본 크기 그대로");
    }
}

}
