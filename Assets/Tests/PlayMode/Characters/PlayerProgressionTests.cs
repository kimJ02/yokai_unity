using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YokaiFront.Characters;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Tests.PlayMode
{

/// <summary>
/// 스프린트 2 트랙 B(레벨업 + 골드 강화 + 스탯 적용) 검증. `docs/sprints/03-growth-curve-worksplit.md`의
/// 확정 공식·원본 인용을 그대로 대상으로 삼는다.
///
/// `Core.ProfileService.Current`는 정적 싱글턴이라 테스트끼리 공유되면 오염된다(이 프로젝트에서
/// `EnemyHealth` 테스트 오염을 이미 한 번 겪은 것과 같은 종류) — 매 테스트 전에 반드시 리셋한다.
/// </summary>
public class PlayerProgressionTests
{
    [SetUp]
    public void ResetProfile() => ProfileService.Reset();

    [TearDown]
    public void CleanupProfile() => ProfileService.Reset();

    // ---- 순수 계산 (레벨/경험치, project_test.html:706) ----

    [Test]
    public void RequiredExp_MatchesOriginalExpCurve()
    {
        // 원본 expCurve(l) = floor(700 × 1.8^(l-1))
        Assert.AreEqual(700, PlayerProfile.RequiredExp(1));
        Assert.AreEqual(1260, PlayerProfile.RequiredExp(2));
        Assert.AreEqual(Mathf.FloorToInt(700f * Mathf.Pow(1.8f, 4)), PlayerProfile.RequiredExp(5));
    }

    [Test]
    public void AddExp_CarriesOverAndCanLevelUpMultipleTimesInOneCall()
    {
        var p = new PlayerProfile();
        int need1 = PlayerProfile.RequiredExp(1); // 700
        int need2 = PlayerProfile.RequiredExp(2); // 1260

        p.AddExp(need1 + need2 + 50); // 2레벨을 한 번에 넘기고 50 남아야 함

        Assert.AreEqual(3, p.level, "몰아서 지급했는데 두 번 레벨업이 안 됐다");
        Assert.AreEqual(50, p.exp, "초과분 이월이 안 됐다");
    }

    [Test]
    public void AddExp_FiresLeveledUp_OnlyWhenLevelActuallyIncreases()
    {
        var p = new PlayerProfile();
        int fired = 0;
        p.LeveledUp += () => fired++;

        p.AddExp(10); // 700 미만 — 레벨업 없음
        Assert.AreEqual(0, fired, "레벨업 안 했는데 이벤트가 발생했다");

        p.AddExp(PlayerProfile.RequiredExp(1)); // 딱 채움
        Assert.AreEqual(1, fired, "레벨업했는데 이벤트가 안 왔다");
    }

    // ---- 순수 계산 (골드 강화 비용, project_test.html:731,:1268) ----

    [Test]
    public void GoldUpgradeCost_MatchesOriginalFormula_ForEachStat()
    {
        // cost(lv) = floor(base × mult^lv). lv=0(구매 전)일 때는 항상 base 그대로.
        Assert.AreEqual(22, GoldUpgradeConfig.Cost(UpgradeStat.Atk, 0));
        Assert.AreEqual(20, GoldUpgradeConfig.Cost(UpgradeStat.Hp, 0));
        Assert.AreEqual(18, GoldUpgradeConfig.Cost(UpgradeStat.Ms, 0));
        Assert.AreEqual(18, GoldUpgradeConfig.Cost(UpgradeStat.AtkSpeed, 0));
        Assert.AreEqual(25, GoldUpgradeConfig.Cost(UpgradeStat.Crit, 0));

        // lv=3일 때 지수 증가 확인 (atk: floor(22 × 1.14^3))
        Assert.AreEqual(Mathf.FloorToInt(22f * Mathf.Pow(1.14f, 3)), GoldUpgradeConfig.Cost(UpgradeStat.Atk, 3));
    }

    [Test]
    public void TryBuyUpgrade_FailsWithoutEnoughGold_SucceedsAndDeductsWithEnough()
    {
        var p = new PlayerProfile();
        p.gold = 21; // atk 1단계 비용(22)보다 1 부족

        Assert.IsFalse(p.TryBuyUpgrade(UpgradeStat.Atk), "골드 부족한데 구매가 성공했다");
        Assert.AreEqual(0, p.upgrades.atk);
        Assert.AreEqual(21, p.gold, "실패했는데 골드가 깎였다");

        p.gold = 22;
        Assert.IsTrue(p.TryBuyUpgrade(UpgradeStat.Atk), "골드가 정확히 충분한데 구매가 실패했다");
        Assert.AreEqual(1, p.upgrades.atk);
        Assert.AreEqual(0, p.gold);

        // 두 번째 구매는 비용이 올라야 함(지수 증가) — 방금 번 것만큼만 주면 또 실패해야 정상
        p.gold = 22;
        Assert.IsFalse(p.TryBuyUpgrade(UpgradeStat.Atk), "지수 증가한 2단계 비용을 1단계 비용으로 살 수 있었다");
    }

    // ---- 순수 계산 (파생 스탯, project_test.html:1284~1288) ----

    [Test]
    public void PlayerStatCalculator_MatchesOriginalFormulas()
    {
        var p = new PlayerProfile();
        p.upgrades.atk = 2; p.upgrades.hp = 2; p.upgrades.ms = 2; p.upgrades.atkSpeed = 2; p.upgrades.crit = 2;

        Assert.AreEqual(10f + 2 * 3f, PlayerStatCalculator.ComputeAtk(p), 0.001f);
        Assert.AreEqual(Mathf.Round(100f + 2 * 25f), PlayerStatCalculator.ComputeMaxHp(p), 0.001f);
        Assert.AreEqual(1f + 2 * 0.04f, PlayerStatCalculator.ComputeMoveSpeedMultiplier(p), 0.001f);
        Assert.AreEqual(1f + 2 * 0.04f, PlayerStatCalculator.ComputeAttackSpeedMultiplier(p), 0.001f);
        Assert.AreEqual(0.10f + 2 * 0.02f, PlayerStatCalculator.ComputeCritChance(p), 0.001f);
    }

    [Test]
    public void DamageCalculator_CritChance_ZeroNeverCrits_OneAlwaysCrits()
    {
        for (int i = 0; i < 50; i++)
        {
            DamageCalculator.Roll(100f, 0f, out bool neverCrit);
            Assert.IsFalse(neverCrit, "critChance=0인데 치명타가 나왔다");
            DamageCalculator.Roll(100f, 1f, out bool alwaysCrit);
            Assert.IsTrue(alwaysCrit, "critChance=1인데 치명타가 안 나왔다");
        }
    }

    // ---- 통합: 레벨업 시에만 최대체력 재계산 + 풀피(원본 :1850, 강화 구매 자체로는 즉시 반영 안 됨) ----

    [UnityTest]
    public IEnumerator PlayerHealth_RecalculatesMaxHp_OnlyOnLevelUp_NotOnUpgradePurchaseAlone()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        var health = go.AddComponent<PlayerHealth>();
        yield return null;

        Assert.AreEqual(100f, health.MaxHp, 0.01f);

        // HP 강화만 사고 레벨업은 안 함 — 원본처럼 즉시 반영되면 안 된다.
        ProfileService.Current.gold = 100000;
        ProfileService.Current.TryBuyUpgrade(UpgradeStat.Hp);
        yield return null;
        Assert.AreEqual(100f, health.MaxHp, 0.01f, "레벨업 없이 강화만 샀는데 최대체력이 바로 올랐다(원본과 다름)");

        // 이제 레벨업 — 여기서 비로소 반영 + 풀피
        health.TakeDamage(50f, null);
        Assert.Less(health.CurrentHp, health.MaxHp);
        ProfileService.Current.AddExp(PlayerProfile.RequiredExp(1));
        yield return null;

        float expectedMaxHp = PlayerStatCalculator.ComputeMaxHp(ProfileService.Current);
        Assert.AreEqual(expectedMaxHp, health.MaxHp, 0.01f, "레벨업했는데 최대체력이 강화 반영해서 재계산되지 않았다");
        Assert.AreEqual(expectedMaxHp, health.CurrentHp, 0.01f, "레벨업했는데 풀피 회복이 안 됐다");

        Object.Destroy(go);
        yield return null;
    }

    // ---- 통합: MageAttack이 매 프레임 강화된 스탯을 반영 ----

    [UnityTest]
    public IEnumerator MageAttack_ReflectsUpgradedAtkAndAttackSpeed()
    {
        var go = new GameObject("TestCaster");
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CircleCollider2D>();
        var mage = go.AddComponent<MageAttack>();
        yield return null;

        float baseDmgBefore = mage.baseDamage;
        float cooldownBefore = mage.cooldown;
        Assert.AreEqual(10f * 0.9f, baseDmgBefore, 0.01f, "강화 전 기본 피해가 statAtk(10)×0.9와 다르다");

        ProfileService.Current.gold = 100000;
        ProfileService.Current.TryBuyUpgrade(UpgradeStat.Atk);
        ProfileService.Current.TryBuyUpgrade(UpgradeStat.AtkSpeed);
        yield return null; // MageAttack.Update()가 한 번 더 돌아야 반영

        Assert.Greater(mage.baseDamage, baseDmgBefore, "공격력 강화를 샀는데 baseDamage가 그대로다");
        Assert.Less(mage.cooldown, cooldownBefore, "공격속도 강화를 샀는데 쿨다운이 안 줄었다");

        Object.Destroy(go);
        yield return null;
    }

    // 참고: CharacterMover2D의 이동속도 강화 반영은 별도 통합 테스트를 만들지 않았다 — 실제 이동은
    // Input.GetKey(좌우 화살표)가 있어야 vx≠0이 되는데 이 프로젝트엔 Input 시뮬레이션 수단이 없어서
    // (GameInput 중앙화 클래스 미구현, CLAUDE.md 참고) h=0 고정이라 배율을 곱해도 결과가 항상 0으로
    // 나와 검증이 안 된다. 배율 계산 자체는 `PlayerStatCalculator_MatchesOriginalFormulas`가 이미
    // 검증했고, 코드 리뷰로 `CharacterMover2D.FixedUpdate()`가 그 값을 실제로 곱하는 것만 확인했다.

    // ---- 통합: 사망 → 정지 → R키 재시작 ----

    [UnityTest]
    public IEnumerator PlayerDeathHandler_FreezesOnDeath_RevivesOnR()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<Rigidbody2D>().gravityScale = 0f;
        go.AddComponent<CircleCollider2D>().radius = 0.5f; // CharacterMover2D가 RequireComponent로 요구
        var health = go.AddComponent<PlayerHealth>();
        var mover = go.AddComponent<CharacterMover2D>();
        var deathHandler = go.AddComponent<PlayerDeathHandler>();
        yield return null;

        health.TakeDamage(500f, null); // 큰 피해로 즉시 사망
        yield return null;

        Assert.IsTrue(deathHandler.IsDead, "체력이 0인데 사망 처리가 안 됐다");
        Assert.IsFalse(mover.enabled, "죽었는데 이동 컴포넌트가 그대로 켜져 있다(정지 안 됨)");

        // R 키 입력 자체는 시뮬레이트 불가(실제 키 이벤트 큐 의존, 이 프로젝트 다른 곳과 같은 제약)라
        // Update()가 누르면 호출하는 바로 그 public 메서드를 직접 불러 전체 부활 경로를 검증한다.
        deathHandler.Revive();
        Assert.IsTrue(health.CurrentHp > 0f, "Revive 후에도 체력이 0이다");
        Assert.IsTrue(mover.enabled, "부활했는데 이동이 다시 안 켜졌다");
        Assert.IsFalse(deathHandler.IsDead, "부활했는데 여전히 사망 상태다");

        Object.Destroy(go);
        yield return null;
    }
}

}
