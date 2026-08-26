using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
/// <summary>
/// 이동 컨트롤러. 실제 Physics2D(Rigidbody2D 동적 바디 + 콜라이더 충돌 해석) 기반 — 전역 중력은
/// BuildPartAScene에서 Physics2D.gravity = (0, -26)로 설정(원본 2600px/s² → 26, 100px=1유닛 축척).
///
/// X는 화살표 키로 직접 속도를 넣어 자유 이동(원본 `updatePlayerCommon`의 bow/gunner 분기가
/// `p.vx = mx * CONFIG.player.moveSpeed`로 가속 없는 즉시 속도라 그대로 이식 — 참고로 블레이드
/// 캐릭터만 별도의 관성 가속 이동(`bladeMove`)을 쓰는데 이번 스프린트 범위(마법사)엔 해당 없음).
/// Y는 중력 + 발판/바닥 콜라이더와의 실제 충돌로 정지한다. 접지 판정은 발밑에서 Ground 레이어로
/// OverlapCircle.
///
/// 원본에서 이식한 디테일(2026-08-26, 전체 재대조):
/// - **코요테 타임(0.10초)·점프 입력 버퍼(0.13초)**: 원본 `COYOTE_T`/`INPUT_BUF_T`. 발판을 살짝
///   벗어난 직후에도 점프가 되고(코요테), 착지 직전에 미리 눌러둔 점프도 착지 즉시 나간다(버퍼).
///   이전엔 "grounded인 바로 그 프레임에 keydown"만 인정해서 원본보다 훨씬 빡빡했다.
/// - **낙하 종단속도 15유닛/s**(원본 `p.vy = Math.min(p.vy, 1500)` → 1500px/s÷100). 실제 물리
///   중력을 쓰면서 높은 발판(최고 5.4유닛)에서 떨어지면 이론상 ≈16.8유닛/s까지 나서 원본의 상한을
///   넘는다 — 그대로 두면 원본보다 더 세게 떨어진다.
/// - **필드 X 경계 여백 0.24유닛**(원본 `clamp(nx, 24, mapW-24)`). 이전엔 여백 없이 [0,26]에
///   딱 붙여서 clamp했다.
///
/// 점프 키는 원본 KEYMAP엔 C와 Space 둘 다 있지만, 사용자 지시로 이번 프로토타입은
/// C만 쓴다 — 원본과의 의도적 차이. 나중에 세션이 이걸 "원본과 다르다"며 되돌리지 말 것.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CharacterMover2D : MonoBehaviour
{
    public float moveSpeed = 2.7f;
    public float jumpSpeed = 9.6f;
    public float groundCheckRadius = 0.12f;
    public LayerMask groundMask;

    [Header("원본 상수 그대로 이식")]
    public float coyoteTime = 0.10f;      // 원본 COYOTE_T
    public float jumpBufferTime = 0.13f;  // 원본 INPUT_BUF_T
    public float terminalFallSpeed = 15f; // 원본 1500px/s ÷100
    public float edgeMargin = 0.24f;      // 원본 clamp(nx, 24, mapW-24)의 24px ÷100

    /// <summary>마지막으로 이동한 좌우 방향(1 또는 -1). 조준 입력이 없을 때 MageAttack의 기본 발사 방향으로 쓰인다.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>다른 스크립트가 프레임마다 갱신하는 이동속도 배율(예: MageAttack의 차지 중 50% 감속). 기본 1.</summary>
    public float SpeedMultiplier = 1f;

    Rigidbody2D rb;
    Collider2D col;
    bool grounded;
    float coyoteTimer;
    float jumpBufferTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 1f;
        if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        coyoteTimer = grounded ? coyoteTime : coyoteTimer - Time.deltaTime;

        if (jumpBufferTimer > 0f && (grounded || coyoteTimer > 0f))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f; // 코요테를 소모해 공중에서 두 번 점프하는 걸 막는다(원본 동일)
        }
    }

    void FixedUpdate()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (h != 0f) Facing = h > 0 ? 1 : -1;

        float vx = h * moveSpeed * SpeedMultiplier;
        float minX = FieldBounds.MinX + edgeMargin;
        float maxX = FieldBounds.MaxX - edgeMargin;
        if (rb.position.x <= minX && vx < 0f) vx = 0f;
        if (rb.position.x >= maxX && vx > 0f) vx = 0f;

        float vy = Mathf.Max(rb.linearVelocity.y, -terminalFallSpeed); // 원본 종단속도 상한
        rb.linearVelocity = new Vector2(vx, vy);

        if (rb.position.x < minX || rb.position.x > maxX)
            rb.position = new Vector2(Mathf.Clamp(rb.position.x, minX, maxX), rb.position.y);

        grounded = CheckGrounded();
    }

    bool CheckGrounded()
    {
        Vector2 feet = (Vector2)transform.position + Vector2.down * col.bounds.extents.y;
        return Physics2D.OverlapCircle(feet, groundCheckRadius, groundMask);
    }
}
}
