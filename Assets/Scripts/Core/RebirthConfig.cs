using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 윤회(輪廻) — 원본 `doRebirth()`(project_test.html:6509)와 윤회 장벽(`:741`·`:973`~`:977`).
    ///
    /// ## 왜 필요한가
    /// 지역 입장료가 지역당 ×2.2로 지수 증가하고(`entryFeeOf`), 몹 체력도 ×2.15로 오른다.
    /// **한 생(生)의 골드 강화만으로는 반드시 벽에 부딪히게 설계돼 있고**, 그 벽을 넘는 유일한
    /// 수단이 윤회다. 그래서 이게 없으면 지역 진행이 중간에서 그냥 멈춘다.
    ///
    /// ## 윤회 장벽 (소프트 벽)
    /// 지역마다 "권장 윤회 횟수"가 있고, 그보다 **모자란 만큼** 지수로 불리해진다(`:975`~`:977`):
    /// 몹 체력 ×2.8, 몹 피해 ×1.45, **내 피해 ×0.62**. 세 개가 동시에 걸려서 두 번쯤 모자라면
    /// 사실상 통과가 불가능해진다 — "더 강해져서 오라"가 아니라 "윤회하고 오라"는 신호다.
    /// </summary>
    public static class RebirthConfig
    {
        // 원본 CONFIG.rebirthWall(project_test.html:741)
        public const float WallHpMult = 2.8f;
        public const float WallDmgMult = 1.45f;
        public const float WallPlayerDmgMult = 0.62f;

        /// <summary>
        /// 지역 하나를 정복했을 때 주는 윤회 포인트 — 원본 `rpOfRegion(r) = max(1, floor(r*r*0.6))`
        /// (`:6502`). 뒤로 갈수록 가파르다: 1·2·5·9·15·21·29·38·48.
        /// </summary>
        public static int RpOfRegion(int region) => Mathf.Max(1, Mathf.FloorToInt(region * region * 0.6f));

        /// <summary>
        /// 그 지역의 권장 윤회 횟수 — 원본 `regionReqRebirth(r) = max(0, ceil((r-2)/2))`(`:973`).
        /// 1·2지역은 0회, 3·4지역 1회, 5·6지역 2회, 7·8지역 3회, 9지역 4회.
        /// </summary>
        public static int RequiredRebirths(int region) =>
            Mathf.Max(0, Mathf.CeilToInt((region - 2) / 2f));

        /// <summary>권장보다 몇 번 모자란가. 0이면 장벽이 없다.</summary>
        public static int Gap(int region, int rebirths) =>
            Mathf.Max(0, RequiredRebirths(region) - rebirths);

        public static float WallEnemyHp(int region, int rebirths) => Mathf.Pow(WallHpMult, Gap(region, rebirths));
        public static float WallEnemyDamage(int region, int rebirths) => Mathf.Pow(WallDmgMult, Gap(region, rebirths));

        /// <summary>**내 피해가 줄어든다**(1보다 작다). 원본 `rebirthWallPlayerDmgMult`(`:977`).</summary>
        public static float WallPlayerDamage(int region, int rebirths) => Mathf.Pow(WallPlayerDmgMult, Gap(region, rebirths));
    }
}
