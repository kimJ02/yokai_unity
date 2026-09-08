using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 난이도 스케일링("가상 지역 레벨") + 처치 보상 수치를 전부 여기 모았다(`docs/sprint2-handoff-split.md`
    /// 트랙 A "0. 착수 전 필독", 2026-09-08 추가). 나중에 밸런스를 조정할 때 이 파일 상수만 바꾸면
    /// 되고 `EnemySpawner`/`RunProgress` 로직 코드는 안 건드려도 된다 — 원본(`project_test.html`)
    /// 밸런스 자체가 완전히 정리된 게 아니라서(예: `CONFIG.souls`/`CONFIG.scale.soulGrow`처럼 아무 데서도
    /// 안 읽는 죽은 설정값이 실제로 있었다, `:701`,`:727`) 우리도 나중에 직접 세밀하게 조정할 가능성이
    /// 높다는 판단에서 나온 구조다.
    ///
    /// 지금 값은 전부 원본 그대로(project_test.html:699,:709,:712,:727) — 임의로 바꾸지 말 것,
    /// 조정은 나중에 실제 플레이해보고 사용자가 결정한다.
    /// </summary>
    public static class DifficultyScalingConfig
    {
        public const int KillsPerRegionLevel = 100; // 원본 regionKillTarget(:699)

        public const float HpPerRegion = 2.15f;     // CONFIG.scale.hpPerRegion(:727)
        public const float DmgPerRegion = 1.4f;     // CONFIG.scale.dmgPerRegion(:727)
        public const float RewardGrow = 1.42f;      // CONFIG.scale.rewardGrow(:727)

        public const float OniBaseHp = 38f;         // CONFIG.enemyBase.oni.hp(:709)
        public const float OniBaseDmg = 13f;        // CONFIG.enemyBase.oni.dmg(:709)
        public const float OniBaseExp = 8f;         // CONFIG.enemyBase.oni.exp(:709)
        public const int OniGoldMin = 5;            // CONFIG.enemyBase.oni.gold[0](:709)
        public const int OniGoldMax = 10;           // CONFIG.enemyBase.oni.gold[1](:709) — 포함 상한
        public const float GoldDropChance = 0.75f;  // CONFIG.goldDropChance(:712)

        public static float ScaledHp(int regionLv) => OniBaseHp * Mathf.Pow(HpPerRegion, regionLv - 1);
        public static float ScaledDmg(int regionLv) => OniBaseDmg * Mathf.Pow(DmgPerRegion, regionLv - 1);
        public static float RewardMultiplier(int regionLv) => Mathf.Pow(RewardGrow, regionLv - 1);
    }
}
