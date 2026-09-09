using UnityEngine;
using YokaiFront.Core;
using YokaiFront.Systems;

namespace YokaiFront.UI
{
    /// <summary>
    /// 결과 화면과 일시정지 화면 — 원본은 이 둘이 **같은 `#result` 패널**이다(project_test.html:539).
    /// `pauseRun()`이 제목을 "일시정지"로 바꾸고 "게임으로 돌아가기" 버튼을 켜는 식이라(`:4332`),
    /// 여기서도 한 컴포넌트가 두 상태를 그린다.
    ///
    /// 버튼은 판단을 하지 않는다 — 전부 <see cref="RunController"/>의 공개 메서드를 부른다(UI 층 규칙).
    /// </summary>
    [DisallowMultipleComponent]
    public class ResultScreen : MonoBehaviour
    {
        RunController run;

        void Awake() => run = Object.FindFirstObjectByType<RunController>();

        void OnGUI()
        {
            var scene = GameState.Current;
            if (scene != GameScene.Pause && scene != GameScene.Result) return;
            if (run == null) run = Object.FindFirstObjectByType<RunController>();

            const float w = 420f, h = 260f;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h), GUI.skin.box);
            GUILayout.Space(8f);

            if (scene == GameScene.Pause) DrawPause();
            else DrawResult();

            GUILayout.EndArea();
        }

        /// <summary>원본 `pauseRun()`이 채우는 내용(project_test.html:4337~4341).</summary>
        void DrawPause()
        {
            GUILayout.Label("<size=20><b>일시정지</b></size>", Rich());
            GUILayout.Space(6f);
            GUILayout.Label($"현재 지역   {RunState.Region}지역 · {RegionConfig.NameOf(RunState.Region)}");
            GUILayout.Label($"이번 사냥   {RunState.Kills}마리");
            GUILayout.Label($"남은 시간   {Mathf.CeilToInt(RunState.TimeLeft)}초");
            GUILayout.Space(12f);

            if (GUILayout.Button("게임으로 돌아가기 (ESC)", GUILayout.Height(30f))) run?.Resume();
            if (GUILayout.Button("로비로 돌아가기", GUILayout.Height(30f))) run?.EnterLobby();
        }

        /// <summary>원본 `endRun(reason)`이 채우는 내용(project_test.html:4358~4377).</summary>
        void DrawResult()
        {
            string title = run == null ? "귀환" : run.LastEndReason switch
            {
                RunEndReason.Dead => "斬 · 敗  —  쓰러졌다",
                RunEndReason.BossDead => "討伐 完了  —  보스 격파!",
                _ => "시간 종료",
            };

            GUILayout.Label($"<size=20><b>{title}</b></size>", Rich());
            GUILayout.Space(6f);
            GUILayout.Label($"처치한 요괴   {RunState.Kills}마리");
            GUILayout.Label($"획득 골드     +{RunState.GoldEarned} G");
            GUILayout.Label($"획득 경험치   +{RunState.ExpEarned}");

            if (run != null && run.LastEndReason == RunEndReason.Dead)
            {
                GUILayout.Space(4f);
                // 원본 결과 화면의 안내 문구 그대로(:4376) — 죽어도 재화는 남는다.
                GUILayout.Label("죽어도 획득한 재화는 유지된다. 강화하고 다시 도전하자.");
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("로비로 돌아가기", GUILayout.Height(30f))) run?.EnterLobby();
        }

        static GUIStyle rich;
        static GUIStyle Rich()
        {
            if (rich == null) rich = new GUIStyle(GUI.skin.label) { richText = true };
            return rich;
        }
    }
}
