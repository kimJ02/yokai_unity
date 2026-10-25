using UnityEngine;
using YokaiFront.Combat;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 마법사 차지 마법진 — 2026-10-25 사용자 "arcane_front_loop = 블랙홀을 쏠 때 시전자 앞에 나타날 마법진, fire_front_loop = 폭발 계열 마법진.
    /// 차징할수록 마법진이 커지고, 차징이 다 되면 커지는 게 멈춘다". Willosuna "Magic Circles VFX Lite"(16프레임 루프, 96px).
    /// 차지 중(<see cref="MageAttack.IsCharging"/>)에만 시전자 앞(바라보는 쪽)에 뜨고, 크기는 차지율에 따라 <see cref="MinScale"/> → 1배로 커지다가
    /// 차지율 100%에서 멈춘다. 쏘거나 취소하면 사라진다. 갈래가 없는 기본 마법사에겐 안 나온다. 그림만이다.
    /// </summary>
    [RequireComponent(typeof(MageAttack))]
    public class MageChargeCircle : MonoBehaviour
    {
        public const float Fps = 12f;
        /// <summary>차지 시작 때 크기(최대의 몇 배).</summary>
        public const float MinScale = 0.25f;
        /// <summary>최대 크기 배율 — 96px 프레임 = 0.75유닛이라 이 배율이면 지름 약 1.5유닛.</summary>
        public const float MaxScale = 2f;
        /// <summary>캐릭터 중심에서 바라보는 쪽으로 · 위로 나가는 거리(유닛).</summary>
        public static readonly Vector2 Offset = new Vector2(0.85f, 0.1f);
        const int SortingOrder = 4;

        MageAttack mage;
        CharacterMover2D mover;
        SpriteRenderer sr;
        Sprite[] arcane, fire;
        float clock;

        /// <summary>차지율 k(0~1)일 때 크기 배율 — 0.25배에서 시작해 1(=최대)에서 멈춘다.</summary>
        public static float ScaleAt(float k) => Mathf.Lerp(MinScale, 1f, Mathf.Clamp01(k)) * MaxScale;

        /// <summary>그 갈래에서 쓸 마법진 — 블랙홀(중력)은 arcane, 폭발은 fire, 그 밖엔 없음(null).</summary>
        public static string CircleFor(MageBranch branch, int tier)
        {
            if (MageSpecConfig.IsGravityOrb(branch, tier)) return "Arcane";
            if (MageSpecConfig.FireTier(branch, tier) > 0) return "Fire";
            return null;
        }

        /// <summary>지금 보이는지(테스트용).</summary>
        public bool Visible => sr != null && sr.enabled;

        void Awake()
        {
            mage = GetComponent<MageAttack>();
            mover = GetComponent<CharacterMover2D>();
            var go = new GameObject("MageChargeCircle");
            go.transform.SetParent(transform, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortingOrder;
            sr.enabled = false;
        }

        // MageAttack.Update가 차지를 끝낸 뒤의 상태를 읽도록 LateUpdate에서 — 쏜 프레임에 한 프레임 더 남지 않게.
        void LateUpdate() => Refresh();

        /// <summary>지금 차지 상태에 맞춰 보이기·크기·그림을 갱신한다(테스트가 직접 부른다).</summary>
        public void Refresh()
        {
            var profile = ProfileService.Current;
            string kind = CircleFor(profile.mageBranch, profile.mageTier);
            if (kind == null || !mage.IsCharging || profile.character != CharacterId.Mage)
            {
                sr.enabled = false;
                clock = 0f;
                return;
            }
            var frames = kind == "Arcane" ? (arcane ??= Load("Arcane")) : (fire ??= Load("Fire"));
            if (frames.Length == 0) return;
            clock += Time.deltaTime;
            sr.sprite = frames[Mathf.FloorToInt(clock * Fps) % frames.Length];
            sr.enabled = true;

            // 부모(캐릭터)가 크기를 가졌을 수 있어 월드 값으로 놓는다.
            int facing = mover != null ? mover.Facing : 1;
            sr.transform.position = transform.position + new Vector3(facing * Offset.x, Offset.y, 0f);
            float s = ScaleAt(mage.ChargeRatio);
            var parentScale = transform.lossyScale;
            sr.transform.localScale = new Vector3(s / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)), s / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        }

        static Sprite[] Load(string kind)
        {
            var all = Resources.LoadAll<Sprite>("Art/Figma/Vfx/MagicCircle/" + kind);
            System.Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name));
            return all;
        }
    }
}
