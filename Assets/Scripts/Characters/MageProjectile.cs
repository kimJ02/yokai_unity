using System.Collections.Generic;
using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
/// <summary>
/// MageAttack이 쏘는 마법탄 하나. 원본 `projectiles` 배열의 화살 오브젝트(kind:'arrow')에 대응.
/// Enemy 태그를 가진 대상에 닿으면 맞고(v0: Health 없이 즉시 Destroy — PlayerAttack과 동일 정책),
/// 같은 대상은 두 번 안 맞으며, pierce(관통) 횟수를 다 쓰거나 life(수명)가 다하면 스스로 사라진다.
///
/// 판정 크기(2026-08-26 수정): 원본은 `rectsOverlap(w.x-20*size, w.y-15*size, 40*size, 30*size, ...)`
/// — 40×30px(=0.4×0.3유닛, 차지 0이면) 직사각형이고 `size=1+chargeK*0.9`로 차지할수록 커진다.
/// 이전 버전은 반지름 0.5(지름 1유닛) 고정 원이라 원본보다 훨씬 크고 차지에 반응하지 않았다 —
/// 실제 판정도 원본 rectangle 그대로 BoxCollider2D로 교체.
/// </summary>
public class MageProjectile : MonoBehaviour
{
    // 원본 hitRect 절반 크기(20px, 15px → 0.2, 0.15유닛). sizeMul(=1+chargeK*0.9)을 곱해서 실제 콜라이더 크기를 만든다.
    const float BaseHalfWidth = 0.2f;
    const float BaseHalfHeight = 0.15f;

    float damage;
    int pierceLeft;
    float life;
    readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    public static MageProjectile Spawn(Vector3 pos, Vector2 velocity, float damage, int pierce, float life, float sizeMul, Sprite sprite, Color color)
    {
        // 루트는 스케일 1로 고정해서 콜라이더 크기를 월드 단위 그대로 넣는다(자식의 시각 스케일과
        // 곱해지는 이중 스케일 버그를 피하려고 판정과 비주얼을 서로 다른 오브젝트로 분리).
        var go = new GameObject("MageBolt");
        go.transform.position = pos;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * (BaseHalfWidth * 2f * sizeMul);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 3;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(BaseHalfWidth * 2f * sizeMul, BaseHalfHeight * 2f * sizeMul);

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
        // Characters 도메인은 Enemies를 직접 참조하면 안 되므로(asmdef 계층 규칙) 구체 타입
        // 대신 Core의 ISpawnProtectable 인터페이스로만 상태를 묻는다.
        var protectable = other.GetComponent<ISpawnProtectable>();
        if (protectable != null && protectable.IsSpawnProtected) return;
        if (!alreadyHit.Add(other)) return;

        Destroy(other.gameObject); // v0: 체력 시스템 없음(PlayerAttack과 동일한 단순화)
        pierceLeft--;
        if (pierceLeft < 0) Destroy(gameObject);
    }
}
}
