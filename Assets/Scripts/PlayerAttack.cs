using UnityEngine;

/// <summary>
/// 공격 버튼 1개. HANDOFF.md 3번 스펙: 반경 R 안의 Enemy 태그 오브젝트에게 즉시 피해.
/// v0에서는 Health 컴포넌트 없이 맞으면 즉시 Destroy — Part B(몬스터)와 서로 안 기다리기 위한
/// 의도적 단순화다(PROGRESS.md 인터페이스 계약 참고). Health를 나중에 도입해도
/// 이 스크립트가 바뀌는 범위는 TakeDamage 호출 한 줄뿐이도록 만들어뒀다.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    public float range = 1.5f;
    public float cooldown = 0.4f;
    public LayerMask enemyMask = ~0; // 기본값: 전체 레이어. Enemy 전용 레이어를 쓰기 전까지는 태그로 한 번 더 거른다.

    float cdTimer;

    void Update()
    {
        cdTimer -= Time.deltaTime;
        if (cdTimer > 0f) return;

        // 원본(project_test.html) KEYMAP: KeyZ = 기본 공격
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Attack();
            cdTimer = cooldown;
        }
    }

    void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);
        foreach (var col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;
            Destroy(col.gameObject);
        }
    }

    // 씬 뷰에서 판정 반경을 눈으로 확인하기 위한 디버그용 표시. 게임 화면(빌드)엔 안 나온다.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
