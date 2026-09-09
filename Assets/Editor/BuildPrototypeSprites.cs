using System.IO;
using UnityEditor;
using UnityEngine;

namespace YokaiFront.Editor
{
    /// <summary>
    /// `Assets/Sprites/Prototype/`의 PNG들을 **스프라이트로 import 설정**한다.
    ///
    /// ## 이 그림들이 어디서 왔나
    /// 원본 프로토타입에는 **이미지 파일이 하나도 없다** — 캐릭터·몹·성소·보스가 전부 Canvas 2D
    /// 드로잉 코드로 그려진다(`project_test.html:4482`~`:6215`). 그래서 "에셋을 복사"할 수가 없어서,
    /// 원본을 로컬 서버로 띄우고 각 `draw*()` 함수를 오프스크린으로 호출해 **투명 배경 PNG로 구워냈다.**
    /// (굽는 절차는 `docs/prototype-sprites.md`, 결과 한눈 보기는 `docs/assets/prototype-sprites.png`.)
    ///
    /// **임시 그림이다.** 정식 아트가 나오면 같은 파일명으로 덮어쓰기만 하면 된다 — 코드는 안 바뀐다.
    ///
    /// ## Pixels Per Unit = 100인 이유
    /// 이 프로젝트의 월드 스케일이 **100px = 1유닛**(CLAUDE.md)이고 원본도 픽셀 좌표계라,
    /// PPU를 100으로 두면 구워낸 그림이 **원본과 정확히 같은 크기**로 들어온다.
    ///
    /// 실행: 메뉴 `YokaiFront/Import Prototype Sprites`.
    /// </summary>
    public static class BuildPrototypeSprites
    {
        public const string Folder = "Assets/Sprites/Prototype";
        public const float PixelsPerUnit = 100f;

        [MenuItem("YokaiFront/Import Prototype Sprites")]
        public static void Apply()
        {
            if (!Directory.Exists(Folder))
            {
                Debug.LogWarning($"[BuildPrototypeSprites] {Folder} 가 없다.");
                return;
            }

            int n = 0;
            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                // 벡터로 그린 매끈한 도형이라 Point로 두면 계단이 심하다.
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                // 원본 색을 그대로 보려고 압축을 끈다(스프라이트가 작아서 용량 부담이 없다).
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                // 피벗은 가운데. 콜라이더가 트랜스폼 중심에 있으므로 그림도 중심을 맞춘다.
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);

                importer.SaveAndReimport();
                n++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[BuildPrototypeSprites] 스프라이트 {n}개 import 설정 완료 (PPU {PixelsPerUnit}) → {Folder}");
        }

        /// <summary>이름으로 스프라이트 하나를 가져온다(없으면 null).</summary>
        public static Sprite Load(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{fileName}.png");
    }
}
