namespace YokaiFront.Core
{
    /// <summary>
    /// 플레이어 진행 상태 — 원본 `meta`(project_test.html:1129 `defaultMeta()`)의 일부를 옮긴 것.
    /// **세이브 데이터다, SO가 아니다**(CLAUDE.md "세이브 데이터 vs 설정 데이터" 절 참고) — 플레이어마다
    /// 다르고 런타임에 계속 바뀌므로 순수 직렬화 가능 클래스로 둔다.
    ///
    /// 스프린트 2("성장곡선 검증", `docs/sprint2-handoff-split.md`)의 트랙 A/B 공유 계약 — 필드
    /// 이름·타입을 임의로 바꾸면 두 트랙이 동시에 깨진다. 트랙 A(처치 보상)는 <see cref="AddGold"/>/
    /// <see cref="AddExp"/>만 호출하고 `gold`/`exp` 필드를 직접 증가시키지 않는다 — 레벨업 판정(트랙 B
    /// 소유)이 `AddExp` 안에서 같이 일어나야 하기 때문이다.
    /// </summary>
    public class PlayerProfile
    {
        public int level = 1;
        public int exp = 0;
        public int gold = 0;
        public int spUsed = 0;
        public UpgradeLevels upgrades = new UpgradeLevels();

        /// <summary>트랙 A(처치 보상)가 호출. 레벨업 로직은 없음 — 골드는 즉시 누적만 하면 된다.</summary>
        public void AddGold(int amount) => gold += amount;

        /// <summary>
        /// 트랙 A(처치 보상)가 호출. 레벨업 판정(원본 `expCurve`/`gainExpMeta`, project_test.html:706,:1440)은
        /// 트랙 B가 여기 채운다 — 지금은 필드만 누적(트랙 B 착수 전 임시).
        /// </summary>
        public void AddExp(int amount) => exp += amount;
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
