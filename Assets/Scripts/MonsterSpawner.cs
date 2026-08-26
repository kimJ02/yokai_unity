using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Part B (feature/monster-combat) - 몬스터 스폰 알고리즘 (HANDOFF.md 2번).
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
/// 필드는 횡스크롤 플랫포머라 X만 자유롭고 Y는 고정 바닥이다 — 몹은 발판을 오르내리지 않으므로
/// (PROGRESS.md 인터페이스 계약) Y는 항상 FieldBounds.GroundY로 고정한다.
///
/// X는 원본 buildSpawnPoints()를 그대로 반영한다 — 원본은 필드 전체에서 균등 랜덤이 아니라
/// "발판 15개의 중심 X + 바닥 위 380px 간격 지점(6곳)" 총 21개의 정해진 스폰 포인트 중 하나를
/// 골라 그 지점 폭(w) 안에서 살짝 흔든다(spawnWave, rand(-pt.w/2+26, pt.w/2-26)). 필드 전체에서
/// 균등 랜덤으로 뽑던 이전 버전은 몹이 아무 데서나 나타나는 게 원본과 확연히 달랐다(사용자 피드백).
/// FieldLayout이 발판/바닥그리드 X 좌표의 단일 출처다.
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
    [Tooltip("같은 웨이브 안에서 몹 사이 최소 간격(월드 유닛). 원본 spawnWave()의 52px(X축 전용) ÷100.")]
    public float minSpacing = 0.52f;
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

        var placedThisWave = new List<Vector2>(); // 원본 spawnWave()의 `const placed=[]`와 동일 — 이 웨이브 안에서만 겹침 체크

        for (int i = 0; i < maxSpawnPerWave; i++)
        {
            // "현재 살아있는 몹 수 + 이번에 스폰할 수 >= 22 면 중단" — 이번에 하나 더 스폰하면
            // 상한을 넘는 시점에 멈춘다.
            if (aliveMonsters.Count + placedThisWave.Count >= maxAliveTotal) break;

            if (TryGetSpawnPosition(placedThisWave, out Vector2 spawnPos))
            {
                GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
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
            float x = FieldBounds.ClampX(PickSpawnPointX());
            Vector2 candidate = new Vector2(x, FieldBounds.GroundY);

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
    /// Y는 항상 GroundY라 발판 스폰 포인트도 "그 발판이 있는 X 위치의 바닥"으로만 쓰인다 — 몹이
    /// 발판 위에 뜨지 않는다(범위 밖).
    /// </summary>
    float PickSpawnPointX()
    {
        int platformCount = FieldLayout.Platforms.GetLength(0);
        int groundCount = FieldLayout.GroundGridX.Length;
        int idx = Random.Range(0, platformCount + groundCount);

        float centerX, width;
        if (idx < platformCount)
        {
            centerX = FieldLayout.Platforms[idx, 0];
            width = FieldLayout.Platforms[idx, 2];
        }
        else
        {
            centerX = FieldLayout.GroundGridX[idx - platformCount];
            width = FieldLayout.GroundGridPointWidth;
        }

        const float margin = 0.26f; // 원본 26px 여백 ÷100
        float halfSpan = Mathf.Max(0f, width / 2f - margin);
        return centerX + Random.Range(-halfSpan, halfSpan);
    }

    bool IsFarEnoughFromPlaced(Vector2 candidate, List<Vector2> placedThisWave)
    {
        // 원본: `Math.abs(q.x - x) < 52` — X축 거리만 본다(Y는 항상 GroundY라 원본의 Y조건은 항상 참).
        foreach (var p in placedThisWave)
        {
            if (Mathf.Abs(p.x - candidate.x) < minSpacing) return false;
        }
        return true;
    }
}
