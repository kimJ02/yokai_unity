using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 플레이어 진행 상태 — 원본 `meta`(project_test.html:1129 `defaultMeta()`)의 일부를 옮긴 것.
    /// **세이브 데이터다, SO가 아니다**(CLAUDE.md "세이브 데이터 vs 설정 데이터" 절 참고) — 플레이어마다
    /// 다르고 런타임에 계속 바뀌므로 순수 직렬화 가능 클래스로 둔다.
    ///
    /// 스프린트 2("성장곡선 검증", `docs/sprint2-handoff-split.md`)의 트랙 A/B 공유 계약 — 필드
    /// 이름·타입을 임의로 바꾸면 두 트랙이 동시에 깨진다. 트랙 A(처치 보상)는 <see cref="AddGold"/>/
    /// <see cref="AddExp"/>만 호출하고 `gold`/`exp` 필드를 직접 증가시키지 않는다.
    /// </summary>
    public class PlayerProfile
    {
        public int level = 1;
        public int exp = 0;
        public int gold = 0;
        public int spUsed = 0;
        public UpgradeLevels upgrades = new UpgradeLevels();

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
    }

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
