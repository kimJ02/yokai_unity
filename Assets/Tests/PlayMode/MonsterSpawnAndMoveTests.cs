using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Part B(몬스터 스폰/이동) 병합 시 없던 PlayMode 테스트를 채운다(PROGRESS.md "확인 필요" 참고).
/// 핵심은 필드 경계 API 전환(FieldBounds.Min/Max → RandomX/GroundY)과 이동축 수정(X만 이동)이
/// 실제로 맞물려 동작하는지 — 스폰 위치와 이동 둘 다 GroundY에서만 벗어나지 않아야 한다.
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
}
