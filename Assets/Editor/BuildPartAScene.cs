using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Part A(필드+카메라+플레이어) 씬을 코드로 조립한다. GUI 클릭 없이
/// `Unity.exe -batchmode -quit -executeMethod BuildPartAScene.Build`로 재현 가능하게 해서,
/// 다른 세션도 이 스크립트만 다시 실행하면 같은 결과를 얻을 수 있다(수동 클릭 산출물이 아님).
/// </summary>
public static class BuildPartAScene
{
    const string SpritePath = "Assets/Sprites/Circle.png";

    [MenuItem("Tools/YokaiFront/Build Part A Scene")]
    public static void Build()
    {
        EnsureCircleSprite();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildFieldBoundary();
        BuildPlayer();

        Directory.CreateDirectory("Assets/Scenes");
        bool ok = EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatCore.unity");
        Debug.Log(ok ? "[BuildPartAScene] CombatCore.unity 저장 완료" : "[BuildPartAScene] 씬 저장 실패");
    }

    static void BuildCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        // 필드 가로 폭 전체가 한 화면에 들어오게 (HANDOFF.md: 카메라 고정, 16:9 가정)
        float width = FieldBounds.MaxX - FieldBounds.MinX;
        cam.orthographicSize = width / 2f / (16f / 9f);
        // 바닥(Y=0)이 화면 아래쪽에 오도록 살짝 위를 본다 — 점프 궤적이 화면 안에 들어오게
        cam.transform.position = new Vector3(0f, cam.orthographicSize * 0.35f, -10f);
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.AddComponent<AudioListener>();
    }

    static void BuildFieldBoundary()
    {
        // 원본처럼 단일 고정 바닥 — 사각형 경계가 아니라 바닥 선 하나만 그린다.
        var go = new GameObject("Ground");
        var lr = go.AddComponent<LineRenderer>();
        float y = FieldBounds.GroundY;
        Vector3[] pts =
        {
            new Vector3(FieldBounds.MinX, y, 0),
            new Vector3(FieldBounds.MaxX, y, 0),
        };
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        lr.widthMultiplier = 0.08f;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.6f, 0.6f, 0.65f, 0.9f);
        lr.sortingOrder = -1;
    }

    static void BuildPlayer()
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = new Vector3(0f, FieldBounds.GroundY, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        sr.color = new Color(0.35f, 0.55f, 1f); // 아군 = 파랑
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        go.AddComponent<CharacterMover2D>();
        go.AddComponent<PlayerAttack>();
    }

    /// <summary>
    /// 절차적으로 채워진 원 스프라이트를 생성한다. 유니티 기본 내장 리소스 중엔
    /// 신뢰할 수 있는 "꽉 찬 원" 스프라이트가 없어서(전부 UI용 라운드사각형) 직접 만든다.
    /// 픽셀당유닛(pixelsPerUnit)을 텍스처 폭과 같게 둬서 스케일 1일 때 지름 1유닛이 되게 한다.
    /// </summary>
    static void EnsureCircleSprite()
    {
        Directory.CreateDirectory("Assets/Sprites");
        if (File.Exists(SpritePath))
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null) return;
        }

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size / 2f;
        Vector2 center = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                // 가장자리 1.5px를 부드럽게 페더링해서 계단현상을 줄인다
                float a = Mathf.Clamp01(r - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
