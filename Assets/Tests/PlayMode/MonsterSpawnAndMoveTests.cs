using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Part B(몬스터 스폰/이동) 관련 PlayMode 테스트. 필드 경계 API 전환 + 이동축 수정(X만 이동) +
/// 원본과 비교해 다시 이식한 것들(스폰 포인트를 균등 랜덤이 아니라 발판/바닥그리드 기반으로,
/// 스폰 직후 2초 무적)이 실제로 맞물려 동작하는지 검증한다.
/// </summary>
public class MonsterSpawnAndMoveTests
{
    /// <summary>
    /// 웨이브 하나를 강제로 발생시켜, 스폰된 몹이 전부 FieldBounds 안 + GroundY 위에 있는지 확인한다.
    /// waveTimer를 기다리지 않고 private SpawnWave()를 리플렉션으로 직접 호출해 테스트를 빠르게 한다.
    /// </summary>
    [UnityTest]
    public IEnumerator SpawnWave_PlacesMonstersWithinFieldBounds_OnGroundY()
    {
        var prefab = new GameObject("TestMonsterPrefab");
        prefab.tag = "Enemy";
        prefab.AddComponent<CircleCollider2D>().radius = 0.3f;

        var spawnerGO = new GameObject("TestSpawner");
        var spawner = spawnerGO.AddComponent<MonsterSpawner>();
        spawner.monsterPrefab = prefab;
        spawner.maxSpawnPerWave = 5;

        var spawnWave = typeof(MonsterSpawner).GetMethod("SpawnWave", BindingFlags.NonPublic | BindingFlags.Instance);
        var aliveField = typeof(MonsterSpawner).GetField("aliveMonsters", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(spawnWave);
        Assert.IsNotNull(aliveField);
        spawnWave.Invoke(spawner, null);
        yield return null;

        // 스포너가 직접 관리하는 목록으로 확인한다 — 태그 전역 검색은 프리팹 템플릿(비활성)
        // 자체와 실제 스폰된 인스턴스를 혼동하기 쉬워서(Instantiate는 활성 상태를 그대로 복사) 피한다.
        var alive = (System.Collections.IList)aliveField.GetValue(spawner);
        Assert.Greater(alive.Count, 0, "웨이브를 발생시켰는데 스폰된 몹이 없다");

        foreach (Transform t in alive)
        {
            Assert.GreaterOrEqual(t.position.x, FieldBounds.MinX - 0.01f, "필드 왼쪽 경계 밖에 스폰됨");
            Assert.LessOrEqual(t.position.x, FieldBounds.MaxX + 0.01f, "필드 오른쪽 경계 밖에 스폰됨");
            Assert.AreEqual(FieldBounds.GroundY, t.position.y, 0.001f, "GroundY가 아닌 높이에 스폰됨(발판 위 등)");
            Object.Destroy(t.gameObject);
        }

        Object.Destroy(prefab);
        Object.Destroy(spawnerGO);
        yield return null;
    }

    /// <summary>
    /// 몹이 플레이어를 쫓아갈 때 X로만 이동하고 Y(GroundY)는 그대로 유지하는지 확인한다.
    /// 플레이어를 몹보다 훨씬 높은 위치(발판 위 시뮬레이션)에 둬서, 예전 버그(X·Y 둘 다 추적)라면
    /// 몹이 위로 떠오르는 걸 바로 잡아낼 수 있게 했다.
    /// </summary>
    [UnityTest]
    public IEnumerator MonsterMove_ChasesOnXOnly_KeepsGroundY()
    {
        var playerGO = new GameObject("TestPlayer");
        playerGO.tag = "Player";
        playerGO.transform.position = new Vector3(10f, 5f, 0f); // 몹보다 훨씬 오른쪽 + 훨씬 위(발판 위 상황)

        var monsterGO = new GameObject("TestMover");
        monsterGO.transform.position = new Vector3(2f, FieldBounds.GroundY, 0f);
        var move = monsterGO.AddComponent<MonsterMove>();
        move.moveSpeed = 5f; // 테스트를 빨리 끝내려고 크게

        float startY = monsterGO.transform.position.y;
        float startX = monsterGO.transform.position.x;

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(startY, monsterGO.transform.position.y, 0.001f, "몹이 Y축으로 움직였다(발판 위 플레이어를 공중에서 쫓아감)");
        Assert.Greater(monsterGO.transform.position.x, startX, "몹이 플레이어(오른쪽) 방향으로 이동하지 않았다");

        Object.Destroy(playerGO);
        Object.Destroy(monsterGO);
        yield return null;
    }

    /// <summary>
    /// 스폰 X좌표가 필드 전체 균등 랜덤이 아니라, 원본처럼 발판 중심/바닥그리드 지점(FieldLayout)
    /// 중 하나 근처(그 지점의 폭 안)에만 찍히는지 확인한다. 여러 번 스폰해서 전부 어떤 스폰
    /// 포인트의 창 안에 들어가는지 검사 — 하나라도 창 밖이면 "아무 데서나" 스폰되는 셈이라 실패.
    /// </summary>
    [UnityTest]
    public IEnumerator SpawnWave_PicksXFromKnownSpawnPoints_NotUniformRandom()
    {
        var prefab = new GameObject("TestMonsterPrefab");
        prefab.tag = "Enemy";
        prefab.AddComponent<CircleCollider2D>().radius = 0.3f;

        var spawnerGO = new GameObject("TestSpawner");
        var spawner = spawnerGO.AddComponent<MonsterSpawner>();
        spawner.monsterPrefab = prefab;
        spawner.maxSpawnPerWave = 7;
        spawner.minSpacing = 0f; // 이 테스트는 분포만 보는 거라 간격 재시도로 흔들리지 않게

        var spawnWave = typeof(MonsterSpawner).GetMethod("SpawnWave", BindingFlags.NonPublic | BindingFlags.Instance);
        var aliveField = typeof(MonsterSpawner).GetField("aliveMonsters", BindingFlags.NonPublic | BindingFlags.Instance);

        // 여러 웨이브를 돌려서 스폰 포인트 21개 중 일부만 우연히 걸리는 걸 방지
        for (int wave = 0; wave < 5; wave++)
        {
            spawnWave.Invoke(spawner, null);
        }
        yield return null;

        var alive = (System.Collections.IList)aliveField.GetValue(spawner);
        Assert.Greater(alive.Count, 0, "스폰된 몹이 없다");

        const float margin = 0.26f;
        foreach (Transform t in alive)
        {
            bool inWindow = false;
            for (int i = 0; i < FieldLayout.Platforms.GetLength(0) && !inWindow; i++)
            {
                float cx = FieldLayout.Platforms[i, 0];
                float halfSpan = Mathf.Max(0f, FieldLayout.Platforms[i, 2] / 2f - margin) + 0.02f;
                if (Mathf.Abs(t.position.x - cx) <= halfSpan) inWindow = true;
            }
            for (int i = 0; i < FieldLayout.GroundGridX.Length && !inWindow; i++)
            {
                float halfSpan = Mathf.Max(0f, FieldLayout.GroundGridPointWidth / 2f - margin) + 0.02f;
                if (Mathf.Abs(t.position.x - FieldLayout.GroundGridX[i]) <= halfSpan) inWindow = true;
            }
            Assert.IsTrue(inWindow, $"x={t.position.x}가 어떤 스폰 포인트 창에도 안 들어감 — 균등 랜덤으로 회귀한 것 아닌지 확인");
            Object.Destroy(t.gameObject);
        }

        Object.Destroy(prefab);
        Object.Destroy(spawnerGO);
        yield return null;
    }

    /// <summary>
    /// 원본 spawnInvuln(스폰 직후 2초 무적)이 실제로 공격을 막는지 확인한다. 무적 중엔
    /// PlayerAttack이 파괴하지 못하고, 무적이 풀리면 정상적으로 파괴돼야 한다.
    /// </summary>
    [UnityTest]
    public IEnumerator SpawnProtectedMonster_SurvivesAttack_ThenDiesAfterProtectionExpires()
    {
        var playerGO = new GameObject("TestPlayer");
        playerGO.tag = "Player";
        var attack = playerGO.AddComponent<PlayerAttack>();
        var attackMethod = typeof(PlayerAttack).GetMethod("Attack", BindingFlags.NonPublic | BindingFlags.Instance);

        var enemyGO = new GameObject("TestProtectedEnemy");
        enemyGO.tag = "Enemy";
        enemyGO.transform.position = playerGO.transform.position; // 사거리 안에 확실히 들어오게
        enemyGO.AddComponent<CircleCollider2D>().radius = 0.3f;
        var move = enemyGO.AddComponent<MonsterMove>(); // AddComponent가 Awake를 동기 실행하므로
        // spawnProtectDuration 필드를 나중에 바꿔도 이미 실행된 Awake엔 반영 안 됨 — private
        // 타이머 자체를 리플렉션으로 직접 짧게 세팅한다(테스트를 빨리 끝내려고, 원본 값은 2초).
        var timerField = typeof(MonsterMove).GetField("spawnProtectTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(timerField);
        timerField.SetValue(move, 0.1f);

        attackMethod.Invoke(attack, null);
        yield return null;
        Assert.IsTrue(enemyGO != null, "스폰 직후 무적 중인데 공격에 파괴됐다");

        yield return new WaitForSeconds(0.2f); // 무적(0.1초) 만료 대기

        attackMethod.Invoke(attack, null);
        yield return null;
        Assert.IsTrue(enemyGO == null, "무적이 풀렸는데도 공격에 안 죽었다");

        Object.Destroy(playerGO);
        yield return null;
    }
}
