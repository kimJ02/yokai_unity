using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YokaiFront.Core;
using YokaiFront.UI;
using YokaiFront.World;

namespace YokaiFront.Editor
{
    /// <summary>
    /// Figma UI 화면을 PNG로 찍는다(2026-10-24) — 1스테이지 게임 화면 위에 UI를 겹쳐서 두 가지로:
    /// `이름_1080.png` = 1920×1080(UI 3배, 실제로 보이는 모습), `이름_360.png` = 640×360(UI 1배, 선택 표시 없이 — Figma 그림과 1:1 비교용).
    /// 씬을 열기만 하고 저장하지 않는다.
    ///
    /// 배치 실행(그래픽 필요 — `-nographics` 빼고):
    ///   Unity.exe -batchmode -projectPath . -executeMethod YokaiFront.Editor.CaptureUiPreview.Run -captureOut &lt;폴더&gt; -quit
    /// </summary>
    public static class CaptureUiPreview
    {
        public static void Run()
        {
            string outDir = ArgValue("-captureOut") ?? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "UiCapture");
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene("Assets/Scenes/CombatCore.unity", OpenSceneMode.Single);
            FieldLayout.SetBossArena(false);
            var cam = Camera.main;
            var player = GameObject.Find("Player");
            FitPlayerSprite(player);
            var visuals = new GameObject("StageVisuals").AddComponent<StageVisuals>();
            visuals.Build();
            PlaceWorld(cam, visuals, player, 4f, FieldBounds.GroundY);

            var pc = PixelCanvas.Create(cam);
            var pause = pc.GetComponent<PauseMenu>();
            var settings = pc.GetComponent<SettingsWindow>();
            var hud = pc.GetComponent<CombatHud>();
            hud.Preview = new CombatHud.PreviewState
            {
                Hp = 72f, MaxHp = 100f, ExpRatio = 0.62f, TimeLeft = 47f, Combo = 32, Kills = 37,
                Attack = default,
                Skill = YokaiFront.Characters.SkillSlotState.Cooldown(1.2f, 3f),
                Player = new Vector2(4f, FieldBounds.GroundY),
                Enemies = new[] { new Vector2(7f, 0f), new Vector2(9.5f, 1.35f), new Vector2(15f, 0f), new Vector2(18.6f, 2.7f), new Vector2(22f, 4.05f) },
            };

            Shot(pc, cam, outDir, "전투HUD", sel => hud.ShowForCapture(sel));
            // Figma 그림과 같은 값(남은 10초·COMBO 32·체력 가득) — 글자 자리를 1:1로 잰다
            hud.Preview = new CombatHud.PreviewState
            {
                Hp = 100f, MaxHp = 100f, ExpRatio = 0f, TimeLeft = 10f, Combo = 32, Kills = 0,
                Player = new Vector2(4f, FieldBounds.GroundY),
            };
            Shot(pc, cam, outDir, "전투HUD_Figma값", sel => hud.ShowForCapture(sel));
            hud.Preview = new CombatHud.PreviewState
            {
                Hp = 18f, MaxHp = 100f, ExpRatio = 0.9f, TimeLeft = 8f, Combo = 0, Kills = 100,
                Skill = YokaiFront.Characters.SkillSlotState.Cooldown(0f, 3f, 0, 3, noStack: true),
                Player = new Vector2(24.8f, FieldBounds.GroundY), PortalInRange = true, PortalUnlocked = true, BossRatio = 0.55f,
            };
            Shot(pc, cam, outDir, "전투HUD_보스바·포탈", sel => hud.ShowForCapture(sel));

            Shot(pc, cam, outDir, "일시정지", sel => pause.ShowForCapture(sel));
            Shot(pc, cam, outDir, "설정", sel => { pause.ShowForCapture(false); settings.ShowForCapture(sel); });

            var title = pc.GetComponent<TitleMenu>();
            var popup = pc.GetComponent<CommonPopup>();
            Shot(pc, cam, outDir, "타이틀", sel => title.ShowForCapture(sel));
            Shot(pc, cam, outDir, "공용팝업", sel =>
            {
                title.ShowForCapture(false);
                CommonPopup.Confirm("새 원정", "지금까지의 기록이 모두 지워집니다.\n처음부터 새로 시작할까요?", null, "시작");
                popup.ShowForCapture(sel);
            });

            Debug.Log("[CaptureUiPreview] 저장: " + outDir);
            EditorApplication.Exit(0);
        }

        static void Shot(PixelCanvas pc, Camera cam, string dir, string name, System.Action<bool> show)
        {
            HideAll(pc);
            show(true);
            Render(pc, cam, dir, name + "_1080", 1920, 1080, 3f);
            HideAll(pc);
            show(false);
            Render(pc, cam, dir, name + "_360", 640, 360, 1f);
        }

        static void HideAll(PixelCanvas pc)
        {
            foreach (var s in pc.GetComponents<PxScreen>()) s.HideForCapture();
        }

        static void Render(PixelCanvas pc, Camera cam, string dir, string name, int w, int h, float scale)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            cam.targetTexture = rt;
            pc.UseCamera(cam); // 캔버스 크기를 새 그림 크기로 다시 잡게
            pc.Scaler.scaleFactor = scale;
            pc.Canvas.scaleFactor = scale;
            Canvas.ForceUpdateCanvases();

            // 배치 모드 편집 화면 렌더에선 SRP 배처를 끈다(CaptureStagePreview와 같은 이유).
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            bool batcher = urp != null && urp.useSRPBatcher;
            if (urp != null) urp.useSRPBatcher = false;
            cam.Render();
            if (urp != null) urp.useSRPBatcher = batcher;

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        /// <summary>게임 화면 자리 — CaptureStagePreview.Shot과 같은 배치(플레이어·카메라·배경).</summary>
        static void PlaceWorld(Camera cam, StageVisuals visuals, GameObject player, float x, float feetY)
        {
            if (player != null) player.transform.position = new Vector3(x, feetY + EntitySizeConfig.PlayerRadius, 0f);
            float camX = Mathf.Clamp(x, FieldBounds.MinX + StageArtConfig.ViewHalfWidth, FieldBounds.MaxX - StageArtConfig.ViewHalfWidth);
            cam.transform.position = new Vector3(camX, CameraFollow2D.DesiredY(feetY), -10f);
            cam.orthographicSize = StageArtConfig.ViewHalfHeight;
            visuals.transform.Find("Background").position = StageVisuals.BackgroundPosition(camX, cam.transform.position.y);
        }

        static void FitPlayerSprite(GameObject player)
        {
            if (player == null) return;
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            var rig = player.GetComponent<YokaiFront.Characters.PlayerRig>();
            if (sr != null && rig != null && rig.characterSprites != null && rig.characterSprites.Length > 0 && rig.characterSprites[0] != null)
            {
                sr.sprite = rig.characterSprites[0];
                sr.color = Color.white;
            }
            if (sr == null || sr.sprite == null) return;
            float s = EntitySizeConfig.PlayerHeight / sr.sprite.bounds.size.y;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        static string ArgValue(string key)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == key) return args[i + 1];
            return null;
        }
    }
}
