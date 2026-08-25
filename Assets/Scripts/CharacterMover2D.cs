using UnityEngine;

/// <summary>
/// 최소 이동 컨트롤러. HANDOFF.md는 "이미 구현된 캐릭터 컨트롤러 재사용"을 전제했지만
/// 배치 자동화 환경에서 에셋스토어 패키지를 인증 없이 받아올 수 없어 직접 짠 대체 구현이다.
///
/// 원본(project_test.html) 그대로 횡스크롤 구조를 따른다 — X는 화살표 키로 자유 이동,
/// Y는 고정 바닥(FieldBounds.GroundY) + 점프(C/Space) 시에만 중력을 받아 포물선을 그린다.
/// 상수는 원본 픽셀값을 100px = 1유닛 기준으로 축척했다(moveSpeed 270px/s → 2.7,
/// jumpVel -960px/s → 9.6, gravity 2600px/s² → 26. 원본 Y+는 화면 아래 방향이라 부호 반전).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMover2D : MonoBehaviour
{
    public float moveSpeed = 2.7f;
    public float jumpSpeed = 9.6f;
    public float gravity = 26f;

    Rigidbody2D rb;
    float vy;
    bool grounded = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // 자체 중력 계산을 쓴다 — FieldBounds.GroundY 기준 단일 바닥이라 Physics2D 중력보다 이게 더 단순하다
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        Vector2 p = rb.position;
        p.y = FieldBounds.GroundY;
        rb.position = p;
    }

    void Update()
    {
        if (grounded && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space)))
        {
            vy = jumpSpeed;
            grounded = false;
        }
    }

    void FixedUpdate()
    {
        float h = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) h += 1f;

        float nx = FieldBounds.ClampX(rb.position.x + h * moveSpeed * Time.fixedDeltaTime);

        float ny = rb.position.y;
        if (!grounded)
        {
            vy -= gravity * Time.fixedDeltaTime;
            ny += vy * Time.fixedDeltaTime;
            if (ny <= FieldBounds.GroundY)
            {
                ny = FieldBounds.GroundY;
                vy = 0f;
                grounded = true;
            }
        }

        rb.MovePosition(new Vector2(nx, ny));
    }
}
