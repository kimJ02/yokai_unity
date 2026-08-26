using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YokaiFront.Characters;
using YokaiFront.Core;
using YokaiFront.Systems;
using YokaiFront.World;

namespace YokaiFront.Editor
{

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
    // 발판 좌표/두께는 FieldLayout.cs로 옮겨서 EnemySpawner의 스폰 포인트 계산과 공유한다
    // (두 군데 따로 들고 있으면 나중에 발판 배치가 또 바뀔 때 하나만 고치는 실수가 남).

    [MenuItem("Tools/YokaiFront/Build Part A Scene")]
    public static void Build()
    {
        EnsureCircleSprite();
        EnsureGroundLayer();
        EnsureMonsterPrefabPhysics();
        Physics2D.gravity = new Vector2(0f, -26f); // 원본 2600px/s² → 26 (100px=1유닛)

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildGround();
        BuildPlatforms();
        var player = BuildPlayer();
        BuildCamera(player.transform);
        BuildEnemySpawner();

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
        // 개정(2026-08-26): "맵이 원본보다 좁아 보인다"는 사용자 지적으로 재계산.
        // 원본 캔버스는 1280×720px 고정이고 mapW=2600px — 화면엔 항상 맵의 1280/2600≈49%만 보이고
        // 나머지는 가로 스크롤로 드러난다. 이전엔 이 비율과 무관하게 orthographicSize=5.5(발판이
        // 다 보이는 정도)로 임의로 잡아서 화면에 맵의 75%(19.6/26유닛)가 보였다 — 원본보다 훨씬
        // 넓게 보여서 "안 넓어 보인다"는 체감이 났다. 100px=1유닛 규칙을 카메라에도 그대로 적용:
        // 세로 절반 크기 = 720px/2 ÷100 = 3.6유닛. 가로는 orthographicSize×aspect로 자동 결정되는데
        // 원본 캔버스 비율(1280:720=16:9)로 맞추면 가로도 정확히 1280px÷100=12.8유닛이 되고,
        // 26/12.8 ≈ 2.03 ≈ 원본의 2600/1280 ≈ 2.03과 일치한다.
        // 세로 하단은 원본 그대로: 원본 캔버스는 groundY(620)가 화면 하단에서 100px(=1유닛) 위라
        // 바닥 아래 여백 1유닛을 그대로 가져온다(viewBottom=-1.0). 다만 세로 상단은 원본 값(6.2,
        // 발판이 y=185=4.35유닛까지였을 때 기준)을 그대로 쓰면 발판 층간 간격을 넓힌 우리 필드에선
        // 최상단 발판(5.40유닛) 위에 선 캐릭터 머리가 화면 위로 살짝 잘린다(발판 간격을 넓힌 건
        // 사용자가 명시적으로 요청한 편차라 되돌리지 않음 — HANDOFF.md 1번 참고). 그래서 상단만
        // 우리 발판 높이 기준으로 다시 계산: 최상단 발판 위 캐릭터 전체(중심+반지름) + 여유 0.2.
        cam.orthographicSize = 3.84f;
        cam.transform.position = new Vector3(playerTransform.position.x, 2.84f, -10f);
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
        col.size = new Vector2(width, FieldLayout.PlatformThickness * 2f);
        go.transform.position = new Vector3(centerX, y - FieldLayout.PlatformThickness, 0f);

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

        for (int i = 0; i < FieldLayout.Platforms.GetLength(0); i++)
        {
            float cx = FieldLayout.Platforms[i, 0];
            float cy = FieldLayout.Platforms[i, 1];
            float w = FieldLayout.Platforms[i, 2];

            var go = new GameObject($"Platform_{i}");
            go.transform.SetParent(parent);
            go.layer = groundLayer;
            go.transform.position = new Vector3(cx, cy, 0f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, FieldLayout.PlatformThickness);
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

    const string MonsterPrefabPath = "Assets/Prefabs/Enemy_Oni.prefab";

    /// <summary>
    /// Part B(HANDOFF.md 2번) 웨이브 스포너를 씬에 등록. 프리팹은 Part B가 만든
    /// Assets/Prefabs/Enemy_Oni.prefab(구 Monster.prefab)을 그대로 참조한다 — 여기서 새로 만들지 않는다.
    /// </summary>
    static void BuildEnemySpawner()
    {
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterPrefab == null)
        {
            Debug.LogWarning($"[BuildPartAScene] {MonsterPrefabPath}를 못 찾아 EnemySpawner를 건너뜀.");
            return;
        }

        var go = new GameObject("EnemySpawner");
        var spawner = go.AddComponent<EnemySpawner>();
        spawner.monsterPrefab = monsterPrefab;
    }

    /// <summary>
    /// Enemy_Oni.prefab(구 Monster.prefab)에 Rigidbody2D가 없었다 — 몹이 발판 높이에 스폰돼도 중력을
    /// 안 받아 허공에 뜬 채로 있거나(또는 발판 콜라이더를 그냥 통과)였다. 실제로 발판 위에 서 있으려면
    /// 플레이어와 동일하게 진짜 물리(Rigidbody2D + 이미 있는 CircleCollider2D)가 필요해서 프리팹에 직접 추가한다.
    ///
    /// 주의: `EnemyMove`에 `[RequireComponent(typeof(Rigidbody2D))]`를 붙여놨더니, 프리팹 파일에
    /// 실제로는 없는데도 로드 시점에 엔진이 메모리상으로만 자동 보충해서
    /// `prefab.GetComponent&lt;Rigidbody2D&gt;() != null`이 거짓으로 참이 되는 걸 직접 확인했다
    /// (그래서 "이미 있으면 건너뛴다"는 가드를 넣었다가 실제 파일엔 한 번도 저장 안 된 채로
    /// 넘어간 적이 있음). 그래서 "있는지 검사 후 건너뛰기"를 하지 않고 항상 로드 → 설정 → 저장한다
    /// — freezeRotation=true를 매번 명시적으로 세팅하는 것도 같은 이유(자동 보충된 기본값은 false).
    /// </summary>
    static void EnsureMonsterPrefabPhysics()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (prefab == null) return;

        var contents = PrefabUtility.LoadPrefabContents(MonsterPrefabPath);
        var rb = contents.GetComponent<Rigidbody2D>();
        if (rb == null) rb = contents.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 1f;
        PrefabUtility.SaveAsPrefabAsset(contents, MonsterPrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        Debug.Log("[BuildPartAScene] Enemy_Oni.prefab Rigidbody2D 확인/설정 완료");
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

}
