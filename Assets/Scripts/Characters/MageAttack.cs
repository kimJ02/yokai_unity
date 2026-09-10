using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
/// <summary>
/// 마법사(원본 무기 "bow"="마법 지팡이") Z 공격 — 원본 `bowFire()`/`CONFIG.bow`(project_test.html:1932-1977)
/// 그대로 이식. Z를 누르고 있으면 차지, 떼면 그 시점 차지율(chargeK = chargeT/chargeMax)로 마법탄 하나를
/// 발사한다. X 스킬(`tryMageSkill`, :2164)은 <see cref="TryUseSkill"/> — 빌드에 따라 불길 이동
/// 텔레포트(폭발 계열)나 대붕괴(중력 계열)로 갈라진다.
///
/// 스킬트리 자체(갈래 선택·티어 습득)는 <see cref="PlayerProfile.TryLearnMageTier"/> — 이 클래스는
/// 그 결과(`ProfileService.Current.mageBranch`/`mageTier`)를 매 프레임 읽어서 공식에 반영만 한다
/// (원본 `bowFire`가 `meta.skills.bow`를 매번 새로 읽는 것과 동일한 방식).
///
/// PlayerAttack(범용 근접 판정)을 대체한다 — 이 캐릭터를 쓰는 동안은 씬에 둘 다 안 붙인다.
/// </summary>
public class MageAttack : MonoBehaviour, ICharacterKit, IRunResettable
{
    public CharacterId Character => CharacterId.Mage;
    /// <summary>마법사는 원본 `updatePlayerCommon`의 즉시-속도 이동을 쓴다(관성은 섬영 전용).</summary>
    public CharacterMover2D.MoveMode RequiredMoveMode => CharacterMover2D.MoveMode.Instant;

    /// <summary>이 캐릭터로 전환됐을 때 — 쿨다운·차지를 초기화한다.</summary>
    public void OnSelected()
    {
        cdTimer = 0f;
        ultCdTimer = 0f;
        CancelCharge();
    }

    /// <summary>다른 캐릭터로 바뀔 때 — 진행 중이던 차지와 감속·표시물을 정리한다.</summary>
    public void OnDeselected() => CancelCharge();

    /// <summary>
    /// 새 사냥 시작 시 — 원본 `resetPlayerForRun()`의 `p.atkCds`·`p.ultCd`·`p.charging` 초기화
    /// (project_test.html:1516·:1526·:1534)와 같은 일이라 캐릭터 전환 초기화를 그대로 재사용한다.
    /// 쿨다운이 남은 채로 런에 들어가면 입장 직후 잠깐 공격이 안 나간다.
    /// </summary>
    public void ResetForRun() => OnSelected();

    void CancelCharge()
    {
        charging = false;
        chargeT = 0f;
        if (chargeIndicator != null) chargeIndicator.gameObject.SetActive(false);
        if (mover != null) mover.SpeedMultiplier = 1f;
    }

    [Header("원본 CONFIG.bow 그대로 (거리·속도는 100px=1유닛 축척)")]
    // cooldown/baseDamage는 매 프레임 Core.PlayerStatCalculator + MageSpecConfig에서 다시 계산되는
    // "표시용 현재값"이다 — 인스펙터에서 수정해도 다음 프레임에 덮어써진다(BaseCooldownConst만 진짜 기준값).
    public float cooldown = 0.5f;          // B.cd ÷ 공격속도 배수 × 티어 쿨감(project_test.html:1941-1942)
    public float baseDamage = 9f;          // statAtk() × B.dmg(0.9) — 골드 강화(atk)가 반영됨
    const float BaseCooldownConst = 0.5f;  // 원본 B.cd(project_test.html:609) — 이 값만 고정 기준
    public float chargeMax = 1.0f;         // B.chargeMax
    public float chargeDmgMult = 1.6f;     // B.chargeDmg
    public int basePierce = 2;             // B.pierce
    public int chargePierce = 4;           // B.chargePierce
    public float projectileSpeed = 10.8f;  // B.speed 1080px/s
    public float projectileRange = 8.8f;   // B.range 880px
    public float chargeMoveSlow = 0.5f;    // 원본 chargeSlow
    public float nearestEnemySearchRadius = 6.2f; // 원본 nearestEnemy(maxDist=620px)(project_test.html:2155)

    public Sprite boltSprite;              // 런타임 AssetDatabase 호출을 피하려고 씬 빌더가 미리 꽂아준다
    public Color boltColor = new Color(0.55f, 0.8f, 1f);

    CharacterMover2D mover;
    PlayerHealth health;
    float cdTimer;
    bool charging;
    float chargeT;
    float ultCdTimer;
    SpriteRenderer chargeIndicator;

