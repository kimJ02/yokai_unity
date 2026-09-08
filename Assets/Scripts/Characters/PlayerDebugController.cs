using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// 스프린트 2("성장곡선 검증") 임시 조작·표시 — `docs/sprint2-handoff-split.md` 확정 사항 그대로.
    /// 정식 강화 UI는 스프린트 3+(HUD `UI` 도메인 신설) 몫이라, 그 전까지 숫자키로 사고 화면 구석에
    /// 최소한의 수치(골드/레벨/강화 단계/다음 비용)를 띄운다 — 표시가 없으면 "체감 벽" 검증 자체가
    /// 눈으로 안 보이는 상태로 진행돼서 원안(로그만)에 없던 걸 추가한 것.
    /// </summary>
    public class PlayerDebugController : MonoBehaviour
    {
        static readonly (KeyCode key, UpgradeStat stat, string label)[] Bindings =
        {
            (KeyCode.Alpha1, UpgradeStat.Atk, "공격력"),
            (KeyCode.Alpha2, UpgradeStat.Hp, "체력"),
            (KeyCode.Alpha3, UpgradeStat.Ms, "이동속도"),
            (KeyCode.Alpha4, UpgradeStat.AtkSpeed, "공격속도"),
            (KeyCode.Alpha5, UpgradeStat.Crit, "치명타"),
        };

        void Update()
        {
            var profile = ProfileService.Current;
            foreach (var (key, stat, _) in Bindings)
                if (Input.GetKeyDown(key)) profile.TryBuyUpgrade(stat);
        }

        void OnGUI()
        {
            var p = ProfileService.Current;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };

            GUI.Label(new Rect(10, 10, 500, 22),
                $"골드 {p.gold}   레벨 {p.level} (EXP {p.exp}/{PlayerProfile.RequiredExp(p.level)})", style);

            var levels = new System.Text.StringBuilder();
            var costs = new System.Text.StringBuilder();
            foreach (var (key, stat, label) in Bindings)
            {
                int lv = p.GetUpgradeLevel(stat);
                levels.Append($"[{(int)key - (int)KeyCode.Alpha0}]{label} Lv{lv}  ");
                costs.Append($"{label}{GoldUpgradeConfig.Cost(stat, lv)}  ");
            }
            GUI.Label(new Rect(10, 32, 700, 22), levels.ToString(), style);
            GUI.Label(new Rect(10, 54, 700, 22), $"다음 비용 — {costs}", style);
        }
    }
}
