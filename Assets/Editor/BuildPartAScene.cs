using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Part A(필드+카메라+플레이어) 씬을 코드로 조립한다. GUI 클릭 없이
/// `Unity.exe -batchmode -quit -executeMethod BuildPartAScene.Build`로 재현 가능하게 해서,
/// 다른 세션도 이 스크립트만 다시 실행하면 같은 결과를 얻을 수 있다(수동 클릭 산출물이 아님).
///
/// 이번 개정: 손으로 계산하던 중력/바닥판정을 실제 Physics2D(Rigidbody2D+Collider2D 충돌 해석)로
/// 바꾸고, 원본 NORMAL_PLATFORMS 배치를 그대로 옮긴 실물 발판을 추가했다(원본 3~4층 수직형 맵).
/// 필드 폭(26유닛)이 발판을 다 넣기엔 한 화면에 담기엔 넓어서 카메라를 X-스크롤로 바꿨다.
/// </summary>
public static class BuildPartAScene
{
    const string SpritePath = "Assets/Sprites/Circle.png";
    const string GroundLayer = "Ground";

    // 원본(project_test.html) NORMAL_PLATFORMS의 X 배치(centerX/width)는 그대로 옮김. pl.x는
    // 원본에서 "왼쪽 끝" 좌표였음이 충돌판정 코드(`p.x > pl.x - 6 && p.x < pl.x + pl.w + 6`)로
    // 확인됨 — 중심이 아니다. 100px=1유닛, groundY=620 기준 centerX=(x+w/2)/100 로 환산.
    //
    // Y(층 간격)는 원본 그대로(y=505/395/285/185, 층간 1.0~1.1유닛)가 아니라 사용자 요청으로
    // 층간 1.35유닛으로 넓혔다 — 의도적 편차(점프 최대 높이 1.772유닛 대비 76% 지점, 원본의
    // ~60%보다 여유 있게). "간격이 너무 작다"는 피드백을 반영한 것이라 나중에 원본 값으로
    // 되돌리지 말 것. 아래에서 위로 통과 가능한 원웨이 발판(BuildPlatforms 참고)이라
    // 이 정도 간격에서도 막힘 없이 오갈 수 있다.
    static readonly float[,] Platforms =
    {
        // centerX, centerY, width  (전부 유닛)
        {3.20f, 1.35f, 2.80f}, {9.00f, 1.35f, 3.20f}, {15.40f, 1.35f, 3.00f}, {21.50f, 1.35f, 3.20f}, // 1층
        {5.90f, 2.70f, 3.00f}, {12.30f, 2.70f, 3.20f}, {18.60f, 2.70f, 3.00f}, {24.00f, 2.70f, 2.60f}, // 2층
        {3.30f, 4.05f, 2.60f}, {9.60f, 4.05f, 3.00f}, {16.20f, 4.05f, 3.00f}, {22.00f, 4.05f, 2.60f}, // 3층
        {6.80f, 5.40f, 2.80f}, {13.40f, 5.40f, 3.00f}, {19.70f, 5.40f, 2.80f}, // 4층
    };
    const float PlatformThickness = 0.15f;

    [MenuItem("Tools/YokaiFront/Build Part A Scene")]
    public static void Build()
    {
        EnsureCircleSprite();
        EnsureGroundLayer();
        Physics2D.gravity = new Vector2(0f, -26f); // 원본 2600px/s² → 26 (100px=1유닛)

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildGround();
        BuildPlatforms();
        var player = BuildPlayer();
        BuildCamera(player.transform);

        Directory.CreateDirectory("Assets/Scenes");
        bool ok = EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatCore.unity");
        Debug.Log(ok ? "[BuildPartAScene] CombatCore.unity 저장 완료" : "[BuildPartAScene] 씬 저장 실패");

        RegisterAsDefaultScene();
    }

