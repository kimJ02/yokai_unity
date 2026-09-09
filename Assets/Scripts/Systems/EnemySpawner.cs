using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Core;
using YokaiFront.Enemies;
using YokaiFront.World;

namespace YokaiFront.Systems
{
/// <summary>
/// Part B (feature/monster-combat) - 몬스터 스폰 알고리즘 (docs/sprints/01-combat-core.md 2번).
///
///   매 waveInterval(3.6초)마다 웨이브 발생:
///     maxSpawnPerWave(7)마리까지 스폰 시도
///     단, (현재 생존 수 + 이번에 스폰할 수) >= maxAliveTotal(22) 면 중단
///     각 마리마다 필드 안에서 무작위 위치를 뽑고, "이번 웨이브에서 이미 배치한" 몹과 너무
///     가까우면(minSpacing 미달) 다시 뽑는다 — 최대 maxPlacementRetries(10)회.
///     그래도 실패하면 이번 마리는 스킵. **간격 체크는 이번 웨이브 안에서만** 한다(원본
///     spawnWave()의 `const placed=[]`가 호출마다 새로 시작 — 이전 웨이브에서 이미 살아있는
///     몹과는 겹쳐도 됨. 전체 생존 몹과 비교하면 필드가 찰수록 원본보다 스폰 실패가 잦아진다).
///
/// 스폰 지점은 원본 buildSpawnPoints()를 그대로 반영한다 — 필드 전체에서 균등 랜덤이 아니라
/// "발판 15개의 중심 X + 바닥 위 380px 간격 지점(6곳)" 총 21개의 정해진 스폰 포인트 중 하나를
/// 골라 그 지점 폭(w) 안에서 살짝 흔든다(spawnWave, rand(-pt.w/2+26, pt.w/2-26)).
///
/// **발판 스폰 포인트는 실제로 그 발판 높이에 스폰한다**(Y를 항상 GroundY로 고정했던 이전
/// 버전은 "발판 위에도 나온다"는 말과 실제 동작이 달랐음 — 사용자가 직접 확인하고 지적).
/// 몹은 이제 Rigidbody2D로 실제 중력을 받으므로(EnemyMove 참고) 발판 위에 스폰하면 물리로
/// 그 위에 서 있는다. FieldLayout이 발판/바닥그리드 좌표의 단일 출처다.
///
/// 스프린트 2(성장곡선 검증, `docs/sprints/03-growth-curve-worksplit.md` 트랙 A) — 스폰 직후 "가상 지역
/// 레벨"(<see cref="RunProgress"/>)에 따라 몹 체력/공격력을 스케일링하고, 그 몹이 죽으면
/// 처치 보상(골드+EXP)을 지급한다. 둘 다 이 스포너가 몹 프리팹을 다루는 유일한 지점이라
/// 자연스럽게 여기서 담당한다(트랙 A/B 경계 — `Core/PlayerProfile.cs`·`Characters/` 전체는
/// 트랙 B 소유라 손대지 않는다). **스케일링/보상 수치는 전부 `Core/DifficultyScalingConfig`에
/// 모아뒀다** — 이 파일엔 매직넘버를 두지 않는다(2026-09-08 "0. 착수 전 필독" 추가 — 원본
/// 밸런스가 완전히 정리된 게 아니라 나중에 직접 조정할 가능성이 높아서, 그때 로직 코드를
/// 뒤지지 않고 숫자만 한 파일에서 바꾸게 하려는 목적).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Header("웨이브 설정 (docs/sprints/01-combat-core.md 2번)")]
    public float waveInterval = 3.6f;
    public int maxSpawnPerWave = 7;
    public int maxAliveTotal = 22;

    [Header("배치 간격")]
    [Tooltip("같은 웨이브 안에서 몹 사이 최소 간격(월드 유닛). 원본 spawnWave()의 52px(X축 전용) ÷100.")]
    public float minSpacing = 0.52f;
    public int maxPlacementRetries = 10;

    [Header("몹 종류별 수치 (Assets/Data/Enemies/, BuildEnemyData가 생성)")]
    [Tooltip("스폰할 몹 종류들. 비어 있으면 프리팹에 들어 있는 값을 그대로 쓴다. 지역별 해금표(원본 rollSpawnType :3933)는 몹 6종을 붙일 때 여기에 얹는다.")]
    public EnemyData[] enemyTypes;

    readonly List<Transform> aliveMonsters = new List<Transform>();
    // 스폰된 몹이 어떤 종류였는지 — 처치 보상에서 종류별 exp/gold를 읽어야 해서 들고 있는다.
    readonly Dictionary<GameObject, EnemyData> spawnedData = new Dictionary<GameObject, EnemyData>();
    float waveTimer;
    float cachedMonsterRadius = -1f; // Awake 시점엔 monsterPrefab이 아직 할당 전이라(씬 빌드 순서상)
                                      // 필요할 때 지연 계산한다(GetMonsterRadius 참고).

