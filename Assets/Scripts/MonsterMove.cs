using UnityEngine;

/// <summary>
/// Part B (feature/monster-combat) - 몬스터 이동 AI.
/// HANDOFF.md 2번: "몹은 가장 가까운 아군 쪽으로 단순 직선 이동 + 접촉 시 공격."
///
/// 주의: 플레이어 Health(체력) 시스템은 이번 스프린트 범위 밖이다(HANDOFF.md "범위 밖" 참고).
/// 따라서 접촉 시 실제 데미지 적용은 아직 연결하지 않았다 — TryAttack()에 자리만 만들어뒀다.
/// PROGRESS.md "확인 필요" 항목 참고.
/// </summary>
[DisallowMultipleComponent]
public class MonsterMove : MonoBehaviour
{
    [Header("스탯 (HANDOFF.md 2번 - 오니 기준 시작값, 원본 76px/s를 100px=1유닛로 축척)")]
    public float moveSpeed = 0.76f;
    public float attackPower = 13f;

    [Header("접촉 판정")]
    [Tooltip("이 거리 안에 들어오면 '접촉'으로 보고 이동을 멈춘다.")]
    public float contactDistance = 0.5f;

    [Tooltip("접촉 상태에서 공격을 재시도하는 간격(초). 실제 데미지 로직은 아직 없음.")]
    public float attackInterval = 1f;

    Transform target;
    float attackTimer;

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            target = FindNearestPlayer();
            if (target == null) return;
        }

        // 필드는 횡스크롤이라 몹도 GroundY에서 X로만 오간다(PROGRESS.md 인터페이스 계약) —
        // Y까지 플레이어를 쫓아가면 플레이어가 발판 위에 있을 때 몹이 공중으로 떠오른다.
        float toTargetX = target.position.x - transform.position.x;
        float distance = Mathf.Abs(toTargetX);

        if (distance > contactDistance)
        {
            float dirX = Mathf.Sign(toTargetX);
            transform.position += new Vector3(dirX * moveSpeed * Time.deltaTime, 0f, 0f);
        }
        else
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                TryAttack(target);
            }
        }
    }

    Transform FindNearestPlayer()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float bestSqrDist = float.MaxValue;

        foreach (var player in players)
        {
            float sqrDist = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = player.transform;
            }
        }

        return nearest;
    }

    void TryAttack(Transform playerTransform)
    {
        // TODO(확인 필요): 플레이어 Health 컴포넌트가 생기면 여기서 attackPower만큼 데미지를 준다.
        // 이번 스프린트는 Health 개념 자체가 범위 밖이라 의도적으로 비워둠 (PROGRESS.md 참고).
    }
}
