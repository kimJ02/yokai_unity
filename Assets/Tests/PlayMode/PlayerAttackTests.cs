using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 실제 Play Mode에서 Physics2D를 돌려 PlayerAttack이 사거리 안의 Enemy만 파괴하는지 확인한다.
/// Edit Mode에서는 Physics2D 쿼리와 Destroy()의 지연 파괴가 제대로 동작하지 않아
/// (Update 루프가 안 돌아서 다음 프레임 정리가 일어나지 않음) 반드시 PlayMode 테스트로 검증해야 한다.
/// </summary>
public class PlayerAttackTests
{
    [UnityTest]
    public IEnumerator Attack_DestroysOnlyTaggedEnemyInRange()
    {
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.AddComponent<CircleCollider2D>().radius = 0.5f;
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var attack = playerGO.AddComponent<PlayerAttack>();

        var near = NewTagged("EnemyNear", playerGO.transform.position + new Vector3(attack.range * 0.5f, 0, 0), "Enemy");
        var far = NewTagged("EnemyFar", playerGO.transform.position + new Vector3(attack.range * 5f, 0, 0), "Enemy");
        var untagged = NewTagged("UntaggedNear", playerGO.transform.position + new Vector3(attack.range * 0.3f, 0, 0), "Untagged");

        yield return null; // 콜라이더가 물리 월드에 등록되도록 한 프레임 대기

        var attackMethod = typeof(PlayerAttack).GetMethod("Attack", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(attackMethod, "PlayerAttack.Attack 메서드를 리플렉션으로 못 찾음");
        attackMethod.Invoke(attack, null);

        yield return null; // Destroy()는 다음 프레임에 실제로 반영된다

        Assert.IsTrue(near == null, "사거리 안의 Enemy가 파괴되지 않았다");
        Assert.IsTrue(far != null, "사거리 밖의 Enemy가 파괴됐다(오탐)");
        Assert.IsTrue(untagged != null, "Enemy 태그가 아닌 오브젝트가 파괴됐다(태그 필터 실패)");

        if (far != null) Object.Destroy(far);
        if (untagged != null) Object.Destroy(untagged);
        Object.Destroy(playerGO);
        yield return null;
    }

    [Test]
    public void FieldBounds_Clamp_KeepsPointInsideBounds()
    {
        Vector2 result = FieldBounds.Clamp(new Vector2(999f, -999f));
        Assert.AreEqual(FieldBounds.Max.x, result.x, 0.001f);
        Assert.AreEqual(FieldBounds.Min.y, result.y, 0.001f);
    }

    static GameObject NewTagged(string name, Vector3 pos, string tag)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.tag = tag;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        return go;
    }
}
