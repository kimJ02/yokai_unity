using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 메카닉(원본 무기 "gunner") **0차 기본공격** — 원본 `gunnerFire()`(project_test.html:2188)의
    /// `laserTier == 0` 분기를 그대로 옮겼다. Z를 누르고 있으면 쿨다운마다 총알이 정면으로 나간다
    /// (마법사처럼 차지하지 않고, 위/아래 조준도 없다 — `vx: p.facing * spd, vy: 0`).
    ///
    /// **0차에 없는 것**(전부 전문화 티어가 있어야 열린다 — `docs/worksplit.md` "0차" 정의):
    /// - X 스킬(레이저 빔 / 설치기·드론)
    /// - 레이저 강화(관통 증가·유도·탄속 변화)
    /// - **스택 축적조차 안 된다**: 원본 `addGunnerStack`이 `if (!s.branch) return 0`으로
    ///   갈래를 안 고른 상태에선 즉시 빠져나간다(`:1414`). 그래서 0차엔 스택 관련 코드가 아예 없다.
    ///
    /// 이동은 마법사와 같은 즉시-속도 방식(원본 `updatePlayerCommon`의 bow/gunner 공통 분기).
    /// </summary>
    public class GunnerAttack : MonoBehaviour, ICharacterKit
    {
        [Header("원본 CONFIG/gunnerFire 그대로 (거리·속도는 100px=1유닛 축척)")]
        [Tooltip("표시용 현재 쿨다운. 매 프레임 BaseCooldownConst ÷ 공격속도 배수로 다시 계산된다.")]
        public float cooldown = 0.24f;
        [Tooltip("표시용 현재 피해량. statAtk × 0.82로 매 프레임 재계산된다.")]
        public float baseDamage = 8.2f;

        const float BaseCooldownConst = 0.24f;   // 원본 laserTier 0일 때의 cd(project_test.html:2196)
        const float DamageMult = 0.82f;          // 원본 dmg(:2203)
        const float BulletSpeed = 13.2f;         // 원본 spd 1320 ÷100(:2192)
        const float BulletLife = 0.62f;          // 원본 life(:2202)
        const int BulletPierce = 1;              // 원본 pierceLeft(:2204)
        const float BulletSize = 1f;             // 원본 size(:2205)
        const float MuzzleForward = 0.30f;       // 원본 x: p.x + facing*30(:2199)
        const float MuzzleHeight = 0.38f;        // 원본 y: p.y - 38 — 원본 Y+가 아래라 부호를 뒤집었다

        public Sprite bulletSprite;              // 런타임 AssetDatabase 호출을 피하려고 씬 빌더가 꽂아준다
        public Color bulletColor = new Color(1f, 0.92f, 0.63f);

        CharacterMover2D mover;
        float cdTimer;

        public CharacterId Character => CharacterId.Gunner;
        /// <summary>메카닉은 마법사와 같은 즉시-속도 이동(관성은 섬영 전용).</summary>
        public CharacterMover2D.MoveMode RequiredMoveMode => CharacterMover2D.MoveMode.Instant;

        public void OnSelected() => cdTimer = 0f;
        public void OnDeselected() { }

        void Awake() => mover = GetComponent<CharacterMover2D>();

        void Update()
        {
            // 원본 statAtk()/statAs()가 호출마다 다시 계산되는 것과 같은 방식 — 필드 자기 자신이 아니라
            // 항상 프로필+상수에서 새로 굴리므로 매 프레임 곱해져 누적되는 버그가 없다.
            var profile = ProfileService.Current;
            baseDamage = PlayerStatCalculator.ComputeAtk(profile) * DamageMult;
            cooldown = BaseCooldownConst / PlayerStatCalculator.ComputeAttackSpeedMultiplier(profile);

            cdTimer -= Time.deltaTime;

            // 원본(:3538): `if (!updateGunnerBeam(...) && !p.attacking && p.atkCds.gunner <= 0 && (held || justAtk)) gunnerFire();`
            // 0차엔 빔이 없어(`updateGunnerBeam`이 tier<=0에서 false) 누르고 있는 동안 쿨다운마다 연사된다.
            if (GameInput.AttackHeld && cdTimer <= 0f)
            {
                Fire();
                cdTimer = cooldown;
            }
        }

        void Fire()
        {
            int facing = mover != null ? mover.Facing : 1;
            Vector3 spawnPos = transform.position + new Vector3(facing * MuzzleForward, MuzzleHeight, 0f);
            Vector2 velocity = new Vector2(facing * BulletSpeed, 0f);

            GunnerBullet.Spawn(spawnPos, velocity, baseDamage, BulletPierce, BulletLife,
                BulletSize, bulletSprite, bulletColor);
        }
    }
}
