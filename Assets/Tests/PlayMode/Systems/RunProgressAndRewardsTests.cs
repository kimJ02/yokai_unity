using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Core;
using YokaiFront.Enemies;
using YokaiFront.Systems;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 스프린트 2(성장곡선 검증) 트랙 A — 가상 지역 레벨(<see cref="RunProgress"/>)과
/// 처치 보상(<see cref="EnemySpawner"/>.HandleEnemyDied)이 실제로 맞물려 도는지 검증한다.
/// 공식 자체는 `docs/sprints/03-growth-curve-worksplit.md`(원본 CONFIG.scale, project_test.html:727) 그대로.
///
/// `RunProgress`/`ProfileService.Current`는 둘 다 정적 상태라 테스트끼리 서로 오염시키므로
/// 매 테스트 시작 전 반드시 초기화한다.
/// </summary>
public class RunProgressAndRewardsTests
{
    [SetUp]
    public void ResetStatics()
    {
        RunProgress.Reset();
        ProfileService.Current = new PlayerProfile();
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.name.StartsWith("Test")) Object.DestroyImmediate(go);
        }
    }

    /// <summary>스폰용 프리팹 템플릿 — EnemyHealth/EnemyMove가 실제로 붙어 있어야
    /// EnemySpawner.ApplyRegionScaling이 컴포넌트를 찾아 스탯을 바꿀 수 있다.</summary>
    static GameObject NewMonsterPrefab()
    {
        var go = new GameObject("TestMonsterPrefab");
        go.tag = "Enemy";
        go.AddComponent<SpriteRenderer>();          // EnemyHealth가 RequireComponent로 요구
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        go.AddComponent<EnemyMove>();                // Rigidbody2D도 같이 붙음
        go.AddComponent<EnemyHealth>();
        return go;
    }

    static EnemySpawner NewSpawner(GameObject prefab)
    {
        var spawnerGO = new GameObject("TestSpawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        spawner.monsterPrefab = prefab;
        // waveTimer 기본값이 0이라 그냥 두면 Update()가 첫 프레임에 자동으로 SpawnWave()를 한 번 더
        // 불러버려서(원본 의도상 정상 동작 — 스포너가 활성화되자마자 첫 웨이브가 바로 터짐) 테스트가
        // 기대하는 "정확히 N마리"가 깨진다(실제로 겪음: 1마리만 스폰했는데 2마리로 나옴). 컴포넌트를
        // 통째로 꺼버리면 aliveMonsters.RemoveAll(null) 같은 나머지 Update 로직까지 같이 죽어서
        // 40마리 연속 처치 테스트가 깨지므로, waveTimer만 충분히 크게 밀어 자동 웨이브만 막는다.
        var timerField = typeof(EnemySpawner).GetField("waveTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        timerField.SetValue(spawner, 9999f);
        return spawner;
    }

    static void InvokeSpawnWave(EnemySpawner spawner)
    {
        var method = typeof(EnemySpawner).GetMethod("SpawnWave", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "EnemySpawner.SpawnWave 리플렉션 실패 — 시그니처가 바뀌었는지 확인");
        method.Invoke(spawner, null);
    }

    static List<Transform> GetAlive(EnemySpawner spawner)
    {
        var field = typeof(EnemySpawner).GetField("aliveMonsters", BindingFlags.NonPublic | BindingFlags.Instance);
        return (List<Transform>)field.GetValue(spawner);
    }

    /// <summary>스폰 직후 무적을 꺼야 TakeDamage가 실제로 먹힌다(EnemyHealthTests와 동일한 함정).</summary>
    static void RemoveSpawnProtection(GameObject enemy)
    {
        var move = enemy.GetComponent<EnemyMove>();
        var timerField = typeof(EnemyMove).GetField("spawnProtectTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        timerField.SetValue(move, 0f);
        enemy.GetComponent<Rigidbody2D>().gravityScale = 0f; // 테스트끼리 물리 간섭 방지(이 프로젝트 단골 함정)
    }

    /// <summary>원본 regionKillTarget(project_test.html:699, `DifficultyScalingConfig.KillsPerRegionLevel`)
    /// 마리마다 지역 레벨이 1 오른다. 상수를 직접 참조해서 나중에 Config 값이 바뀌어도 테스트가 안 깨지게 한다.</summary>
    [Test]
    public void RegionLv_IncrementsEveryKillsPerRegionLevel()
    {
        int perLevel = DifficultyScalingConfig.KillsPerRegionLevel;
        Assert.AreEqual(1, RunProgress.RegionLv, "초기 지역 레벨은 1이어야 한다");

        for (int i = 0; i < perLevel - 1; i++) RunProgress.RegisterKill();
        Assert.AreEqual(1, RunProgress.RegionLv, $"{perLevel - 1}마리째엔 아직 1지역이어야 한다");

        RunProgress.RegisterKill(); // perLevel 마리째
        Assert.AreEqual(2, RunProgress.RegionLv, $"{perLevel}마리째에 2지역으로 올라가야 한다");

        int more = perLevel * 3 / 2; // 총 perLevel × 2.5마리가 되도록
        for (int i = 0; i < more; i++) RunProgress.RegisterKill();
        Assert.AreEqual(3, RunProgress.RegionLv, "perLevel×2.5마리째엔 3지역이어야 한다");
    }

    /// <summary>
    /// 스폰 시점의 RegionLv에 따라 몹 체력/공격력이 원본 CONFIG.scale 공식대로 스케일링되는지 확인한다.
    /// hp = 38 × 2.15^(RegionLv-1), dmg = 13 × 1.4^(RegionLv-1).
    /// </summary>
    [UnityTest]
    public IEnumerator SpawnWave_ScalesEnemyStatsByRegionLevel()
    {
        for (int i = 0; i < DifficultyScalingConfig.KillsPerRegionLevel * 2; i++) RunProgress.RegisterKill(); // RegionLv = 3
        Assert.AreEqual(3, RunProgress.RegionLv);

        var prefab = NewMonsterPrefab();
        var spawner = NewSpawner(prefab);
        spawner.maxSpawnPerWave = 1;
        spawner.maxAliveTotal = 10;

        InvokeSpawnWave(spawner);
        yield return null;

        var alive = GetAlive(spawner);
        Assert.AreEqual(1, alive.Count, "1마리만 스폰하도록 했는데 개수가 다르다");

        var health = alive[0].GetComponent<EnemyHealth>();
        var move = alive[0].GetComponent<EnemyMove>();
        float expectedHp = DifficultyScalingConfig.ScaledHp(3);
        float expectedDmg = DifficultyScalingConfig.ScaledDmg(3);
        Assert.AreEqual(expectedHp, health.MaxHp, 0.01f, "3지역 체력 스케일링(DifficultyScalingConfig.ScaledHp(3))이 안 맞는다");
        Assert.AreEqual(expectedDmg, move.attackPower, 0.01f, "3지역 공격력 스케일링(DifficultyScalingConfig.ScaledDmg(3))이 안 맞는다");
        // SetMaxHp()를 거쳤다면 CurrentHp도 같이 갱신돼야 한다(안 그러면 이 프로젝트 단골 함정 재발).
        Assert.AreEqual(expectedHp, health.CurrentHp, 0.01f, "SetMaxHp() 대신 필드 직접 대입을 써서 CurrentHp가 안 갱신됐다");
    }

    /// <summary>몹 1마리를 죽였을 때 EXP는 항상 지급되고(1지역, sc=1 → 8 그대로) 처치 수도 오르는지 확인한다.</summary>
    [UnityTest]
    public IEnumerator EnemyDeath_AlwaysGrantsExp_AndRegistersKill()
    {
        var attacker = new GameObject("TestAttacker");
        var prefab = NewMonsterPrefab();
        var spawner = NewSpawner(prefab);
        spawner.maxSpawnPerWave = 1;
        spawner.maxAliveTotal = 10;

        InvokeSpawnWave(spawner);
        yield return null;

        var enemyGO = GetAlive(spawner)[0].gameObject;
        RemoveSpawnProtection(enemyGO);
        var health = enemyGO.GetComponent<EnemyHealth>();

        health.TakeDamage(health.MaxHp + 1f, attacker); // 확실히 즉사시킬 만큼
        yield return null;

        Assert.AreEqual(1, RunProgress.TotalKills, "죽였는데 누적 처치 수가 안 올랐다");
        int expectedExp = Mathf.RoundToInt(DifficultyScalingConfig.OniBaseExp * DifficultyScalingConfig.RewardMultiplier(1));
        Assert.AreEqual(expectedExp, ProfileService.Current.exp, "1지역(sc=1) 오니 처치 EXP는 DifficultyScalingConfig.OniBaseExp 그대로여야 한다");
    }

    /// <summary>
    /// 골드는 75% 확률 드랍이라 한 번으로는 검증할 수 없다 — 여러 마리를 죽여서 통계적으로 확인한다.
    /// 40마리 연속 무드랍 확률은 0.25^40 ≈ 0에 수렴하므로 골드가 0으로 남으면 확률 로직 자체가 깨진 것.
    /// 이 구간(1~40마리)에선 RegionLv가 계속 1로 고정돼(100마리 미만) EXP는 정확히 8×40으로 결정적이다.
    /// </summary>
    [UnityTest]
    public IEnumerator EnemyDeath_DropsGoldMostOfTheTime_OverManyKills()
    {
        var attacker = new GameObject("TestAttacker");
        var prefab = NewMonsterPrefab();
        var spawner = NewSpawner(prefab);
        spawner.maxSpawnPerWave = 1;
        spawner.maxAliveTotal = 1; // 매번 한 마리씩만 살아있게(죽이고 바로 다음 마리 스폰)

        const int kills = 40;
        for (int i = 0; i < kills; i++)
        {
            InvokeSpawnWave(spawner);
            yield return null;

            var enemyGO = GetAlive(spawner)[GetAlive(spawner).Count - 1].gameObject;
            RemoveSpawnProtection(enemyGO);
            var health = enemyGO.GetComponent<EnemyHealth>();
            health.TakeDamage(health.MaxHp + 1f, attacker);
            yield return null;
        }

        Assert.AreEqual(kills, RunProgress.TotalKills);
        int expectedExp = Mathf.RoundToInt(DifficultyScalingConfig.OniBaseExp * DifficultyScalingConfig.RewardMultiplier(1)) * kills;
        Assert.AreEqual(expectedExp, ProfileService.Current.exp, $"40마리 전부 1지역(sc=1)이라 EXP는 결정적이어야 한다(OniBaseExp×{kills})");
        Assert.Greater(ProfileService.Current.gold, 0, $"{kills}마리 죽였는데 골드가 한 번도 안 드랍됐다(DifficultyScalingConfig.GoldDropChance 확률상 사실상 불가능) — 확률 로직 확인 필요");
    }
}
}