    // 몹이 몹을 낳는 요청(분열귀 → 새끼)을 받는다. 정적 이벤트라 **반드시 OnDisable에서 해제**해야
    // 파괴된 스포너를 계속 참조하지 않는다(CLAUDE.md "낮은 층이 높은 층의 기능을 요청" 항목).
    void OnEnable() => EnemySpawnRequestBus.Requested += HandleSpawnRequest;
    void OnDisable() => EnemySpawnRequestBus.Requested -= HandleSpawnRequest;

    /// <summary>
    /// 원본 `spawnEnemyAt(x, y, 'splitlet', { noElite: true, protect: 0.35 })`(project_test.html:1842).
    /// 웨이브 상한과 무관하게 즉시 스폰한다 — 원본도 죽은 자리에서 바로 낳는다.
    /// </summary>
    void HandleSpawnRequest(Vector2 position, EnemyType enemyType)
    {
        if (monsterPrefab == null) return;

        var data = FindData(enemyType);
        var monster = Instantiate(monsterPrefab, position, Quaternion.identity);
        ApplyEnemyData(monster, data);

        // 원본 `protect: 0.35` — 새끼는 일반 스폰(2초)보다 훨씬 짧은 보호만 받는다.
        var move = monster.GetComponent<EnemyMove>();
        if (move != null) move.SetSpawnProtection(0.35f);

        aliveMonsters.Add(monster.transform);
    }

    EnemyData FindData(EnemyType enemyType)
    {
        if (enemyTypes == null) return null;
        foreach (var d in enemyTypes)
            if (d != null && d.type == enemyType) return d;
        return null;
    }

    void Update()
    {
        aliveMonsters.RemoveAll(t => t == null);

        waveTimer -= Time.deltaTime;
        if (waveTimer <= 0f)
        {
            waveTimer = waveInterval;
            SpawnWave();
        }
    }

