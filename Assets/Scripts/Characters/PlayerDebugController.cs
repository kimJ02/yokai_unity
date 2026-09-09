using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Characters
{
    /// <summary>
    /// ⚠️ **삭제 예정 — 원본에 없는 임시방편이다.** 원본은 강화·전문화를 **로비 탭**에서 한다
    /// (project_test.html:6473 강화탭 / `:6971` 전문화탭). 인게임 숫자키로 사는 건 로비 UI가 없어서
    /// 넣은 대체물이고, 로비 UI(`docs/worksplit.md` 내 담당 2번)가 생기면 이 파일을 지운다.
    /// **"원본에 있는 조작"으로 오해해서 유지하지 말 것.**
    ///
    /// 지금 남겨두는 이유: 강화·빌드를 살 다른 수단이 아직 없어서, 지금 지우면 검증이 막힌다.
    /// 그래서 런 사이클 스프린트에서는 남기고 로비 UI 스프린트에서 지운다(`HANDOFF.md` 7번).
    /// 조작: 1~5 골드 강화 5종, 6/7 마법사 빌드(폭발/중력) 티어 습득.
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
        PlayerRig rig;

        void Awake()
        {
            rig = GetComponent<PlayerRig>();

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

            // Tab = 다음 캐릭터로 전환. 원본은 로비의 캐릭터 선택 탭이라 이것도 임시 조작이다.
            // 아직 키트가 붙지 않은 캐릭터(섬영·드루이드)는 자동으로 건너뛴다.
            if (Input.GetKeyDown(KeyCode.Tab) && rig != null) rig.SelectNext();
        }

        void OnGUI()
        {
            var p = ProfileService.Current;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };

            var panel = new Rect(6, 6, 760, 122);
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

            string charLabel = p.character switch
            {
                CharacterId.Mage => "마법사",
                CharacterId.Gunner => "메카닉",
                CharacterId.Blade => "섬영",
                CharacterId.Druid => "드루이드",
                _ => p.character.ToString(),
            };
            GUI.Label(new Rect(12, 98, 740, 22), $"[Tab] 캐릭터 전환 — 현재: {charLabel}", style);
        }
    }
}
