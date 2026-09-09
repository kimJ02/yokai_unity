using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 지역(스테이지) 고정 수치 — 원본 `REGION_NAMES`(project_test.html:969),
    /// `regionBaseLv`(`:972`), `CONFIG.entryFee`(`:739`), `entryFeeOf`(`:6438`) 그대로.
    ///
    /// 진행 상태(어디까지 깼는지)는 여기가 아니라 <see cref="PlayerProfile"/>에 있다 —
    /// 이건 플레이어마다 다르지 않은 **설정값**이고 그건 **세이브 데이터**다
    /// (CLAUDE.md "세이브 데이터 vs 설정 데이터").
    /// </summary>
    public static class RegionConfig
    {
        public const int Count = 9;

        public static readonly string[] Names =
        {
            "대나무 숲", "버려진 신사", "안개 골짜기", "홍등 거리", "폐허 성곽",
            "설산 고개", "화염 동굴", "저승 문턱", "백귀야행",
        };

        // 원본 CONFIG.entryFee(:739) — 지역당 지수 증가라 결국 감당이 안 되고, 그게 윤회를 강제한다.
        const float EntryFeeBase = 300f;
        const float EntryFeeGrow = 2.2f;

        public static string NameOf(int region) =>
            region >= 1 && region <= Count ? Names[region - 1] : "?";

        /// <summary>원본 `entryFeeOf(r)`(:6438) — 1지역은 무료.</summary>
        public static int EntryFee(int region) =>
            region <= 1 ? 0 : Mathf.FloorToInt(EntryFeeBase * Mathf.Pow(EntryFeeGrow, region - 2));

        /// <summary>몹 표시 레벨 = 권장 레벨. 원본 `regionBaseLv(r) = (r-1)*3 + 1`(:972).</summary>
        public static int RecommendedLevel(int region) => (region - 1) * 3 + 1;
    }
}
