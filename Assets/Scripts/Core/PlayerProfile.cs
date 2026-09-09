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
    [System.Serializable]
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

        /// <summary>윤회 포인트 — 가챠 재화. 원본 `meta.rp`(project_test.html:1147). **윤회해도 안 없어진다.**</summary>
        public int rp = 0;
        /// <summary>지금까지 윤회한 횟수. 원본 `meta.rebirths` — 윤회 장벽 계산의 기준이다.</summary>
        public int rebirths = 0;

        /// <summary>
        /// 지역별 진행도 — 원본 `meta.regions[r] = { kills, bossUnlocked, bossCleared }`(project_test.html:1131).
        /// 인덱스는 0부터라 `[0]`이 1지역이다.
        ///
        /// 셋이 사슬처럼 이어진다: **누적 처치 100 → 보스 해금 → 보스 격파 → 다음 지역 개방**.
        /// 하나라도 빠지면 진행이 막히거나(해금 안 됨) 압력이 사라진다(전부 개방).
        /// </summary>
        public int[] regionKills = new int[RegionConfig.Count];
        public bool[] regionBossUnlocked = new bool[RegionConfig.Count];
        public bool[] regionBossCleared = new bool[RegionConfig.Count];

        static bool InRange(int region) => region >= 1 && region <= RegionConfig.Count;

        /// <summary>원본 `regionOpen(r) = r === 1 || regions[r-1].bossCleared`(project_test.html:6434).</summary>
        public bool IsRegionOpen(int region)
        {
            if (region <= 1) return true;
            if (!InRange(region)) return false;
            return regionBossCleared[region - 2]; // 이전 지역
        }

        public int RegionKills(int region) => InRange(region) ? regionKills[region - 1] : 0;
        public bool IsBossUnlocked(int region) => InRange(region) && regionBossUnlocked[region - 1];
        public bool IsBossCleared(int region) => InRange(region) && regionBossCleared[region - 1];

        /// <summary>
        /// 이 지역에서 1마리 잡았다. 원본 `:1866`~`:1869`:
        /// <code>
        /// if (R.kills &lt; regionKillTarget) { R.kills++; if (R.kills >= target) R.bossUnlocked = true; }
        /// </code>
        /// **목표에 도달하면 더 안 센다** — 진행 게이지가 100/100에서 멈춘다.
        /// </summary>
        /// <returns>이번 처치로 보스가 새로 해금됐으면 true.</returns>
        public bool RegisterRegionKill(int region)
        {
            if (!InRange(region)) return false;
            int i = region - 1;
            if (regionKills[i] >= RunState.RegionKillTarget) return false;

            regionKills[i]++;
            if (regionKills[i] >= RunState.RegionKillTarget && !regionBossUnlocked[i])
            {
                regionBossUnlocked[i] = true;
                return true;
            }
            return false;
        }

        /// <summary>보스를 잡았다 — 다음 지역이 열린다. 원본 `onBossKilled`(project_test.html:4282).</summary>
        public void MarkBossCleared(int region)
        {
            if (InRange(region)) regionBossCleared[region - 1] = true;
        }

        /// <summary>
        /// 지금 윤회하면 받을 윤회 포인트 — 원본 `rpPreview()`(project_test.html:6505).
        /// **이번 생에 정복한 지역만** 센다. 원본 주석 그대로 "갈아넣은 시간이 아니라
        /// '어디까지 뚫었나'가 보상이다" — 시간을 오래 쓴다고 늘지 않는다.
        /// </summary>
        public int RebirthPointPreview()
        {
            int sum = 0;
            for (int r = 1; r <= RegionConfig.Count; r++)
                if (IsBossCleared(r)) sum += RebirthConfig.RpOfRegion(r);
            return sum;
        }

        /// <summary>이번 생에 정복한 지역 수. 원본 `clearedRegions()`(`:6496`).</summary>
        public int ClearedRegionCount()
        {
            int n = 0;
            for (int r = 1; r <= RegionConfig.Count; r++) if (IsBossCleared(r)) n++;
            return n;
        }

        /// <summary>
        /// 윤회한다 — 원본 `doRebirth()`(project_test.html:6509).
        ///
        /// **초기화**: 레벨·경험치·골드·골드 강화·전문화(SP)·지역 진행.
        /// **유지**: 윤회 포인트·윤회 횟수(그리고 나중에 아이템).
        ///
        /// 정복한 지역이 하나도 없으면 아무 일도 안 한다(원본 `if (gain &lt; 1) return`) —
        /// 얻을 게 없는데 진행만 날리는 걸 막는 안전장치다.
        /// </summary>
        /// <returns>얻은 윤회 포인트. 0이면 윤회가 일어나지 않았다.</returns>
        public int DoRebirth()
        {
            int gain = RebirthPointPreview();
            if (gain < 1) return 0;

            rp += gain;
            rebirths++;

            level = 1;
            exp = 0;
            gold = 0;
            upgrades = new UpgradeLevels();
            spUsed = 0;
            mageBranch = MageBranch.None;
            mageTier = 0;

            regionKills = new int[RegionConfig.Count];
            regionBossUnlocked = new bool[RegionConfig.Count];
            regionBossCleared = new bool[RegionConfig.Count];

            return gain;
        }

        /// <summary>레벨이 실제로 오를 때(한 번 이상) 1회 발생. `Characters/PlayerHealth`가 구독해서
        /// 최대체력 재계산 + 풀피 회복을 한다(원본 `project_test.html:1850`, 레벨업 때만 일어나고
        /// 골드 강화 구매 자체로는 즉시 반영되지 않는다 — 원본과 동일한 비직관적 동작).</summary>
        // 이벤트는 직렬화 대상이 아니다 — 뒤에 숨은 델리게이트 필드가 private이라 `JsonUtility`가
        // 애초에 건드리지 않는다(`[NonSerialized]`는 이벤트 선언에 못 붙는다, CS0592).
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
