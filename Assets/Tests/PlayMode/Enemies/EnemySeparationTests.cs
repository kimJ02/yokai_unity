using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Core;
using YokaiFront.Enemies;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 몹끼리 안 겹치기(2026-10-25 사용자 "몬스터끼리 겹치면 안 된다" — 원본은 겹친다). 몹끼리 물리 접촉은 끄고(서로 위로 타고 올라가 쌓이던 원인)
/// 같은 줄에 있는 것끼리 옆으로 떼어 놓는다(`EnemyMove.SeparateFromNeighbours`).
/// </summary>
public class EnemySeparationTests
{
    [SetUp]
    public void Setup()
    {
        GameState.Reset();
        CombatModifiers.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go != null && go.name.StartsWith("Test")) Object.DestroyImmediate(go);
    }

    static void NewGround()
    {
        var g = new GameObject("TestGround");
        g.layer = LayerMask.NameToLayer("Ground");
        g.transform.position = new Vector3(12f, FieldBounds.GroundY - 0.5f, 0f);
        g.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
    }

    static void NewPlatform(Vector2 center, float width)
    {
        var p = new GameObject("TestPlatform");
        p.layer = LayerMask.NameToLayer("Ground");
        p.transform.position = center;
        p.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.15f);
    }

    /// <summary>게임 몹처럼 Enemy 레이어에 둔 몹. 반지름 0.5, 스폰 보호 없음, 기본은 제자리.</summary>
    static EnemyMove NewEnemy(float x, float y, float speed = 0f)
    {
        var go = new GameObject("TestSepEnemy");
        go.tag = "Enemy";
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.position = new Vector3(x, y, 0f);
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        var move = go.AddComponent<EnemyMove>();
        move.moveSpeed = speed;
        move.wanderIntervalRange = new Vector2(100f, 100f); // 배회하다 방향을 안 바꾸게
        move.SetSpawnProtection(0f);
        return move;
    }

    static IEnumerator Physics(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.fixedDeltaTime) yield return new WaitForFixedUpdate();
    }

    [Test]
    public void EnemyPhysicsContact_WithOtherEnemies_IsOff()
    {
        var e = NewEnemy(5f, 0.5f);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Assert.AreNotEqual(0, e.GetComponent<Rigidbody2D>().excludeLayers.value & (1 << enemyLayer),
                           "몹끼리 물리로 부딪히면 원형끼리 밀다가 위로 타고 올라간다");
    }

    [UnityTest]
    public IEnumerator OverlappingEnemies_SpreadSideBySide_NotStacked()
    {
        NewGround();
        var a = NewEnemy(5f, FieldBounds.GroundY + 0.5f);
        var b = NewEnemy(5.1f, FieldBounds.GroundY + 0.5f);
        yield return Physics(1.2f);

        float dx = Mathf.Abs(a.WorldPosition.x - b.WorldPosition.x);
        Assert.GreaterOrEqual(dx, 0.95f, "겹친 두 마리가 옆으로 벌어져야 한다(반지름 합 1.0)");
        Assert.AreEqual(FieldBounds.GroundY + 0.5f, a.WorldPosition.y, 0.05f, "위로 올라타면 안 된다");
        Assert.AreEqual(FieldBounds.GroundY + 0.5f, b.WorldPosition.y, 0.05f, "위로 올라타면 안 된다");
    }

    [UnityTest]
    public IEnumerator WalkingIntoNeighbour_LinesUp_DoesNotPassOrClimb()
    {
        NewGround();
        var standing = NewEnemy(5f, FieldBounds.GroundY + 0.5f);
        var walker = NewEnemy(7f, FieldBounds.GroundY + 0.5f, speed: 1f);
        walker.SetDirection(-1);
        yield return Physics(3f);

        Assert.GreaterOrEqual(walker.WorldPosition.x - standing.WorldPosition.x, 0.95f, "앞 몹을 뚫고 지나가면 안 된다 — 뒤에 줄을 선다");
        Assert.AreEqual(FieldBounds.GroundY + 0.5f, walker.WorldPosition.y, 0.05f, "앞 몹 위로 타고 올라가면 안 된다");
    }

    [UnityTest]
    public IEnumerator DifferentFloors_DoNotPushEachOther()
    {
        NewGround();
        float platformTop = FieldBounds.GroundY + 2f;
        NewPlatform(new Vector2(5f, platformTop - 0.075f), 3f);
        var below = NewEnemy(5f, FieldBounds.GroundY + 0.5f);
        var above = NewEnemy(5f, platformTop + 0.5f);
        yield return Physics(0.6f);

        Assert.AreEqual(5f, below.WorldPosition.x, 0.02f, "다른 층 몹 때문에 밀리면 안 된다");
        Assert.AreEqual(5f, above.WorldPosition.x, 0.02f, "다른 층 몹 때문에 밀리면 안 된다");
    }
}

}
