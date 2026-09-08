using UnityEngine;

namespace YokaiFront.Combat
{
    /// <summary>
    /// 최종 피해량 계산. 원본 `dealDamage()`(`project_test.html:1664`)의 피해 계산식에서
    /// **이번 스프린트 범위에 해당하는 항만** 옮긴 것이다.
    ///
    /// 원본 전체 식:
    ///   max(1, round( statAtk() × dmgMultAll() × mult × vulnMult × lvF × itemDmgVs(e)
    ///                 × rebirthWallPlayerDmgMult() × rand(0.9,1.1) × (crit ? critMultOf() : 1) ))
    ///
    /// 지금 구현한 항: `statAtk × mult`(= 인자로 받는 baseDamage) · `rand(0.9,1.1)` · 치명타 · `max(1, round())`
    /// 빠진 항(전부 해당 시스템이 아직 없어서 — HANDOFF.md "범위 밖" 참고):
    ///   `dmgMultAll()`(콤보·살기·아이템·성소) · `vulnMult`(균열의 각인) · `lvF`(레벨 페널티)
    ///   · `itemDmgVs`(상황부 아이템) · `rebirthWallPlayerDmgMult`(윤회 벽)
    /// 나중에 그 시스템들이 생기면 **여기 한 곳만** 고치면 되도록 계산을 이 클래스로 모았다.
    ///
    /// 이 클래스가 `Combat`에 있는 이유: 무기에 종속되지 않는 전투 공용 인프라(CLAUDE.md 폴더 구조 규칙의
    /// "피해 판정 헬퍼")이고, `Characters`(2층)가 `Combat`(1층)을 참조할 수 있어 마법사 무기에서도 쓸 수 있다.
    /// </summary>
    public static class DamageCalculator
    {
        // 원본 CONFIG.player (`:605`) — 스탯 시스템(골드 강화·아이템)이 아직 없어 기본값 고정.
        // 스탯이 생기면 statCrit()/critMultOf()에 해당하는 값을 인자로 받도록 바꾼다.
        public const float BaseCritChance = 0.10f;   // 원본 baseCrit: 0.10
        public const float CritMultiplier = 1.7f;    // 원본 critMult: 1.7

        // 원본 `rand(0.9, 1.1)` (`:983`, `:1664`) — 같은 공격이라도 피해가 ±10% 흔들린다.
        public const float VarianceMin = 0.9f;
        public const float VarianceMax = 1.1f;

        /// <summary>
        /// 최종 피해량을 굴린다. `baseDamage`는 무기 배수·차지 배수까지 이미 반영된 값을 넘긴다
        /// (예: `MageAttack`이 `baseDamage × (1 + chargeDmgMult × chargeK)`를 계산해서 전달).
        /// </summary>
        public static int Roll(float baseDamage, out bool isCrit)
        {
            isCrit = Random.value < BaseCritChance;
            float variance = Random.Range(VarianceMin, VarianceMax);
            float raw = baseDamage * variance * (isCrit ? CritMultiplier : 1f);
            return Mathf.Max(1, Mathf.RoundToInt(raw)); // 원본 max(1, round(...))
        }

        /// <summary>치명타 여부가 필요 없을 때 쓰는 간편 버전.</summary>
        public static int Roll(float baseDamage) => Roll(baseDamage, out _);
    }
}
