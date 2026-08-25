using UnityEngine;

/// <summary>
/// 전투 필드의 경계. 원본(project_test.html)은 횡스크롤 플랫포머 구조라
/// X(좌우)만 자유 이동이고 Y(높이)는 고정 바닥 + 점프 시에만 바뀐다 — top-down으로
/// 자유 이동하는 구조가 아니다. Part A(플레이어 이동 clamp)와 Part B(몬스터 스폰 X좌표
/// 계산)가 공통으로 참조하는 값이라 이 형태를 유지한다 — 임의로 바꾸지 말 것.
/// (100 유니티유닛 = 원본 100px 기준으로 원본 상수를 그대로 축척했다)
/// </summary>
public static class FieldBounds
{
    public static float MinX = -8f;
    public static float MaxX = 8f;
    public static float GroundY = 0f;

    public static float ClampX(float x) => Mathf.Clamp(x, MinX, MaxX);

    public static float RandomX() => Random.Range(MinX, MaxX);
}
