using UnityEditor;
using UnityEngine;

namespace YokaiFront.Editor
{
    /// <summary>
    /// Figma에서 받은 도트 그림(`Assets/Resources/Art/Figma/`)의 가져오기 설정 — 2026-10-24 C안(1유닛 = 128px).
    ///
    /// Point 필터·무압축·밉맵 없음은 <see cref="BuildUiTextures"/>와 같은 이유다(도트가 도트로 보이는 최소 조건).
    /// 픽셀/유닛은 파일 이름으로 정한다 — `ground_*`는 64(Figma가 32px 원본을 2배로 깔았다), 나머지는 128.
    /// 타일로 늘려 그리는 그림이 있어서 메시는 `FullRect`여야 한다(아니면 `SpriteRenderer` 타일 모드가 경고만 내고 늘린다).
    /// 모양(`textureShape`)은 반드시 2D로 박는다 — 메타가 비어 있는 새 그림은 큐브맵으로 들어가서 `Resources.Load`가 못 찾았다
    /// (2026-10-24). 설정을 바꾸면 <see cref="GetVersion"/>을 올려야 이미 들어온 그림도 다시 가져온다.
    /// </summary>
    public class FigmaArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Resources/Art/Figma/";

        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = System.IO.Path.GetFileName(assetPath).StartsWith("ground_") ? 64f : 128f;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.textureShape = TextureImporterShape.Texture2D;
            ti.SetTextureSettings(settings);
            ti.textureShape = TextureImporterShape.Texture2D;
        }
    }
}
