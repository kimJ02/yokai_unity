using UnityEngine;

/// <summary>
/// 전투 필드의 경계. Part A(플레이어 이동 clamp)와 Part B(몬스터 스폰 위치 계산)가
/// 공통으로 참조하는 값이라 PROGRESS.md의 인터페이스 계약대로 이 형태를 유지한다 — 임의로 바꾸지 말 것.
/// </summary>
public static class FieldBounds
{
    public static Vector2 Min = new Vector2(-8f, -4.5f);
    public static Vector2 Max = new Vector2(8f, 4.5f);

    public static Vector2 Clamp(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, Min.x, Max.x),
            Mathf.Clamp(pos.y, Min.y, Max.y)
        );
    }

    public static Vector2 RandomPoint()
    {
        return new Vector2(Random.Range(Min.x, Max.x), Random.Range(Min.y, Max.y));
    }
}
