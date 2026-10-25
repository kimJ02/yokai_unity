using UnityEngine;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 마법사 차지 단계 깜빡임 — 2026-10-25 사용자 "법사는 차지 단계마다 플레이어가 깜빡거리게, 마법진은 없애".
    /// 차지가 한 단계 오를 때마다 캐릭터 몸 모양이 한 번 하얗게 번쩍인다(몹 피격 플래시와 같은 셰이더·같은 세기·같은 감쇠).
    ///
    /// 단계는 원본 `bowFire`가 실제로 가르는 문턱 그대로다: **50% = 강화탄**(`charged: chargeK >= 0.5` — 관통·폭발력·중첩 2)과
    /// **99% = 완충**(`full = chargeK >= 0.99` — 폭발력 최대·중첩 3), project_test.html:1944·:1968·:1973·:2047.
    /// 원본엔 이 깜빡임이 없다(원본의 차지 표시는 버프 줄 "차지 N%"뿐) — 사용자 결정으로 넣은 것.
    /// </summary>
    [RequireComponent(typeof(MageAttack))]
    public class MageChargeFlash : MonoBehaviour
    {
        /// <summary>깜빡이는 차지 문턱(차지율) — 원본 강화탄·완충.</summary>
        public static readonly float[] Stages = { 0.5f, 0.99f };
        /// <summary>한 번 번쩍임이 사라지는 시간(초) — 몹 피격 플래시(원본 `e.flash -= dt * 6`, 약 0.17초)와 같게.</summary>
        public const float Duration = 1f / 6f;
        /// <summary>번쩍임 세기 — 몹 피격 플래시(원본 `flash × 0.75`, :4799)와 같게.</summary>
        public const float Strength = 0.75f;

        static Material flashMaterial;
        MageAttack mage;
        SpriteRenderer body, flashSr;
        int stage;
        float flash;

        /// <summary>차지율 k에서 몇 단계인지 — 0 = 아직, 1 = 강화탄, 2 = 완충.</summary>
        public static int StageAt(float k)
        {
            int s = 0;
            foreach (float t in Stages) if (k >= t) s++;
            return s;
        }

        /// <summary>지금 번쩍임 세기 0~1(테스트용).</summary>
        public float FlashAmount => flash;

        void Awake()
        {
            mage = GetComponent<MageAttack>();
            body = GetComponent<SpriteRenderer>();
        }

        // MageAttack.Update가 차지율을 올린 뒤에 읽도록 LateUpdate에서.
        void LateUpdate() => Refresh(Time.deltaTime); // 히트스톱·일시정지 땐 0 — 번쩍임도 같이 멈춘다

        /// <summary>차지 단계가 올랐으면 번쩍이고, 번쩍임을 줄여 그린다(테스트가 직접 부른다).</summary>
        public void Refresh(float dt)
        {
            int now = mage != null && mage.IsCharging ? StageAt(mage.ChargeRatio) : 0;
            if (now > stage) flash = 1f;
            else if (flash > 0f) flash = Mathf.Max(0f, flash - dt / Duration);
            stage = now;
            Draw();
        }

        void Draw()
        {
            if (body == null) return;
            if (flashSr == null)
            {
                if (flash <= 0f) return;
                if (flashMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Shaders/SpriteFlash");
                    if (shader == null) return;
                    flashMaterial = new Material(shader) { name = "MageChargeFlash" };
                }
                var go = new GameObject("ChargeFlash");
                go.transform.SetParent(transform, false);
                flashSr = go.AddComponent<SpriteRenderer>();
                flashSr.sharedMaterial = flashMaterial;
            }
            flashSr.enabled = flash > 0f && body.sprite != null;
            if (!flashSr.enabled) return;
            flashSr.sprite = body.sprite;
            flashSr.flipX = body.flipX;
            flashSr.flipY = body.flipY;
            flashSr.sortingLayerID = body.sortingLayerID;
            flashSr.sortingOrder = body.sortingOrder + 1;
            flashSr.color = new Color(1f, 1f, 1f, flash * Strength);
        }
    }
}
