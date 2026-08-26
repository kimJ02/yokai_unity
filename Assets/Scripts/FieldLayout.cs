using UnityEngine;

/// <summary>
/// 필드의 정적 공간 데이터(발판 배치, 원본 스폰 포인트) — 씬 조립(BuildPartAScene)과
/// 몬스터 스폰(MonsterSpawner)이 공유하는 단일 출처. 예전엔 이 발판 좌표가
/// BuildPartAScene.cs 안에만 있어서 스폰 포인트 계산 때 값을 복붙해야 했는데,
/// 그러면 나중에 발판 배치가 또 바뀔 때(오늘 층간 간격을 넓힌 것처럼) 두 군데를
/// 따로 고쳐야 하는 위험이 있어 여기 하나로 합쳤다.
/// </summary>
public static class FieldLayout
{
    // 원본(project_test.html) NORMAL_PLATFORMS의 X 배치(centerX/width)는 그대로 옮김. pl.x는
    // 원본에서 "왼쪽 끝" 좌표였음이 충돌판정 코드(`p.x > pl.x - 6 && p.x < pl.x + pl.w + 6`)로
    // 확인됨 — 중심이 아니다. 100px=1유닛, groundY=620 기준 centerX=(x+w/2)/100 로 환산.
    //
    // Y(층 간격)는 원본 그대로(y=505/395/285/185, 층간 1.0~1.1유닛)가 아니라 사용자 요청으로
    // 층간 1.35유닛으로 넓혔다 — 의도적 편차(원본으로 되돌리지 말 것, HANDOFF.md 1번 참고).
    public static readonly float[,] Platforms =
    {
        // centerX, centerY, width  (전부 유닛)
        {3.20f, 1.35f, 2.80f}, {9.00f, 1.35f, 3.20f}, {15.40f, 1.35f, 3.00f}, {21.50f, 1.35f, 3.20f}, // 1층
        {5.90f, 2.70f, 3.00f}, {12.30f, 2.70f, 3.20f}, {18.60f, 2.70f, 3.00f}, {24.00f, 2.70f, 2.60f}, // 2층
        {3.30f, 4.05f, 2.60f}, {9.60f, 4.05f, 3.00f}, {16.20f, 4.05f, 3.00f}, {22.00f, 4.05f, 2.60f}, // 3층
        {6.80f, 5.40f, 2.80f}, {13.40f, 5.40f, 3.00f}, {19.70f, 5.40f, 2.80f}, // 4층
    };
    public const float PlatformThickness = 0.15f;

    // 원본 buildSpawnPoints()의 바닥 그리드: `for (let x=260; x<mapW-160; x+=380) {x, y:groundY, w:300}`
    // → mapW=2600 기준 x=260,640,1020,1400,1780,2160(px), 100px=1유닛로 축척.
    public static readonly float[] GroundGridX = { 2.6f, 6.4f, 10.2f, 14.0f, 17.8f, 21.6f };
    public const float GroundGridPointWidth = 3.0f; // 원본 w:300px → 3.0유닛

    /// <summary>발판 i번 위에 반지름 radius인 원형 콜라이더가 안착했을 때의 중심 Y(발판 윗면+반지름).</summary>
    public static float PlatformLandingY(int index, float radius) => Platforms[index, 1] + PlatformThickness / 2f + radius;
    public static float PlatformLeftX(int index) => Platforms[index, 0] - Platforms[index, 2] / 2f;
    public static float PlatformRightX(int index) => Platforms[index, 0] + Platforms[index, 2] / 2f;
}