    void Awake()
    {
        mover = GetComponent<CharacterMover2D>();
        health = GetComponent<PlayerHealth>();

        var ind = new GameObject("ChargeIndicator");
        ind.transform.SetParent(transform, false);
        chargeIndicator = ind.AddComponent<SpriteRenderer>();
        chargeIndicator.sprite = boltSprite;
        chargeIndicator.color = new Color(boltColor.r, boltColor.g, boltColor.b, 0.6f);
        chargeIndicator.sortingOrder = 4;
        ind.SetActive(false);
    }

    void Update()
    {
        var profile = ProfileService.Current;
        var branch = profile.mageBranch;
        int tier = profile.mageTier;

        // 골드 강화가 반영된 파생 스탯을 매 프레임 새로 계산(원본 statAtk()/statAs()가 호출마다
        // 다시 계산되는 함수인 것과 동일한 방식) — 필드 자기 자신이 아니라 항상 프로필+상수에서
        // 새로 굴리므로 매 프레임 곱해지는 식으로 값이 누적(compounding)되는 버그가 없다.
        baseDamage = PlayerStatCalculator.ComputeAtk(profile) * PlayerStatCalculator.BowDamageMult;
        // 원본 `p.bowCdMax = B.cd / statAs() * cdMult() * cdBonus`(:1942) — `cdMult()`가 '시간의 조각'이다.
        cooldown = BaseCooldownConst / PlayerStatCalculator.ComputeAttackSpeedMultiplier(profile)
            * PlayerStatCalculator.ComputeCooldownMultiplier(profile)
            * MageSpecConfig.BowCooldownBonus(branch, tier); // 원본 :1941-1942

        // 이번 프레임 시작 시점(직전 프레임까지의) 차지 상태를 기준으로 감속을 먼저 적용한다 —
        // 발사(release)되는 바로 그 프레임도 "떼기 직전까지는 차지 중"이었으므로 감속이 맞다.
        // 아래에서 곧바로 charging이 꺼질 수 있어(발사 처리) 순서를 이렇게 잡아야 한다.
        if (mover != null) mover.SpeedMultiplier = charging ? chargeMoveSlow : 1f;

        cdTimer -= Time.deltaTime;
        ultCdTimer -= Time.deltaTime;

        if (GameInput.UltDown && ultCdTimer <= 0f) TryUseSkill(profile, branch, tier);

        bool held = GameInput.AttackHeld;
        if (held && !charging && cdTimer <= 0f)
        {
            charging = true;
            chargeT = 0f;
            chargeIndicator.gameObject.SetActive(true);
        }

        if (charging)
        {
            chargeT = Mathf.Min(chargeMax, chargeT + Time.deltaTime);
            float k = chargeT / chargeMax;
            chargeIndicator.transform.localScale = Vector3.one * (0.35f + k * 0.5f);

            if (!held)
            {
                Fire(k, branch, tier);
                charging = false;
                chargeIndicator.gameObject.SetActive(false);
                cdTimer = cooldown;
            }
        }
    }

    void Fire(float chargeK, MageBranch branch, int tier)
    {
        bool gravityOrb = MageSpecConfig.IsGravityOrb(branch, tier);
        int fireTier = MageSpecConfig.FireTier(branch, tier);
        int facing = mover != null ? mover.Facing : 1;

        // 원본: aimX=우-좌, aimY=아래-위(화면좌표). Unity는 Y+가 위라 아래-위 항을 뒤집어서
        // "위 화살표=+Y"가 되게 맞췄다(원본과 시각적으로 동일한 결과).
        float aimX = (GameInput.Right ? 1 : 0) - (GameInput.Left ? 1 : 0);
        float aimY = (GameInput.Up ? 1 : 0) - (GameInput.Down ? 1 : 0);

        // 원본(project_test.html:1951-1954) — 중력 계열에서 방향키 없이 "아래"만 누르면(우리 좌표계는
        // 아래=aimY<0), 발사와 별개로 현재 위치 바로 아래에 즉시 중력점을 하나 터뜨린다.
        if (gravityOrb && aimY < 0f && aimX == 0f)
            MageSkillEffects.DetonateGravityOrb(transform.position + new Vector3(0f, -0.34f, 0f), tier, chargeK, facing);

        if (aimX == 0f && aimY == 0f) aimX = facing;
        Vector2 aim = new Vector2(aimX, aimY).normalized;
        if (Mathf.Abs(aim.x) > 0.1f && mover != null) mover.SetFacing(aim.x > 0f ? 1 : -1); // 원본 :1959

        float dmg = MageSpecConfig.BowDamage(branch, tier, chargeK, chargeDmgMult, baseDamage);
        int pierce = MageSpecConfig.BowPierce(branch, tier, chargeK, basePierce, chargePierce);
        bool inf = MageSpecConfig.BowInfinitePierce(branch, tier, chargeK);
        float spd = projectileSpeed * (1f + 0.3f * chargeK);
        float life = MageSpecConfig.BowLife(branch, tier, chargeK, projectileRange, projectileSpeed);
        float sizeMul = MageSpecConfig.BowSize(branch, tier, chargeK);
        bool explosive = MageSpecConfig.BowExplosive(branch, tier);
        float explosionPower = MageSpecConfig.BowExplosionPower(chargeK);
        bool charged = chargeK >= 0.5f;

        Vector3 spawnPos = transform.position + new Vector3(aim.x >= 0 ? 0.26f : -0.26f, 0.36f, 0f);
        MageProjectile.Spawn(spawnPos, aim * spd, dmg, pierce, life, sizeMul, boltSprite, boltColor,
            inf, charged, explosive, explosionPower, gravityOrb, chargeK, tier, transform.position, facing);
    }

