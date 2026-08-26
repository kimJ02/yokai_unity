using UnityEngine;
using YokaiFront.Core;
using YokaiFront.World;

namespace YokaiFront.Enemies
{
/// <summary>
/// Part B (feature/monster-combat) - 몬스터 이동 AI. HANDOFF.md 2번 스탯 범위(몹 1종=오니 기준)를
/// 원본 updateEnemies()의 "오니·분열귀·새끼" 분기(기본 보행 AI)를 그대로 이식해서 구현한다 —
/// 이건 프로토타입이 아니라 실제 구현이므로 세부 동작까지 원본과 동일하게 맞춘다. 아래 각 동작은
/// 전부 원본 코드의 특정 줄에 대응한다(주석에 표시).
///
/// - **추적/배회**: `Math.abs(dx) < 300`(3유닛) 안이면 플레이어 쪽으로 방향만 맞추고 계속 걷는다.
///   범위 밖이면 무작위 1.5~3.5초마다 방향을 뒤집으며 배회한다(`e.wanderT`). "접촉하면 멈춘다"는
///   동작이 원본엔 없다 — 계속 걷다가 겹치면 매 프레임 접촉 판정만 별도로 일어난다.
/// - **중력/착지**: `e.vy += gravity*dt; e.y += e.vy*dt` — 플레이어와 동일한 진짜 물리(Rigidbody2D).
/// - **발판 가장자리 반전**: 발판 위(바닥이 아님)에 서 있고 가장자리 14px 안쪽에 닿으면 그 자리에서
///   멈추고 안쪽으로 방향을 뒤집는다 — **발판 위 몹은 걸어서 떨어지지 않는다**(원본 주석 그대로:
///   "발판 위 몹은 가장자리에서 되돌아간다 — 내려오지 않는다"). 이전 버전은 이 로직이 없어서
///   그냥 물리로 굴러떨어지게만 뒀는데, 원본과 다른 동작이었다.
/// - **스폰 무적**: 원본 `CONFIG.run.spawnProtect: 2.0`(`spawnInvuln`) — 스폰 후 2초간
///   모든 피해 판정 함수가 대상을 건너뛴다. `IsSpawnProtected`로 노출, 공격 스크립트가 확인한다.
/// - **접촉 데미지**: 원본은 매 프레임 `rectsOverlap` 판정 후 `damagePlayer()` 호출 — 별도
///   쿨다운 없이 겹치는 동안 계속 불린다(실제 반복 피해 방지는 플레이어 쪽 무적시간이 담당).
///   플레이어 Health가 아직 없어(HANDOFF.md 범위 밖) `TryAttack()`은 자리만 만들어둔 상태 유지.
/// - **낙하 종단속도 15유닛/s**(원본 `e.vy`엔 명시적 상한이 없지만 플레이어와 같은 `updateEnemies`
///   중력 루프를 쓰고 원본 전체가 이 상한을 공유함 — `CharacterMover2D` 참고).
/// - **필드 X 경계 여백 0.3유닛**(원본 `e.x = clamp(e.x, 30, mapW-30)`, 매 프레임). 이전엔 배회
///   중 경계 clamp가 아예 없어서 몹이 필드 밖으로 나갈 수 있었다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyMove : MonoBehaviour, ISpawnProtectable
{
    [Header("스탯 (HANDOFF.md 2번 - 오니 기준 시작값, 원본 76px/s를 100px=1유닛로 축척)")]
    public float moveSpeed = 0.76f;
    public float attackPower = 13f;

    [Header("AI (원본 오니 분기 그대로)")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 그 방향으로 계속 걷는다. 원본 300px → 3유닛.")]
    public float aggroRangeX = 3f;
    [Tooltip("범위 밖일 때 배회 방향을 바꾸는 간격(최소~최대, 초). 원본 rand(1.5, 3.5).")]
    public Vector2 wanderIntervalRange = new Vector2(1.5f, 3.5f);

    [Header("발판 가장자리 반전 (원본: 발판 위 몹은 걸어서 안 떨어짐)")]
    [Tooltip("발판 가장자리에서 이만큼 안쪽에서 멈추고 방향을 반전한다. 원본 14px → 0.14유닛.")]
    public float platformEdgeMargin = 0.14f;
    [Tooltip("발판에 '올라서 있다'고 볼 X축 근접 여유. 원본 12px → 0.12유닛.")]
    public float platformProximityMargin = 0.12f;
    [Tooltip("발판에 '올라서 있다'고 볼 Y축 오차 허용. 원본 3px → 0.03유닛.")]
    public float onPlatformYTolerance = 0.03f;

    [Tooltip("스폰 직후 무적 시간(초) — 원본 CONFIG.run.spawnProtect 그대로.")]
    public float spawnProtectDuration = 2f;

    [Header("원본 상수 그대로 이식")]
    public float terminalFallSpeed = 15f; // 원본 1500px/s ÷100(CharacterMover2D와 동일 상한)
    public float edgeMargin = 0.3f;       // 원본 clamp(e.x, 30, mapW-30)의 30px ÷100

    /// <summary>스폰 직후 무적 상태인지. 공격 스크립트는 이 몹을 파괴하기 전에 반드시 확인한다.</summary>
    public bool IsSpawnProtected => spawnProtectTimer > 0f;

    Rigidbody2D rb;
    CircleCollider2D col;
    float spawnProtectTimer;
    Transform target;
    int dir = 1; // 원본 e.dir(1 또는 -1) — 추적/배회/가장자리반전이 전부 이 값을 공유
    float wanderTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        col = GetComponent<CircleCollider2D>();
        spawnProtectTimer = spawnProtectDuration;
        wanderTimer = Random.Range(wanderIntervalRange.x, wanderIntervalRange.y);
    }

    void Update()
    {
        if (spawnProtectTimer > 0f) spawnProtectTimer -= Time.deltaTime;

        if (target == null || !target.gameObject.activeInHierarchy)
            target = FindNearestPlayer();

        UpdateDirection();
        TryTurnBackAtPlatformEdge();

        if (target != null && OverlapsTarget())
            TryAttack(target); // 원본: 쿨다운 없이 겹치는 동안 매 프레임 호출(실제 반복 피해 방지는 플레이어 무적시간 담당)
    }

    void FixedUpdate()
    {
        float vx = dir * moveSpeed;
        float minX = FieldBounds.MinX + edgeMargin;
        float maxX = FieldBounds.MaxX - edgeMargin;
        if (rb.position.x <= minX && vx < 0f) vx = 0f;
        if (rb.position.x >= maxX && vx > 0f) vx = 0f;

        float vy = Mathf.Max(rb.linearVelocity.y, -terminalFallSpeed); // 원본 종단속도 상한
        rb.linearVelocity = new Vector2(vx, vy); // Y(중력·착지)는 Physics2D에 맡김

        if (rb.position.x < minX || rb.position.x > maxX)
            rb.position = new Vector2(Mathf.Clamp(rb.position.x, minX, maxX), rb.position.y);
    }

    /// <summary>원본: `if (Math.abs(dx) < 300) dir = sign(dx) || dir; else { wander }`.</summary>
    void UpdateDirection()
    {
        if (target != null)
        {
            float dx = target.position.x - transform.position.x;
            if (Mathf.Abs(dx) < aggroRangeX)
            {
                if (dx != 0f) dir = dx > 0f ? 1 : -1; // Math.sign(0)이 falsy라 dx==0이면 dir 유지
                return;
            }
        }
        Wander();
    }

    void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            dir = -dir;
            wanderTimer = Random.Range(wanderIntervalRange.x, wanderIntervalRange.y);
        }
    }

    /// <summary>
    /// 원본: `e.vy===0 && e.y < groundY-2`(=우리 좌표계에서 "발판 위에 정지해 있음")일 때,
    /// 지금 서 있는 발판을 찾아 가장자리 14px 안쪽이면 그 자리에 고정하고 안쪽으로 dir 반전.
    /// </summary>
    void TryTurnBackAtPlatformEdge()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > 0.01f) return; // 낙하/착지 중이면 스킵(원본 e.vy===0)
        if (transform.position.y <= FieldBounds.GroundY + col.radius + 0.02f) return; // 바닥이면 발판 로직 불필요

        for (int i = 0; i < FieldLayout.Platforms.GetLength(0); i++)
        {
            float landingY = FieldLayout.PlatformLandingY(i, col.radius);
            if (Mathf.Abs(transform.position.y - landingY) >= onPlatformYTolerance) continue;

            float left = FieldLayout.PlatformLeftX(i);
            float right = FieldLayout.PlatformRightX(i);
            if (transform.position.x < left - platformProximityMargin || transform.position.x > right + platformProximityMargin)
                continue;

            float leftEdge = left + platformEdgeMargin;
            float rightEdge = right - platformEdgeMargin;
            if (transform.position.x < leftEdge)
            {
                transform.position = new Vector3(leftEdge, transform.position.y, transform.position.z);
                dir = 1;
            }
            else if (transform.position.x > rightEdge)
            {
                transform.position = new Vector3(rightEdge, transform.position.y, transform.position.z);
                dir = -1;
            }
            break;
        }
    }

    bool OverlapsTarget()
    {
        var targetCol = target.GetComponent<Collider2D>();
        float targetRadius = targetCol != null ? targetCol.bounds.extents.x : 0.5f;
        float dist = Vector2.Distance(transform.position, target.position);
        return dist < col.radius + targetRadius;
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
}
