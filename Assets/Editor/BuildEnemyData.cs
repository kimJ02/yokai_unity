using System.IO;
using UnityEditor;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Editor
{
    /// <summary>
    /// `Assets/Data/Enemies/`의 `EnemyData` 에셋 7종을 **원본 수치 그대로** 생성/갱신한다.
    /// 에셋을 손으로 만들지 않는 이유는 씬을 `BuildPartAScene`으로 만드는 것과 같다 — 에디터에서
    /// 손으로 찍은 값은 원본과 어긋나도 아무도 모르고, diff에도 근거가 안 남는다.
    ///
    /// 실행: 메뉴 `YokaiFront/Build Enemy Data`, 또는 배치모드
    /// `-executeMethod YokaiFront.Editor.BuildEnemyData.Build`
    ///
    /// 수치 출처는 전부 원본 `CONFIG.enemyBase`(project_test.html:707~714)와 넉백 배율(`:1678`).
    /// **값을 바꾸려면 여기를 고치고 다시 실행할 것** — 에셋을 직접 편집하면 다음 실행에 덮어써진다.
    /// </summary>
    public static class BuildEnemyData
    {
        const string DataFolder = "Assets/Data/Enemies";

        /// <summary>
        /// 원본 한 줄에 대응하는 정의. `w`/`h`는 원본 픽셀 크기(환산 근거로만 기록)이고,
        /// 실제 원형 콜라이더 반지름은 아래 <see cref="RadiusFor"/>로 환산한다.
        /// </summary>
        struct Row
        {
            public EnemyType type;
            public string display;
            public float hp, dmg, speedPx, exp;
            public int goldMin, goldMax;
            public float w, h;
            public float knockbackMul;
            public Color color;
        }

        // 원본 CONFIG.enemyBase(:707~714) 그대로. speed는 px/s(환산은 아래에서 ÷100).
        static readonly Row[] Rows =
        {
            new Row { type = EnemyType.Wisp,     display = "도깨비불", hp = 26,  dmg = 8,  speedPx = 66,  exp = 5,  goldMin = 3,  goldMax = 6,  w = 34, h = 34, knockbackMul = 1f,   color = new Color(0.56f, 0.85f, 1f) },
            new Row { type = EnemyType.Oni,      display = "오니",     hp = 38,  dmg = 13, speedPx = 76,  exp = 8,  goldMin = 5,  goldMax = 10, w = 42, h = 46, knockbackMul = 1f,   color = new Color(0.85f, 0.20f, 0.20f) },
            // 원본 :1678 — 대오니만 넉백 0.4배(덜 밀린다). 이동/AI 로직은 오니와 완전히 같다(:710 주석 참고).
            new Row { type = EnemyType.BigOni,   display = "대오니",   hp = 160, dmg = 26, speedPx = 44,  exp = 27, goldMin = 16, goldMax = 32, w = 68, h = 80, knockbackMul = 0.4f, color = new Color(0.69f, 0.75f, 1f) },
            new Row { type = EnemyType.Charger,  display = "돌진귀",   hp = 50,  dmg = 15, speedPx = 58,  exp = 11, goldMin = 6,  goldMax = 12, w = 46, h = 48, knockbackMul = 1f,   color = new Color(1f, 0.55f, 0.25f) },
            new Row { type = EnemyType.Shooter,  display = "사수귀",   hp = 30,  dmg = 10, speedPx = 55,  exp = 10, goldMin = 6,  goldMax = 12, w = 38, h = 46, knockbackMul = 1f,   color = new Color(0.78f, 0.42f, 1f) },
            new Row { type = EnemyType.Splitter, display = "분열귀",   hp = 55,  dmg = 12, speedPx = 60,  exp = 9,  goldMin = 5,  goldMax = 10, w = 48, h = 44, knockbackMul = 1f,   color = new Color(0.40f, 0.80f, 0.45f) },
            new Row { type = EnemyType.Splitlet, display = "새끼",     hp = 14,  dmg = 7,  speedPx = 112, exp = 3,  goldMin = 1,  goldMax = 3,  w = 26, h = 28, knockbackMul = 1f,   color = new Color(0.55f, 0.90f, 0.60f) },
        };

        // 오니의 현재 콜라이더(0.5)와 원본 높이(46px)를 기준으로 나머지 종류를 비례 환산한다.
        //
        // ⚠️ 원본은 사각형(w×h) 판정이고 우리는 원형이라 1:1 대응이 없다. 게다가 우리 포트는 플레이어·오니가
        // 둘 다 지름 1유닛으로, 원본(플레이어 34×56, 오니 42×46)보다 전체적으로 크게 잡혀 있다 —
        // **이건 이번 작업에서 생긴 게 아니라 원래 있던 계통 편차**라, 여기서 몰래 바꾸면 스폰 높이·충돌·
        // 기존 테스트가 한꺼번에 흔들린다. 그래서 오니를 지금 값에 고정하고 **종류 간 상대 크기만** 원본에
        // 맞춘다(대오니는 오니보다 확실히 크고, 새끼는 확실히 작게). 절대 크기 재조정은 별도 작업으로 남긴다.
        const float OniRadius = 0.5f;
        const float OniHeightPx = 46f;
        static float RadiusFor(float heightPx) => OniRadius * (heightPx / OniHeightPx);

        /// <summary>`Assets/Sprites/Prototype/` 안의 파일명 규칙.</summary>
        static string SpriteNameFor(EnemyType type) => type switch
        {
            EnemyType.Wisp => "enemy_wisp",
            EnemyType.Oni => "enemy_oni",
            EnemyType.BigOni => "enemy_bigoni",
            EnemyType.Charger => "enemy_charger",
            EnemyType.Shooter => "enemy_shooter",
            EnemyType.Splitter => "enemy_splitter",
            EnemyType.Splitlet => "enemy_splitlet",
            _ => null,
        };

        [MenuItem("YokaiFront/Build Enemy Data")]
        public static void Build()
        {
            Directory.CreateDirectory(DataFolder);
            BuildPrototypeSprites.Apply(); // 스프라이트 import 설정을 먼저 맞춘다

            foreach (var row in Rows)
            {
                string path = $"{DataFolder}/{row.type}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                bool created = asset == null;
                if (created) asset = ScriptableObject.CreateInstance<EnemyData>();

                asset.type = row.type;
                asset.displayName = row.display;
                asset.maxHp = row.hp;
                asset.attackPower = row.dmg;
                asset.moveSpeed = row.speedPx / 100f;   // 100px = 1유닛 (CLAUDE.md 월드 스케일 규칙)
                asset.exp = row.exp;
                asset.goldMin = row.goldMin;
                asset.goldMax = row.goldMax;
                asset.knockbackMultiplier = row.knockbackMul;
                asset.colliderRadius = RadiusFor(row.h);
                asset.color = row.color;
                // 프로토타입에서 구워낸 그림을 연결한다(없으면 null → 프리팹 기본 원형 유지).
                asset.sprite = BuildPrototypeSprites.Load(SpriteNameFor(row.type));
                asset.originalWidthPx = row.w;
                asset.originalHeightPx = row.h;

                if (created) AssetDatabase.CreateAsset(asset, path);
                else EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BuildEnemyData] EnemyData {Rows.Length}종 생성/갱신 완료 → {DataFolder}");
        }
    }
}