    void SpawnWave()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] monsterPrefab이 비어 있어 스폰을 건너뜀.");
            return;
        }

        var placedThisWave = new List<Vector2>(); // 원본 spawnWave()의 `const placed=[]`와 동일 — 이 웨이브 안에서만 겹침 체크

        for (int i = 0; i < maxSpawnPerWave; i++)
        {
            // "현재 살아있는 몹 수 + 이번에 스폰할 수 >= 22 면 중단" — 이번에 하나 더 스폰하면
            // 상한을 넘는 시점에 멈춘다.
            if (aliveMonsters.Count + placedThisWave.Count >= maxAliveTotal) break;

            if (TryGetSpawnPosition(placedThisWave, out Vector2 spawnPos))
            {
                GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
                ApplyEnemyData(monster, PickEnemyData());
                aliveMonsters.Add(monster.transform);
                placedThisWave.Add(spawnPos);
            }
            // 10회 재시도 후에도 자리를 못 찾으면 이번 마리는 스킵하고 다음 마리로 넘어간다.
        }
    }

    bool TryGetSpawnPosition(List<Vector2> placedThisWave, out Vector2 result)
    {
        for (int attempt = 0; attempt < maxPlacementRetries; attempt++)
        {
            Vector2 candidate = PickSpawnPoint();
            candidate.x = FieldBounds.ClampX(candidate.x);

            if (IsFarEnoughFromPlaced(candidate, placedThisWave))
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// 원본 spawnWave()의 `spawnPoints[randInt(...)]` + `rand(-pt.w/2+26, pt.w/2-26)`를 그대로 옮김.
    /// 발판 중심(15개)과 바닥 그리드(6개) 중 하나를 균등 랜덤으로 고른 뒤 그 지점 폭 안에서 흔든다.
    /// 발판 포인트는 **그 발판의 실제 착지 Y**(발판 윗면 + 몹 반지름)를 쓴다 — 몹이 물리로
    /// 그 위에 서 있게 된다. 바닥그리드 포인트는 FieldBounds.GroundY.
    /// </summary>
    Vector2 PickSpawnPoint()
    {
        int platformCount = FieldLayout.Platforms.GetLength(0);
        int groundCount = FieldLayout.GroundGridX.Length;
        int idx = Random.Range(0, platformCount + groundCount);

        float centerX, centerY, width;
        float radius = GetMonsterRadius();
        if (idx < platformCount)
        {
            centerX = FieldLayout.Platforms[idx, 0];
            centerY = FieldLayout.PlatformLandingY(idx, radius);
            width = FieldLayout.Platforms[idx, 2];
        }
        else
        {
            centerX = FieldLayout.GroundGridX[idx - platformCount];
            centerY = FieldBounds.GroundY + radius;
            width = FieldLayout.GroundGridPointWidth;
        }

        const float margin = 0.26f; // 원본 26px 여백 ÷100
        float halfSpan = Mathf.Max(0f, width / 2f - margin);
        return new Vector2(centerX + Random.Range(-halfSpan, halfSpan), centerY);
    }

    float GetMonsterRadius()
    {
        if (cachedMonsterRadius < 0f)
        {
            var col = monsterPrefab != null ? monsterPrefab.GetComponent<CircleCollider2D>() : null;
            cachedMonsterRadius = col != null ? col.radius : 0.5f;
        }
        return cachedMonsterRadius;
    }

    bool IsFarEnoughFromPlaced(Vector2 candidate, List<Vector2> placedThisWave)
    {
        // 원본: `Math.abs(q.x-x)<52 && Math.abs(q.y-y)<12` — 둘 다 만족해야(AND) "너무 가깝다".
        // 발판 층마다 Y가 다르므로(이전엔 전부 GroundY라 Y조건이 항상 참이었음) 이제 Y차이가
        // 크면(다른 층) 겹쳐도 통과시킨다 — 원본과 동일.
        const float yThreshold = 0.12f; // 원본 12px ÷100
        foreach (var p in placedThisWave)
        {
            if (Mathf.Abs(p.x - candidate.x) < minSpacing && Mathf.Abs(p.y - candidate.y) < yThreshold)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 이번에 스폰할 몹 종류를 고른다. **지금은 등록된 종류 중 균등 랜덤**이고, 원본의 지역별 해금표
    /// (`rollSpawnType(region)` project_test.html:3933 — 지역이 오를수록 강한 종이 확률표에 추가된다)는
    /// 몹 6종을 실제로 붙일 때 여기에 얹는다(`HANDOFF.md` 6번).
    /// </summary>
    EnemyData PickEnemyData()
    {
        if (enemyTypes == null || enemyTypes.Length == 0) return null;
        return enemyTypes[Random.Range(0, enemyTypes.Length)];
    }

    /// <summary>
    /// 스폰 직후 한 번: **종류별 기본 스탯(`EnemyData`) × 지역 배율(`DifficultyScalingConfig`)** 을 적용하고
    /// 처치 보상을 연결한다. 원본도 같은 두 층 구조다 — `CONFIG.enemyBase[type]`에 지역 배율을 곱한다(`:3958`).
    ///
    /// `EnemyData`가 안 꽂혀 있으면 프리팹에 들어 있는 값을 그대로 쓴다(스폰 자체는 계속 되게).
    /// </summary>
    void ApplyEnemyData(GameObject monster, EnemyData data)
    {
        int regionLv = RunProgress.RegionLv;

        var health = monster.GetComponent<EnemyHealth>();
        if (health != null)
        {
            float baseHp = data != null ? data.maxHp : health.MaxHp;
            // 필드만 바꾸면 이미 실행된 Awake가 세팅한 CurrentHp엔 반영 안 되는 이 프로젝트 단골
            // 함정이 있어(EnemyHealth 참고) 반드시 SetMaxHp()를 통해서 바꾼다.
            health.SetMaxHp(DifficultyScalingConfig.ScaledHp(baseHp, regionLv));
            if (data != null) health.knockbackMultiplier = data.knockbackMultiplier;
            health.Died += HandleEnemyDied;
        }

        var move = monster.GetComponent<EnemyMove>();
        if (move != null)
        {
            float baseDmg = data != null ? data.attackPower : move.attackPower;
            move.attackPower = DifficultyScalingConfig.ScaledDmg(baseDmg, regionLv);
            if (data != null) move.moveSpeed = data.moveSpeed;
        }

        if (data != null)
        {
            // 몸집·색은 지역 배율과 무관한 종류 고유값이다.
            var col = monster.GetComponent<CircleCollider2D>();
            if (col != null) col.radius = data.colliderRadius;
            var sr = monster.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = data.color;

            spawnedData[monster] = data; // 처치 보상에서 종류별 exp/gold를 읽으려고 기억해둔다
        }
    }

    /// <summary>
    /// 처치 보상(원본 killEnemy(), project_test.html:1793) — EXP는 항상 지급, 골드는 확률 드랍.
    /// 종류별 기본 보상은 <see cref="EnemyData"/>, 지역 배율은 <see cref="DifficultyScalingConfig"/>.
    /// 엘리트 배수·연쇄처치·살기(fury) 보너스는 아직 범위 밖 — "몹 1마리 = 고정 공식" 루프만 구현한다.
    /// </summary>
    void HandleEnemyDied(EnemyHealth enemy)
    {
        RunProgress.RegisterKill();

        EnemyData data = null;
        if (enemy != null) spawnedData.TryGetValue(enemy.gameObject, out data);

        float sc = DifficultyScalingConfig.RewardMultiplier(RunProgress.RegionLv);
        float baseExp = data != null ? data.exp : 8f;
        int goldMin = data != null ? data.goldMin : 5;
        int goldMax = data != null ? data.goldMax : 10;

        ProfileService.Current.AddExp(Mathf.RoundToInt(baseExp * sc));

        if (Random.value < DifficultyScalingConfig.GoldDropChance)
        {
            int rolled = Random.Range(goldMin, goldMax + 1); // Random.Range(int)는 상한이 배타적이라 +1
            ProfileService.Current.AddGold(Mathf.RoundToInt(rolled * sc));
        }

        if (enemy != null) spawnedData.Remove(enemy.gameObject);
    }
}
}