    /// <summary>
    /// 배치모드로 씬을 만들기만 하면 "다음에 에디터를 열었을 때 이 씬이 뜨는" 상태가 저절로 안 남는다
    /// (LastSceneManagerSetup.txt가 -quit 배치 실행에서는 갱신 안 됨 — 직접 확인함).
    /// 그래서 두 가지를 명시적으로 해둔다:
    /// 1) Build Settings의 씬 목록에 등록 — 어떤 씬이 "이 프로젝트의 씬"인지 명확해짐
    /// 2) playModeStartScene 지정 — 에디터에 어떤 씬이 열려있든 Play를 누르면 무조건 CombatCore가 실행된다.
    /// </summary>
    static void RegisterAsDefaultScene()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/CombatCore.unity");
        EditorSceneManager.playModeStartScene = sceneAsset;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/CombatCore.unity", true),
        };
        Debug.Log("[BuildPartAScene] playModeStartScene + Build Settings를 CombatCore.unity로 등록");
    }

    /// <summary>
    /// 발판/바닥 전용 물리 레이어. 접지 판정(OverlapCircle)이 플레이어·몬스터·투사체 콜라이더를
    /// 걸러내고 진짜 "땅"만 보게 하려고 분리했다. TagManager.asset의 layers 배열에 직접 쓴다
    /// (Project Settings 창을 열지 않고도 배치 스크립트에서 재현 가능하게).
    /// </summary>
    static void EnsureGroundLayer()
    {
        if (LayerMask.NameToLayer(GroundLayer) != -1) return;

        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets.Length == 0)
        {
            Debug.LogError("[BuildPartAScene] TagManager.asset을 못 찾음 — Ground 레이어 등록 실패");
            return;
        }
        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var layers = tagManager.FindProperty("layers");
        // 0~7은 내장 예약 레이어. 8번(첫 사용자 정의 슬롯)에 등록한다.
        layers.GetArrayElementAtIndex(8).stringValue = GroundLayer;
        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildPartAScene] Ground 레이어(8번) 등록");
    }

    static void BuildCamera(Transform playerTransform)
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        // 원본 발판이 4층(최고 y=5.40유닛)까지 있어 필드 전체 폭(26유닛)을 한 화면에 못 담는다
        // (담으면 캐릭터가 너무 작아짐) — 세로는 발판이 전부 들어오는 높이로 고정하고 가로만 스크롤한다.
        cam.orthographicSize = 5.5f;
        cam.transform.position = new Vector3(playerTransform.position.x, 3f, -10f);
        cam.backgroundColor = Color.white;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.AddComponent<AudioListener>();

        var follow = camGO.AddComponent<CameraFollow2D>();
        follow.target = playerTransform;
    }

    static void BuildGround()
    {
        // 원본처럼 mapW 전체 폭의 단일 고정 바닥. 실제 충돌은 BoxCollider2D(Ground 레이어)가 담당하고,
        // LineRenderer는 눈에 보이는 표시일 뿐이다.
        var go = new GameObject("Ground");
        go.layer = LayerMask.NameToLayer(GroundLayer);
        float y = FieldBounds.GroundY;
        float width = FieldBounds.MaxX - FieldBounds.MinX;
        float centerX = (FieldBounds.MinX + FieldBounds.MaxX) * 0.5f;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, PlatformThickness * 2f);
        go.transform.position = new Vector3(centerX, y - PlatformThickness, 0f);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(FieldBounds.MinX, y, 0));
        lr.SetPosition(1, new Vector3(FieldBounds.MaxX, y, 0));
        lr.widthMultiplier = 0.08f;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        lr.sortingOrder = -1;
    }

    /// <summary>
    /// 원본 NORMAL_PLATFORMS 15개를 실물 콜라이더로 배치한다. 원본처럼 "아래/옆에서는 그냥
    /// 통과하고 위에서 떨어질 때만 착지되는" 원웨이 발판 — Unity 내장 PlatformEffector2D(
    /// useOneWay=true) + Collider2D.usedByEffector로 구현했다(막힌 콜라이더였던 이전 버전은
    /// 원본과 달리 점프 중 발판 밑면에 머리가 막히는 버그가 있었음 — 사용자 피드백으로 수정).
    /// 원본의 dropTimer(아래로 뛰어내리기 입력)는 이번엔 안 넣음 — "위에서 착지, 아래서 통과"
    /// 자체가 요청받은 핵심이라 그 이상은 범위 밖.
    /// </summary>
    static void BuildPlatforms()
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayer);
        var parent = new GameObject("Platforms").transform;

        for (int i = 0; i < Platforms.GetLength(0); i++)
        {
            float cx = Platforms[i, 0];
            float cy = Platforms[i, 1];
            float w = Platforms[i, 2];

            var go = new GameObject($"Platform_{i}");
            go.transform.SetParent(parent);
            go.layer = groundLayer;
            go.transform.position = new Vector3(cx, cy, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, PlatformThickness);
            col.usedByEffector = true;

            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-w / 2f, 0f, 0f));
            lr.SetPosition(1, new Vector3(w / 2f, 0f, 0f));
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.08f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0.4f, 0.32f, 0.22f, 1f); // 목조 발판 느낌의 갈색
            lr.sortingOrder = -1;
        }
    }

    static GameObject BuildPlayer()
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        // 원본 스폰 좌표 p.x=220 그대로(100px=1유닛 → 2.2)
        go.transform.position = new Vector3(2.2f, FieldBounds.GroundY + 0.5f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        sr.sprite = circleSprite;
        sr.color = new Color(0.35f, 0.55f, 1f); // 아군 = 파랑
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        go.AddComponent<CharacterMover2D>();
        var mage = go.AddComponent<MageAttack>();
        mage.boltSprite = circleSprite; // 런타임 AssetDatabase 호출(빌드에서 못 씀) 없이 미리 꽂아줌

        return go;
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
