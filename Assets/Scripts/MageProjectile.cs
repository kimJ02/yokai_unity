using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MageAttack이 쏘는 마법탄 하나. 원본 `projectiles` 배열의 화살 오브젝트(kind:'arrow')에 대응.
/// Enemy 태그를 가진 대상에 닿으면 맞고(v0: Health 없이 즉시 Destroy — PlayerAttack과 동일 정책),
/// 같은 대상은 두 번 안 맞으며, pierce(관통) 횟수를 다 쓰거나 life(수명)가 다하면 스스로 사라진다.
/// </summary>
public class MageProjectile : MonoBehaviour
{
    float damage;
    int pierceLeft;
    float life;
    readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    public static MageProjectile Spawn(Vector3 pos, Vector2 velocity, float damage, int pierce, float life, float visualScale, Sprite sprite, Color color)
    {
        var go = new GameObject("MageBolt");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * visualScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 3;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = velocity;

        var proj = go.AddComponent<MageProjectile>();
        proj.damage = damage;
        proj.pierceLeft = pierce;
        proj.life = life;
        return proj;
    }

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        // 원본은 `if (e.spawnInvuln > 0) continue`로 스폰 직후 무적 대상을 hitSet에 아예 안 넣는다
        // — pierce도 안 깎이고, 무적이 풀린 뒤 다시 판정에 걸릴 수 있다. 그대로 이식.
        var mover = other.GetComponent<MonsterMove>();
        if (mover != null && mover.IsSpawnProtected) return;
        if (!alreadyHit.Add(other)) return;

        Destroy(other.gameObject); // v0: 체력 시스템 없음(PlayerAttack과 동일한 단순화)
        pierceLeft--;
        if (pierceLeft < 0) Destroy(gameObject);
    }
}
