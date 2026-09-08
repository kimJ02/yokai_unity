using UnityEngine;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 스프린트 2 임시 사망 처리 — "정지 + R키로 그 자리에서 재시작"(`docs/sprint2-handoff-split.md`
    /// 확정 사항). 원본은 죽으면 런 종료+결과화면(project_test.html:1927 `endRun('dead')`)인데,
    /// 그 런 사이클 자체가 스프린트 3 범위라 아직 없다. 죽을 때마다 에디터를 재시작해야 하면
    /// "5지역쯤 체감 벽" 검증이라는 이번 스프린트의 목적 자체가 방해받아서 최소한만 만든다.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        PlayerHealth health;
        CharacterMover2D mover;
        MageAttack mage;
        bool dead;

        public bool IsDead => dead;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mover = GetComponent<CharacterMover2D>();
            mage = GetComponent<MageAttack>();
            health.Died += HandleDied;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        void HandleDied()
        {
            dead = true;
            if (mover != null) mover.enabled = false; // 정지 — 원본엔 없는 "테스트 편의" 구간(위 클래스 주석 참고)
            if (mage != null) mage.enabled = false;
        }

        void Update()
        {
            if (dead && Input.GetKeyDown(KeyCode.R)) Revive();
        }

        /// <summary>R키 처리 본체 — Input 시뮬레이션이 안 되는 PlayMode 테스트가 직접 호출할 수 있게
        /// public으로 분리했다(이 프로젝트에서 실제 키 입력이 필요한 로직을 테스트할 때 쓰는 패턴).</summary>
        public void Revive()
        {
            health.Revive();
            if (mover != null) mover.enabled = true;
            if (mage != null) mage.enabled = true;
            dead = false;
        }
    }
}
