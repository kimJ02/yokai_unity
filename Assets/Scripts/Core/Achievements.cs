using System;
using System.Collections.Generic;

namespace YokaiFront.Core
{
    /// <summary>
    /// 누적 통계 — 원본 `meta.stats`(project_test.html:1151)와 `meta.totalKills`(`:1152`).
    /// **윤회해도 사라지지 않는다** — 업적이 "이번 생"이 아니라 "지금까지"를 보기 때문이다.
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int totalKills;
        public int maxCombo;
        public int elites;
        public int shrines;
        public int bosses;
        public int rpEarned;
        public int goldEarned;
        public int pulls;
    }

    /// <summary>업적 하나. 원본 `ACHIEVEMENTS`의 한 줄(`:802`~`:838`).</summary>
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        /// <summary>달성 조건. 프로필 전체를 보고 판단한다(원본 `chk()`가 전역 `meta`를 보는 것과 같다).</summary>
        public Func<PlayerProfile, bool> Check;
    }

    /// <summary>
    /// 업적 — 원본 `ACHIEVEMENTS`(project_test.html:802)와 `checkAchievements()`(`:1451`).
    ///
    /// 원본 주석 그대로 "현재 캐릭터/윤회/강화 루프에 맞춘 진행 목표"다. 보상은 없고
    /// **어디까지 왔는지를 보여주는 지표**다 — 그래서 조건을 임의로 완화하면 의미가 사라진다.
    ///
    /// ## 아직 판정할 수 없는 것
    /// 원본 업적 중 메카닉·섬영 빌드 5층(`mechanic5`·`blade5`·`specAll`)은 그 스킬트리가 아직
    /// 없어서 **뺐다**(항상 거짓인 업적을 목록에 두면 "달성 불가"가 정상인지 버그인지 알 수 없다).
    /// 해당 트리를 만들 때 여기 줄만 추가하면 된다.
    /// </summary>
    public static class Achievements
    {
        static Achievement A(string id, string nm, string desc, Func<PlayerProfile, bool> chk) =>
            new Achievement { id = id, name = nm, description = desc, Check = chk };

        static int UpgradeTotal(PlayerProfile p) =>
            p.upgrades.atk + p.upgrades.hp + p.upgrades.ms + p.upgrades.atkSpeed + p.upgrades.crit;

        static int ItemKinds(PlayerProfile p)
        {
            int n = 0;
            foreach (var s in p.items.stacks) if (s.count > 0) n++;
            return n;
        }

        static bool HasGrade(PlayerProfile p, ItemGrade grade)
        {
            foreach (var s in p.items.stacks)
            {
                var def = ItemDatabase.Get(s.id);
                if (def != null && def.grade == grade && s.count > 0) return true;
            }
            return false;
        }

        static bool AnyCapped(PlayerProfile p)
        {
            foreach (var s in p.items.stacks)
                if (s.count >= ItemDatabase.CapOf(s.id) && s.count > 0) return true;
            return false;
        }

        /// <summary>원본 `ACHIEVEMENTS` 순서 그대로.</summary>
        public static readonly Achievement[] All =
        {
            A("kill1",     "첫 사냥",       "요괴 1마리 처치",        p => p.stats.totalKills >= 1),
            A("kill500",   "요괴 사냥꾼",   "누적 500마리 처치",      p => p.stats.totalKills >= 500),
            A("kill5k",    "백귀 참살",     "누적 5,000마리 처치",    p => p.stats.totalKills >= 5000),
            A("kill20k",   "요괴의 재앙",   "누적 20,000마리 처치",   p => p.stats.totalKills >= 20000),
            A("combo50",   "연격의 미학",   "콤보 50 달성",           p => p.stats.maxCombo >= 50),
            A("combo200",  "무아의 경지",   "콤보 200 달성",          p => p.stats.maxCombo >= 200),
            A("elite",     "강적 격파",     "엘리트 요괴 처치",       p => p.stats.elites >= 1),
            A("elite50",   "엘리트 사냥",   "엘리트 50마리 처치",     p => p.stats.elites >= 50),
            A("shrine",    "성소 파괴자",   "요괴 성소 파괴",         p => p.stats.shrines >= 1),
            A("reg1",      "숲의 수호자",   "1지역 보스 처치",        p => p.IsBossCleared(1)),
            A("reg3",      "골짜기 정벌",   "3지역 정복",             p => p.IsBossCleared(3)),
            A("reg5",      "성곽 함락",     "5지역 정복",             p => p.IsBossCleared(5)),
            A("reg7",      "지옥의 문",     "7지역 정복",             p => p.IsBossCleared(7)),
            A("reg9",      "백귀야행 종결", "9지역 정복 — 완주!",     p => p.IsBossCleared(9)),
            A("boss5",     "보스 헌터",     "보스 5종 처치",          p => p.stats.bosses >= 5),
            A("boss9",     "백귀 토벌자",   "보스 9종 처치",          p => p.stats.bosses >= 9),
            A("spec1",     "전문화 입문",   "캐릭터 빌드 1층 개방",   p => p.mageTier >= 1),
            A("mage5",     "대마법사",      "마법사 빌드 5층 완성",   p => p.mageTier >= 5),
            A("reborn1",   "윤회의 시작",   "첫 윤회",                p => p.rebirths >= 1),
            A("reborn3",   "세 번째 삶",    "윤회 3회",               p => p.rebirths >= 3),
            A("reborn5",   "거듭된 환생",   "윤회 5회",               p => p.rebirths >= 5),
            A("wall9",     "윤회의 증명",   "9지역 권장 윤회 달성",   p => p.rebirths >= RebirthConfig.RequiredRebirths(9)),
            A("rp100",     "윤회의 무게",   "누적 윤회 포인트 100",   p => p.stats.rpEarned >= 100),
            A("rp1k",      "윤회의 주인",   "누적 윤회 포인트 1,000", p => p.stats.rpEarned >= 1000),
            A("goldUp50",  "단련가",        "골드 강화 총합 50 달성", p => UpgradeTotal(p) >= 50),
            A("item1",     "첫 수확",       "아이템 1종 획득",        p => ItemKinds(p) >= 1),
            A("item10",    "수집가",        "아이템 10종 획득",       p => ItemKinds(p) >= 10),
            A("itemAll",   "만물의 주인",   "아이템 전 종류 수집",    p => ItemKinds(p) >= ItemDatabase.All.Length),
            A("legend1",   "전설의 발견",   "전설 등급 아이템 획득",  p => HasGrade(p, ItemGrade.Legend)),
            A("maxDup",    "한계 돌파",     "아이템 1종을 중복 상한까지 수집", AnyCapped),
            A("pull100",   "백 번의 기원",  "누적 100회 뽑기",        p => p.stats.pulls >= 100),
            A("gold100k",  "축재",          "누적 골드 100,000 획득", p => p.stats.goldEarned >= 100000),
        };

        /// <summary>
        /// 새로 달성한 업적을 확인하고 기록한다 — 원본 `checkAchievements()`(project_test.html:1451).
        /// 처치·윤회·뽑기처럼 통계가 움직인 뒤에 부르면 된다. **이미 달성한 건 다시 안 알린다.**
        /// </summary>
        /// <returns>이번에 새로 달성한 것들(없으면 빈 목록).</returns>
        public static List<Achievement> CheckNew(PlayerProfile p)
        {
            var fresh = new List<Achievement>();
            foreach (var a in All)
            {
                if (p.achieved.Contains(a.id)) continue;
                if (!a.Check(p)) continue;
                p.achieved.Add(a.id);
                fresh.Add(a);
            }
            return fresh;
        }

        public static int Count(PlayerProfile p)
        {
            int n = 0;
            foreach (var a in All) if (p.achieved.Contains(a.id)) n++;
            return n;
        }
    }
}
