using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 모든 키 입력은 여기를 통해서만 읽는다. 원본 `KEYMAP`(project_test.html:1007)의 액션 이름을
    /// 그대로 프로퍼티로 옮겼다 — CLAUDE.md "입력 처리" 규칙("스크립트에서 Input.GetKey(KeyCode.X)를
    /// 직접 호출하지 않는다")이 실제로는 안 지켜지고 있던 걸 발견해서(`CharacterMover2D`/`MageAttack`/
    /// `PlayerAttack` 세 곳이 직접 호출 중이었음) 여기로 전부 모았다.
    ///
    /// 원본 KEYMAP은 점프에 KeyC와 Space 둘 다 매핑하지만, 이 프로젝트는 **의도적으로 C만 쓴다**
    /// (`docs/sprints/01-combat-core.md`·`CLAUDE.md`에 명시된 의도적 편차 — 되돌리지 말 것).
    ///
    /// Unity의 `Input.GetKey`/`GetKeyDown` 자체가 이미 "누르고 있음/방금 눌림"을 프레임 단위로
    /// 구분해주므로, 원본처럼 별도 Set 기반 상태 관리를 재구현할 필요는 없다 — 여기서는 키 이름만
    /// 한 곳에 모으는 게 목적이다.
    /// </summary>
    public static class GameInput
    {
        // ---- 이동 (원본 left/right/up/down) ----
        public static bool Left => Input.GetKey(KeyCode.LeftArrow);
        public static bool Right => Input.GetKey(KeyCode.RightArrow);
        public static bool Up => Input.GetKey(KeyCode.UpArrow);
        public static bool Down => Input.GetKey(KeyCode.DownArrow);

        // ---- 점프 (원본 jump — 이 프로젝트는 C 전용, 의도적 편차) ----
        public static bool JumpHeld => Input.GetKey(KeyCode.C);
        public static bool JumpDown => Input.GetKeyDown(KeyCode.C);

        // ---- 공격 (원본 atkBow, 마법사 Z) ----
        public static bool AttackHeld => Input.GetKey(KeyCode.Z);
        public static bool AttackDown => Input.GetKeyDown(KeyCode.Z);

        // ---- 스킬 (원본 ult, X) ----
        public static bool UltDown => Input.GetKeyDown(KeyCode.X);

        // ---- 일시정지 (원본 :1026 — ESC로 run ↔ pause 토글) ----
        /// <summary>
        /// ESC. 원본은 사냥 중 ESC로 일시정지하고, 그 화면에서 "게임으로 돌아가기 / 로비로 돌아가기"를
        /// 고른다(`:1027`~`:1028`). **ESC 자체가 로비로 나가는 키가 아니다** — 한 단계 거친다.
        /// </summary>
        public static bool PauseDown => Input.GetKeyDown(KeyCode.Escape);
    }
}
