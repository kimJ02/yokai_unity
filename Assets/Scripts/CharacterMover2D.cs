using UnityEngine;

/// <summary>
/// 최소 이동 컨트롤러. HANDOFF.md는 "이미 구현된 캐릭터 컨트롤러 재사용"을 전제했지만
/// 배치 자동화 환경에서 에셋스토어 패키지를 인증 없이 받아올 수 없어 직접 짠 대체 구현이다.
/// 나중에 실제 캐릭터 컨트롤러 에셋으로 교체할 때 이 클래스만 갈아끼우면 되도록
/// 외부에 노출하는 건 Transform 위치뿐이라 다른 스크립트와의 결합은 없다.
/// WASD/방향키로 이동, 필드 경계를 벗어나지 못한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMover2D : MonoBehaviour
{
    public float moveSpeed = 4f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector2 next = rb.position + dir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(FieldBounds.Clamp(next));
    }
}
