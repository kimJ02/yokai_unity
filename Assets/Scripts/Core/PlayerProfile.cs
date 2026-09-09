using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 플레이어 진행 상태 — 원본 `meta`(project_test.html:1129 `defaultMeta()`)의 일부를 옮긴 것.
    /// **세이브 데이터다, SO가 아니다**(CLAUDE.md "세이브 데이터 vs 설정 데이터" 절 참고) — 플레이어마다
    /// 다르고 런타임에 계속 바뀌므로 순수 직렬화 가능 클래스로 둔다.
    ///
    /// 스프린트 2("성장곡선 검증", `docs/sprints/03-growth-curve-worksplit.md`)의 트랙 A/B 공유 계약 — 필드
    /// 이름·타입을 임의로 바꾸면 두 트랙이 동시에 깨진다. 트랙 A(처치 보상)는 <see cref="AddGold"/>/
    /// <see cref="AddExp"/>만 호출하고 `gold`/`exp` 필드를 직접 증가시키지 않는다.
    /// </summary>
    public class PlayerProfile
    {
        public int level = 1;
        public int exp = 0;
        public int gold = 0;

        /// <summary>
        /// 지금 고른 캐릭터. 원본 `meta.character`(project_test.html:1134, 기본값 'mage').
        /// 파생 스탯의 캐릭터 배수(<see cref="CharacterStats.StatMultiplier"/>)가 이 값을 본다.
        ///
        /// ⚠️ 원본은 레벨·경험치·SP를 **캐릭터별로** 따로 관리한다(`meta.charProgress`, `:1249`).
        /// 우리는 아직 단일 `level`/`exp`/`spUsed`를 공유한다 — 0차 단계 검증엔 지장이 없어서
        /// 미뤄둔 것이고, **단계 2(캐릭터별 5차 심화) 때 `branch`/`tier` 일반화와 같이 분리한다**
        /// (`docs/worksplit.md` 7절 확정 사항).
        /// </summary>
        public CharacterId character = CharacterId.Mage;

        /// <summary>
        /// 스킬트리(전문화)에 쓴 SP. 원본은 캐릭터별로 따로 관리한다(`charProg().spUsed`,
        /// project_test.html:1249) — 이 프로토타입엔 마법사 하나뿐이라 이 필드가 곧 마법사의 spUsed다.
        /// 캐릭터가 늘어나면 `PlayerProfile` 자체를 캐릭터별로 두거나 이 필드를 분리해야 한다.
        /// </summary>
        public int spUsed = 0;
        public MageBranch mageBranch = MageBranch.None;
        public int mageTier = 0;

        public UpgradeLevels upgrades = new UpgradeLevels();

        /// <summary>
        /// 지역별 보스 격파 여부. 원본 `meta.regions[r].bossCleared`(project_test.html:6435)로,
        /// **다음 지역을 여는 유일한 조건**이다(`regionOpen(r) = r === 1 || regions[r-1].bossCleared`).
        /// 인덱스는 0부터라 `regionBossCleared[0]`이 1지역이다.
        /// </summary>
        public bool[] regionBossCleared = new bool[RegionConfig.Count];

        /// <summary>
        /// ⚠️ **임시 — 보스를 구현하면 삭제한다.** 지역 해금 조건이 "이전 지역 보스 격파"인데
        /// 보스가 아직 없어서 정상적으로는 1지역에서 영영 못 나간다. 그러면 방금 만든 지역별
        /// 몹 해금표·난이도 스케일링을 **실제로 확인할 방법이 없어서** 열어둔 스위치다.
        /// 원본에 없는 동작이므로 보스가 생기는 즉시 이 필드와 참조처를 지울 것.
        /// </summary>
        public bool debugUnlockAllRegions = true;

        /// <summary>원본 `regionOpen(r)`(project_test.html:6434).</summary>
        public bool IsRegionOpen(int region)
        {
            if (region <= 1) return true;
            if (debugUnlockAllRegions) return true;
            int prev = region - 2; // 이전 지역의 0-based 인덱스
            return prev >= 0 && prev < regionBossCleared.Length && regionBossCleared[prev];
        }

        /// <summary>레벨이 실제로 오를 때(한 번 이상) 1회 발생. `Characters/PlayerHealth`가 구독해서
        /// 최대체력 재계산 + 풀피 회복을 한다(원본 `project_test.html:1850`, 레벨업 때만 일어나고
        /// 골드 강화 구매 자체로는 즉시 반영되지 않는다 — 원본과 동일한 비직관적 동작).</summary>
        public event System.Action LeveledUp;

        /// <summary>트랙 A(처치 보상)가 호출. 즉시 누적만 하면 됨(레벨 개념 없음).</summary>
        public void AddGold(int amount) => gold += amount;

        /// <summary>
        /// 트랙 A(처치 보상)가 호출. 원본 `gainExpMeta()`(project_test.html:1440) 그대로 —
        /// 초과분을 이월하며 한 번의 호출로 여러 레벨이 오를 수 있다.
        /// </summary>
        public void AddExp(int amount)
        {
            exp += amount;
            bool leveled = false;
            while (exp >= RequiredExp(level))
            {
                exp -= RequiredExp(level);
                level++;
                leveled = true;
            }
            if (leveled) LeveledUp?.Invoke();
        }

        /// <summary>원본 `CONFIG.expCurve(l) = floor(700 × 1.8^(l-1))`(project_test.html:706).</summary>
        public static int RequiredExp(int level) => Mathf.FloorToInt(700f * Mathf.Pow(1.8f, level - 1));

        public int GetUpgradeLevel(UpgradeStat stat) => stat switch
        {
            UpgradeStat.Atk => upgrades.atk,
            UpgradeStat.Hp => upgrades.hp,
            UpgradeStat.Ms => upgrades.ms,
            UpgradeStat.AtkSpeed => upgrades.atkSpeed,
            UpgradeStat.Crit => upgrades.crit,
            _ => 0,
        };

        /// <summary>
        /// 골드가 충분하면 강화 1단계 구매(원본 `buyGoldUpgrade`, project_test.html:7042의 1회분).
        /// 비용은 <see cref="GoldUpgradeConfig.Cost"/>(현재 단계 기준, 원본 `upCost` :1268). 성공 시 true.
        /// </summary>
        public bool TryBuyUpgrade(UpgradeStat stat)
        {
            int level0 = GetUpgradeLevel(stat);
            int cost = GoldUpgradeConfig.Cost(stat, level0);
            if (gold < cost) return false;
            gold -= cost;
            SetUpgradeLevel(stat, level0 + 1);
            return true;
        }

        void SetUpgradeLevel(UpgradeStat stat, int newLevel)
        {
            switch (stat)
            {
                case UpgradeStat.Atk: upgrades.atk = newLevel; break;
                case UpgradeStat.Hp: upgrades.hp = newLevel; break;
                case UpgradeStat.Ms: upgrades.ms = newLevel; break;
                case UpgradeStat.AtkSpeed: upgrades.atkSpeed = newLevel; break;
                case UpgradeStat.Crit: upgrades.crit = newLevel; break;
            }
        }

        /// <summary>원본 `spTotal()`(project_test.html:1258) = 레벨-1. 1레벨엔 SP가 0이다.</summary>
        public int SpTotal => level - 1;
        /// <summary>원본 `spAvail()`(project_test.html:1259).</summary>
        public int SpAvailable => SpTotal - spUsed;

        /// <summary>
        /// 마법사 빌드 티어당 SP 비용. 원본 `SPEC.bow.move/conv.tiers[].cost`(project_test.html:900-913) —
        /// 두 갈래 모두 1,2,3,4,5로 동일하다.
        /// </summary>
        static readonly int[] MageTierCost = { 1, 2, 3, 4, 5 };

        /// <summary>
        /// 다음 티어를 습득한다(원본 `learnSkill`, project_test.html:7011). 갈래는 처음 배울 때 고정되고
        /// (`branch !== null && branch !== 선택` 이면 거부), 반드시 순서대로만(1→2→3→4→5) 배울 수 있다.
        /// </summary>
        public bool TryLearnMageTier(MageBranch branch)
        {
            if (branch == MageBranch.None) return false;
            if (mageBranch != MageBranch.None && mageBranch != branch) return false;
            int nextTier = mageTier + 1;
            if (nextTier > MageTierCost.Length) return false;
            int cost = MageTierCost[nextTier - 1];
            if (SpAvailable < cost) return false;
            mageBranch = branch;
            mageTier = nextTier;
            spUsed += cost;
            return true;
        }
    }

    /// <summary>원본 마법사 빌드 갈래. move=폭발 계열, conv=중력 계열(project_test.html:900-913).</summary>
    public enum MageBranch { None, Explosion, Gravity }

    /// <summary>
    /// 골드 강화 5종의 현재 단계. 원본 `CONFIG.upgrades`(project_test.html:731)와 이름을 맞췄으나
    /// `as`(공격속도)는 C# 예약어라 `atkSpeed`로 대체했다 — 트랙 A/B 둘 다 이 이름을 쓸 것.
    /// </summary>
    [System.Serializable]
    public class UpgradeLevels
    {
        public int atk;
        public int hp;
        public int ms;
        public int atkSpeed;
        public int crit;
    }
}