    /// <summary>원본 `tryMageSkill()`(project_test.html:2164) — X 스킬.</summary>
    void TryUseSkill(PlayerProfile profile, MageBranch branch, int tier)
    {
        if (branch == MageBranch.None || tier < 1) return; // 원본: "빌드 1층부터 X 스킬 사용 가능"(:2169)

        int facing = mover != null ? mover.Facing : 1;
        if (branch == MageBranch.Explosion)
        {
            FireTeleportDash(tier, facing);
        }
        else
        {
            Vector2? nearest = FindNearestEnemyPosition();
            MageSkillEffects.GravityCollapse(tier, transform.position, facing, nearest);
        }
        // 원본 `p.ultCdMax = ... * cdMult()`(:2177) — 궁극기 쿨타임에도 '시간의 조각'이 붙는다.
        ultCdTimer = MageSpecConfig.UltCooldown(branch, tier)
                     * PlayerStatCalculator.ComputeCooldownMultiplier(ProfileService.Current); // 원본 :2177
    }

    /// <summary>원본 `mageFireTeleport()`(project_test.html:2132) — 방향키 방향(없으면 정면)으로
    /// 순간이동하고, 지나온 경로에 불길 장판을 남긴다.</summary>
    void FireTeleportDash(int tier, int facing)
    {
        if (mover == null) return;
        Vector3 fromPos = transform.position;

        float dx = (GameInput.Right ? 1 : 0) - (GameInput.Left ? 1 : 0);
        float dy = (GameInput.Up ? 1 : 0) - (GameInput.Down ? 1 : 0);
        if (dx == 0f && dy == 0f) dx = facing;
        Vector2 dir = new Vector2(dx, dy).normalized;
        if (Mathf.Abs(dir.x) > 0.1f) mover.SetFacing(dir.x > 0f ? 1 : -1);

        Vector3 toPos = fromPos + new Vector3(dir.x * MageSpecConfig.TeleportDistanceX, dir.y * MageSpecConfig.TeleportDistanceY, 0f);
        toPos.x = Mathf.Clamp(toPos.x, FieldBounds.MinX, FieldBounds.MaxX);
        // 원본은 착지 지형까지 정밀 계산(groundYBelow)해서 발판 위에 정확히 세우지만, 우리는 실제
        // Physics2D 콜라이더가 있어 순간이동 직후 겹침이 생겨도 물리가 바로 풀어준다 — Y는 바닥 아래로만
        // 안 내려가게 최소한만 막는다.
        toPos.y = Mathf.Max(toPos.y, FieldBounds.GroundY);

        mover.Teleport(toPos);
        health?.GrantInvuln(MageSpecConfig.TeleportInvuln); // 원본 :2149

        int trailPoints = MageSpecConfig.TeleportTrailPoints(tier);
        for (int i = 0; i <= trailPoints; i++)
        {
            float t = trailPoints <= 0 ? 1f : (float)i / trailPoints;
            FireTrailZone.Spawn(Vector3.Lerp(fromPos, toPos, t), tier, MageSpecConfig.TeleportTrailPower);
        }
    }

    /// <summary>원본 `nearestEnemy()`(project_test.html:2155) — 대붕괴가 중력점이 하나도 없을 때 쓸 대체 좌표.</summary>
    Vector2? FindNearestEnemyPosition()
    {
        Vector2? best = null;
        float bestDist = nearestEnemySearchRadius;
        foreach (var col in Physics2D.OverlapCircleAll(transform.position, nearestEnemySearchRadius))
        {
            if (!col.CompareTag("Enemy")) continue;
            var protectable = col.GetComponent<ISpawnProtectable>();
            if (protectable != null && protectable.IsSpawnProtected) continue;
            var dmgable = col.GetComponent<IDamageable>();
            if (dmgable != null && dmgable.IsDead) continue;

            float d = Vector2.Distance(transform.position, col.transform.position);
            if (d < bestDist) { bestDist = d; best = col.transform.position; }
        }
        return best;
    }
}
}
