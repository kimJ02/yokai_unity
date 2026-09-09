using UnityEngine;

namespace YokaiFront.Characters
{
    /// <summary>
    /// ⚠️ **삭제 예정 — 원본에 없는 임시방편이다.** 런 사이클(현재 스프린트, `HANDOFF.md` 7번)을
    /// 구현하면서 이 파일을 지우고, 사망은 `endRun('dead')` → 결과 화면 → 로비로 처리한다.
    /// **"원본에 있는 기능"으로 오해해서 유지하지 말 것.**
    ///
    /// 원본은 죽으면 런이 끝난다(project_test.html:1927 `endRun('dead')`). 부활은 아이템
    /// '최후의 발악'을 가졌을 때 체력 40%로 **자동** 발동하는 것뿐이고(`:1917`), R키 같은 수동 부활은 없다.
    /// 이 클래스는 런 사이클이 없던 시절(성장곡선 검증 스프린트, `docs/sprints/03-growth-curve-worksplit.md`)
    /// "죽을 때마다 에디터를 재시작해야 하면 체감 벽 검증이 불가능하다"는 이유로 넣은 것이다 —
    /// 2026-09-08 사용자 지적으로 폐기 확정.
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
