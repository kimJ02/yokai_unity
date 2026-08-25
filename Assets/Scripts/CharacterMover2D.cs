using UnityEngine;

/// <summary>
/// 이동 컨트롤러. 이번 개정에서 자체 중력 계산(vy 수동 적분)을 걷어내고
/// 실제 Physics2D(Rigidbody2D 동적 바디 + 콜라이더 충돌 해석)로 교체했다 —
/// "물리엔진 구현" 요청 반영. 전역 중력은 BuildPartAScene에서
/// Physics2D.gravity = (0, -26)로 설정(원본 2600px/s² → 26, 100px=1유닛 축척).
///
/// X는 여전히 화살표 키로 직접 속도를 넣어 자유 이동(경계에서 clamp),
/// Y는 더 이상 손으로 계산하지 않고 중력 + 발판/바닥 콜라이더와의 실제 충돌로 정지한다.
/// 접지 판정은 발밑에서 Ground 레이어로 OverlapCircle — 원본처럼 여러 층 발판을 딛고
/// 설 수 있어야 해서 "고정 바닥 하나"였던 이전 가정을 버렸다.
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

    /// <summary>마지막으로 이동한 좌우 방향(1 또는 -1). 조준 입력이 없을 때 MageAttack의 기본 발사 방향으로 쓰인다.</summary>
    public int Facing { get; private set; } = 1;

    Rigidbody2D rb;
    Collider2D col;
    bool grounded;

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
        if (grounded && Input.GetKeyDown(KeyCode.C))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        }
    }

    void FixedUpdate()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (h != 0f) Facing = h > 0 ? 1 : -1;

        float vx = h * moveSpeed;
        if (rb.position.x <= FieldBounds.MinX && vx < 0f) vx = 0f;
        if (rb.position.x >= FieldBounds.MaxX && vx > 0f) vx = 0f;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        grounded = CheckGrounded();
    }

    bool CheckGrounded()
    {
        Vector2 feet = (Vector2)transform.position + Vector2.down * col.bounds.extents.y;
        return Physics2D.OverlapCircle(feet, groundCheckRadius, groundMask);
    }
}
