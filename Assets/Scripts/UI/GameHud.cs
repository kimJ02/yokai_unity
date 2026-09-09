using UnityEngine;
using YokaiFront.Characters;
using YokaiFront.Core;

namespace YokaiFront.UI
{
    /// <summary>
    /// 사냥 중 HUD — 원본 `#hud`(project_test.html:487)와 `syncHUD()`(`:6269` 부근)에 대응한다.
    /// 체력·남은 시간·지역·처치 수·골드만 보여준다(콤보·버프·보스 바는 그 시스템이 아직 없다).
    ///
    /// 일시정지·결과 화면에서도 계속 그린다 — 원본도 `#hud`를 결과 화면에서만 숨긴다.
    /// 여기서는 <see cref="GameScene.Lobby"/>일 때만 숨긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHud : MonoBehaviour
    {
        PlayerHealth health;

        void Awake() => FindPlayer();

        void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) health = player.GetComponent<PlayerHealth>();
        }

        void OnGUI()
        {
            if (GameState.Current == GameScene.Lobby) return;
            if (health == null) FindPlayer();

            var profile = ProfileService.Current;
            GUILayout.BeginArea(new Rect(16f, 12f, 460f, 130f));

            if (health != null)
                GUILayout.Label($"체력  {Mathf.CeilToInt(health.CurrentHp)} / {Mathf.CeilToInt(health.MaxHp)}");

            GUILayout.Label($"{RunState.Region}지역 · {RegionConfig.NameOf(RunState.Region)}" +
                            $"    남은 시간 {Mathf.CeilToInt(RunState.TimeLeft)}초");
            GUILayout.Label($"처치 {RunState.Kills}    골드 {profile.gold} G    Lv.{profile.level}");
            GUILayout.Label("ESC — 일시정지 / 로비로 나가기");

            GUILayout.EndArea();
        }
    }
}
