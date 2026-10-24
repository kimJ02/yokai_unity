using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YokaiFront.Core;
using YokaiFront.World;

namespace YokaiFront.Editor
{
    /// <summary>
    /// 1스테이지 화면을 PNG로 찍는다 — 배치 테스트는 그래픽 없이(-nographics) 돌아 화면을 못 보니, 눈으로 확인할 그림을 만든다.
    /// 씬을 열기만 하고 저장하지 않는다. `PixelPerfectCamera`와 같은 결과가 되게 960×540으로 그린다(키우는 건 보는 쪽에서 2배).
    ///
    /// 배치 실행(그래픽 필요 — `-nographics` 빼고):
    ///   Unity.exe -batchmode -projectPath . -executeMethod YokaiFront.Editor.CaptureStagePreview.Run -captureOut &lt;폴더&gt; -quit
    /// </summary>
    public static class CaptureStagePreview
    {
        public static void Run()
        {
            string outDir = ArgValue("-captureOut") ?? Path.Combine(Directory.GetCurrentDirectory(), "Temp", "StageCapture");
            Directory.CreateDirectory(outDir);

            EditorSceneManager.OpenScene("Assets/Scenes/CombatCore.unity", OpenSceneMode.Single);
            FieldLayout.SetBossArena(false);
            var cam = Camera.main;
            var player = GameObject.Find("Player");
            FitPlayerSprite(player);

            var visuals = new GameObject("StageVisuals").AddComponent<StageVisuals>();
            visuals.Build();

            // (이름, 플레이어 x, 발 높이) — 땅 / 1층 발판 / 필드 오른쪽 끝
            Shot(cam, visuals, player, outDir, "1_땅", 4.0f, FieldBounds.GroundY);
            Shot(cam, visuals, player, outDir, "2_발판1층", FieldLayout.Platforms[0, 0], FieldLayout.PlatformTopY(0));
            Shot(cam, visuals, player, outDir, "3_오른쪽끝", FieldBounds.MaxX - 2f, FieldBounds.GroundY);
            Debug.Log("[CaptureStagePreview] 저장: " + outDir);
            EditorApplication.Exit(0);
        }

        static void Shot(Camera cam, StageVisuals visuals, GameObject player, string dir, string name, float x, float feetY)
        {
            if (player != null) player.transform.position = new Vector3(x, feetY + EntitySizeConfig.PlayerRadius, 0f);
            float camX = Mathf.Clamp(x, FieldBounds.MinX + StageArtConfig.ViewHalfWidth, FieldBounds.MaxX - StageArtConfig.ViewHalfWidth);
            cam.transform.position = new Vector3(camX, CameraFollow2D.DesiredY(feetY), -10f);
            cam.orthographicSize = StageArtConfig.ViewHalfHeight;
            visuals.transform.Find("Background").position = StageVisuals.BackgroundPosition(camX, cam.transform.position.y);

            var rt = new RenderTexture(StageArtConfig.RefWidth, StageArtConfig.RefHeight, 24, RenderTextureFormat.ARGB32)
            { filterMode = FilterMode.Point };
            cam.targetTexture = rt;
            // 배치 모드 편집 화면 렌더에선 SRP 배처가 켜져 있으면 모든 스프라이트가 첫 텍스처(배경)로 찍혔다(2026-10-24).
            // 캡처할 때만 끄고 되돌린다 — 게임 설정은 안 바뀐다.
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            bool batcher = urp != null && urp.useSRPBatcher;
            if (urp != null) urp.useSRPBatcher = false;
            cam.Render();
            if (urp != null) urp.useSRPBatcher = batcher;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        /// <summary>실행 중 `PlayerRig.ApplySprite`가 하는 크기 맞추기를 편집 모드에서 똑같이(64px·128px/유닛이면 1배).</summary>
        static void FitPlayerSprite(GameObject player)
        {
            if (player == null) return;
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            var rig = player.GetComponent<YokaiFront.Characters.PlayerRig>();
            if (sr != null && rig != null && rig.characterSprites != null && rig.characterSprites.Length > 0 && rig.characterSprites[0] != null)
            {
                sr.sprite = rig.characterSprites[0]; // 실행 중엔 PlayerRig가 고른 캐릭터 그림으로 바꾼다
                sr.color = Color.white;               // 정식 그림이라 덧칠 없음(PlayerRig.characterColors가 흰색)
            }
            if (sr == null || sr.sprite == null) return;
            float h = sr.sprite.bounds.size.y;
            float s = EntitySizeConfig.PlayerHeight / h;
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
