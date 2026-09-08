using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 골드 강화가 반영된 파생 스탯. 원본 `statAtk`/`statMaxHp`/`statMs`/`statAs`/`statCrit`
    /// (project_test.html:1284~1288)에서 아이템 항(전부 0, 아이템 시스템 없음)과 캐릭터 배수
    /// (마법사=1이라 무시 가능)를 뺀 형태다.
    ///
    /// 기본값은 원본 `CONFIG.player`(:605)·`CONFIG.bow.dmg`(:609)를 그대로 옮긴 것.
    /// `BaseCritChance`가 `Combat.DamageCalculator.BaseCritChance`와 값이 겹치는데(둘 다 0.10) —
    /// `Core`는 `Combat`을 참조할 수 없어서(asmdef 계층, Core는 참조 대상 없음) 부득이 중복
    /// 정의했다. SO 전환(스프린트 4)에서 데이터 하나로 합칠 것 — 값을 바꿀 땐 두 곳 다 같이.
    /// </summary>
    public static class PlayerStatCalculator
    {
        public const float BaseAtk = 10f;          // CONFIG.player.baseAtk
        public const float BaseMaxHp = 100f;       // CONFIG.player.baseHp
        public const float BaseCritChance = 0.10f; // CONFIG.player.baseCrit
        public const float BowDamageMult = 0.9f;   // CONFIG.bow.dmg(:609) — 마법사 무기 배수

        public static float ComputeAtk(PlayerProfile p) => BaseAtk + p.upgrades.atk * 3f;

        public static float ComputeMaxHp(PlayerProfile p) => Mathf.Round(BaseMaxHp + p.upgrades.hp * 25f);

        public static float ComputeMoveSpeedMultiplier(PlayerProfile p) => 1f + p.upgrades.ms * 0.04f;

        public static float ComputeAttackSpeedMultiplier(PlayerProfile p) => 1f + p.upgrades.atkSpeed * 0.04f;

        public static float ComputeCritChance(PlayerProfile p) => BaseCritChance + p.upgrades.crit * 0.02f;
    }
}
