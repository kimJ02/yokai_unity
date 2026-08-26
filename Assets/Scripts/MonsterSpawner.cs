using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Part B (feature/monster-combat) - 몬스터 스폰 알고리즘 (HANDOFF.md 2번).
///
///   매 waveInterval(3.6초)마다 웨이브 발생:
///     maxSpawnPerWave(7)마리까지 스폰 시도
///     단, (현재 생존 수 + 이번에 스폰할 수) >= maxAliveTotal(22) 면 중단
///     각 마리마다 필드 안에서 무작위 위치를 뽑고, 기존 몹과 너무 가까우면(minSpacing 미달)
///     다시 뽑는다 — 최대 maxPlacementRetries(10)회. 그래도 실패하면 이번 마리는 스킵.
///
/// 필드 경계는 FieldBounds.Min / FieldBounds.Max를 그대로 소비한다.
/// FieldBounds는 Part A(feature/player-field)가 소유하는 계약 파일이다 — 이 브랜치에는
/// 독립적으로 컴파일/검증하기 위한 임시 최소 버전만 같이 넣어뒀다(FieldBounds.cs 상단 주석 참고).
/// main 병합 시 Part A 버전으로 교체될 예정.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    public GameObject monsterPrefab;

    [Header("웨이브 설정 (HANDOFF.md 2번)")]
    public float waveInterval = 3.6f;
    public int maxSpawnPerWave = 7;
    public int maxAliveTotal = 22;

    [Header("배치 간격")]
    [Tooltip("몹 사이 최소 간격(월드 유닛). 원본 게임 값이 아니라 시작점이므로 플레이해보며 조정.")]
    public float minSpacing = 1f;
    public int maxPlacementRetries = 10;

    readonly List<Transform> aliveMonsters = new List<Transform>();
    float waveTimer;

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
            Debug.LogWarning("[MonsterSpawner] monsterPrefab이 비어 있어 스폰을 건너뜀.");
            return;
        }

        for (int i = 0; i < maxSpawnPerWave; i++)
        {
            // "현재 살아있는 몹 수 + 이번에 스폰할 수 >= 22 면 중단" — 이번에 하나 더 스폰하면
            // 상한을 넘는 시점에 멈춘다.
            if (aliveMonsters.Count >= maxAliveTotal) break;

            if (TryGetSpawnPosition(out Vector2 spawnPos))
            {
                GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
                aliveMonsters.Add(monster.transform);
            }
            // 10회 재시도 후에도 자리를 못 찾으면 이번 마리는 스킵하고 다음 마리로 넘어간다.
        }
    }

    bool TryGetSpawnPosition(out Vector2 result)
    {
        for (int attempt = 0; attempt < maxPlacementRetries; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(FieldBounds.Min.x, FieldBounds.Max.x),
                Random.Range(FieldBounds.Min.y, FieldBounds.Max.y));

            if (IsFarEnoughFromExisting(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }

    bool IsFarEnoughFromExisting(Vector2 candidate)
    {
        float minSqrDist = minSpacing * minSpacing;
        foreach (var monster in aliveMonsters)
        {
            if (monster == null) continue;
            if (((Vector2)monster.position - candidate).sqrMagnitude < minSqrDist)
                return false;
        }
        return true;
    }
}
