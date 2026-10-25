using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.UI
{
    /// <summary>
    /// 적이 타격을 받을 때 나는 소리 — 2026-10-25 사용자가 받은 Kenney Impact Sounds의 `footstep_carpet_002`.
    /// 직접 타격마다(`CombatEvents.EnemyDamaged` — 화상 틱 같은 지속 피해는 안 부른다) 한 번. 소리 크기는 설정 창의 효과음 값.
    /// 한 프레임에 여러 마리를 맞춰도 시끄럽지 않게 짧은 간격(<see cref="MinGap"/>) 안의 소리는 건너뛰고, 음높이를 조금씩 흔든다.
    /// 판(`PixelCanvas`)에 붙어 게임 씬이 열릴 때 같이 생긴다.
    /// </summary>
    public class HitSound : MonoBehaviour
    {
        public const string ClipPath = "Audio/Sfx/enemy_hit";
        /// <summary>이 시간(초)보다 가까운 소리는 건너뛴다.</summary>
        public const float MinGap = 0.04f;
        const float BaseVolume = 0.7f;

        AudioSource source;
        AudioClip clip;
        float lastTime = -10f;

        /// <summary>지금까지 낸 소리 수(테스트용).</summary>
        public int Played { get; private set; }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            clip = Resources.Load<AudioClip>(ClipPath);
        }

        void OnEnable() => CombatEvents.EnemyDamaged += OnHit;
        void OnDisable() => CombatEvents.EnemyDamaged -= OnHit;

        void OnHit(GameObject enemy) => Play();

        public void Play()
        {
            float now = Time.unscaledTime; // 히트스톱 중엔 timeScale이 0이다
            if (clip == null || now - lastTime < MinGap) return;
            float vol = BaseVolume * GameSettings.SfxVolume / GameSettings.VolumeMax;
            if (vol <= 0f) return;
            lastTime = now;
            source.pitch = Random.Range(0.92f, 1.08f);
            source.PlayOneShot(clip, vol);
            Played++;
        }
    }
}
