using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
/// <summary>
/// 마법사(원본 무기 "bow"="마법 지팡이") 기본 공격 — 원본 `bowFire()`/`CONFIG.bow` 그대로 이식.
/// Z를 누르고 있으면 차지, 떼면 그 시점 차지율(chargeK = chargeT/chargeMax)로 마법탄 하나를 발사한다.
/// 차지할수록: 데미지↑(dmg = baseDmg*(1+chargeDmg*chargeK)), 관통↑(pierce = base+floor(chargeK*chargePierce)),
/// 탄속↑(speed*(1+0.3*chargeK)) — 전부 원본 수치 그대로, 사거리(range)만큼의 수명 후 소멸.
/// 스킬트리(fireTier/frostTier)·중력구슬 분기는 이번 스프린트 범위 밖(HANDOFF.md 3번=버튼 1개)이라 뺐다.
/// PlayerAttack(범용 근접 판정)을 대체한다 — 이 캐릭터를 쓰는 동안은 씬에 둘 다 안 붙인다.
///
/// 개정(2026-08-26): 원본은 차지 중(`p.charging`) 이동속도가 절반(`chargeSlow = p.charging ? 0.5 : 1`,
/// `p.vx = mx * moveSpeed * ... * chargeSlow`)이다 — 완전히 빠져 있었다. `CharacterMover2D.SpeedMultiplier`를
/// 매 프레임 갱신해서 이식.
/// </summary>
public class MageAttack : MonoBehaviour
{
    [Header("원본 CONFIG.bow 그대로 (거리·속도는 100px=1유닛 축척)")]
    // 스프린트 2부터 cooldown/baseDamage는 매 프레임 Core.PlayerStatCalculator에서 다시 계산되는
    // "표시용 현재값"이다 — 인스펙터에서 수정해도 다음 프레임에 덮어써진다(BaseCooldownConst만 진짜 기준값).
    public float cooldown = 0.5f;          // B.cd ÷ 공격속도 배수(statAs, project_test.html:1942)
    public float baseDamage = 9f;          // statAtk() × B.dmg(0.9) — 골드 강화(atk)가 반영됨
    const float BaseCooldownConst = 0.5f;  // 원본 B.cd(project_test.html:609) — 이 값만 고정 기준
    public float chargeMax = 1.0f;         // B.chargeMax
    public float chargeDmgMult = 1.6f;     // B.chargeDmg
    public int basePierce = 2;             // B.pierce
    public int chargePierce = 4;           // B.chargePierce
    public float projectileSpeed = 10.8f;  // B.speed 1080px/s
    public float projectileRange = 8.8f;   // B.range 880px
    public float chargeMoveSlow = 0.5f;    // 원본 chargeSlow

    public Sprite boltSprite;              // 런타임 AssetDatabase 호출을 피하려고 씬 빌더가 미리 꽂아준다
    public Color boltColor = new Color(0.55f, 0.8f, 1f);

    CharacterMover2D mover;
    float cdTimer;
    bool charging;
    float chargeT;
    SpriteRenderer chargeIndicator;

    void Awake()
    {
        mover = GetComponent<CharacterMover2D>();

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
        // 골드 강화가 반영된 파생 스탯을 매 프레임 새로 계산(원본 statAtk()/statAs()가 호출마다
        // 다시 계산되는 함수인 것과 동일한 방식) — 필드 자기 자신이 아니라 항상 프로필+상수에서
        // 새로 굴리므로 매 프레임 곱해지는 식으로 값이 누적(compounding)되는 버그가 없다.
        var profile = ProfileService.Current;
        baseDamage = PlayerStatCalculator.ComputeAtk(profile) * PlayerStatCalculator.BowDamageMult;
        cooldown = BaseCooldownConst / PlayerStatCalculator.ComputeAttackSpeedMultiplier(profile); // 원본 :1942

        // 이번 프레임 시작 시점(직전 프레임까지의) 차지 상태를 기준으로 감속을 먼저 적용한다 —
        // 발사(release)되는 바로 그 프레임도 "떼기 직전까지는 차지 중"이었으므로 감속이 맞다.
        // 아래에서 곧바로 charging이 꺼질 수 있어(발사 처리) 순서를 이렇게 잡아야 한다.
        if (mover != null) mover.SpeedMultiplier = charging ? chargeMoveSlow : 1f;

        cdTimer -= Time.deltaTime;

        bool held = Input.GetKey(KeyCode.Z);
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
                Fire(k);
                charging = false;
                chargeIndicator.gameObject.SetActive(false);
                cdTimer = cooldown;
            }
        }
    }

    void Fire(float chargeK)
    {
        // 원본: aimX=우-좌, aimY=아래-위(화면좌표). Unity는 Y+가 위라 아래-위 항을 뒤집어서
        // "위 화살표=+Y"가 되게 맞췄다(원본과 시각적으로 동일한 결과).
        float aimX = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        float aimY = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        if (aimX == 0f && aimY == 0f) aimX = mover != null ? mover.Facing : 1;

        Vector2 aim = new Vector2(aimX, aimY).normalized;

        float dmg = baseDamage * (1f + chargeDmgMult * chargeK);
        int pierce = basePierce + Mathf.FloorToInt(chargeK * chargePierce);
        float spd = projectileSpeed * (1f + 0.3f * chargeK);
        float life = projectileRange / projectileSpeed; // 원본과 동일하게 차지와 무관한 고정 수명
        float sizeMul = 1f + chargeK * 0.9f; // 원본 `size: 1 + chargeK*0.9` — 판정/시각 크기 배율

        Vector3 spawnPos = transform.position + new Vector3(aim.x >= 0 ? 0.26f : -0.26f, 0.36f, 0f);
        // 마지막 인자(시전자)는 넉백 방향 기준 — 원본이 `sign(e.x - player.x)`로 플레이어 위치를 쓰기 때문.
        MageProjectile.Spawn(spawnPos, aim * spd, dmg, pierce, life, sizeMul, boltSprite, boltColor, gameObject);
    }
}
}
