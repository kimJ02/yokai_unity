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

        Texture2D bgTexture;

        void Awake()
        {
            // 씬 배경색이 흰색(BuildPartAScene.cs `cam.backgroundColor = Color.white`)이라 흰 글씨가
            // 그대로 묻혀서 안 보였다(사용자가 직접 플레이해보고 지적) — 배경색과 무관하게 항상 읽히도록
            // 텍스트 뒤에 어두운 반투명 판을 깐다.
            bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
            bgTexture.Apply();
        }

        void OnDestroy()
        {
            if (bgTexture != null) Destroy(bgTexture);
        }

        void Update()
        {
            var profile = ProfileService.Current;
            foreach (var (key, stat, _) in Bindings)
                if (Input.GetKeyDown(key)) profile.TryBuyUpgrade(stat);

            // 마법사 스킬트리(전문화) 임시 조작 — 정식 UI(강화/전문화 탭)가 생기기 전까지.
            // 6=폭발 계열 다음 티어 습득, 7=중력 계열 다음 티어 습득(원본 learnSkill, project_test.html:7011).
            if (Input.GetKeyDown(KeyCode.Alpha6)) profile.TryLearnMageTier(MageBranch.Explosion);
            if (Input.GetKeyDown(KeyCode.Alpha7)) profile.TryLearnMageTier(MageBranch.Gravity);
        }

        void OnGUI()
        {
            var p = ProfileService.Current;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };

            var panel = new Rect(6, 6, 760, 100);
            GUI.DrawTexture(panel, bgTexture);

            GUI.Label(new Rect(12, 10, 740, 22),
                $"골드 {p.gold}   레벨 {p.level} (EXP {p.exp}/{PlayerProfile.RequiredExp(p.level)})   SP {p.SpAvailable}/{p.SpTotal}", style);

            var levels = new System.Text.StringBuilder();
            var costs = new System.Text.StringBuilder();
            foreach (var (key, stat, label) in Bindings)
            {
                int lv = p.GetUpgradeLevel(stat);
                levels.Append($"[{(int)key - (int)KeyCode.Alpha0}]{label} Lv{lv}  ");
                costs.Append($"{label}{GoldUpgradeConfig.Cost(stat, lv)}  ");
            }
            GUI.Label(new Rect(12, 32, 740, 22), levels.ToString(), style);
            GUI.Label(new Rect(12, 54, 740, 22), $"다음 비용 — {costs}", style);

            string branchLabel = p.mageBranch switch
            {
                MageBranch.Explosion => "폭발",
                MageBranch.Gravity => "중력",
                _ => "미선택",
            };
            GUI.Label(new Rect(12, 76, 740, 22), $"[6]폭발 [7]중력 — 마법사 빌드: {branchLabel} {p.mageTier}티어", style);
        }
    }
}
