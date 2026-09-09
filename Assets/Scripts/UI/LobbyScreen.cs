using UnityEngine;
using YokaiFront.Characters;
using YokaiFront.Core;
using YokaiFront.Systems;

namespace YokaiFront.UI
{
    /// <summary>
    /// 로비 — 원본 `#lobby`(project_test.html:512)와 `renderLobby()`(`:6378`)에 대응한다.
    /// 탭 구성도 원본과 같다: **캐릭터 선택 / 스테이지 / 강화 / 전문화**
    /// (윤회·업적 탭은 요괴의 혼·업적 시스템이 아직 없어서 이번 범위 밖 — 아래 "빠진 탭" 참고).
    ///
    /// ## 왜 IMGUI(OnGUI)인가
    /// 이 프로젝트는 **렌더링·비주얼 전부가 명시적으로 범위 밖**이고(원형 스프라이트 유지, 사용자 확인),
    /// 지금 필요한 건 "로비로 나가서 캐릭터를 고르고 강화를 찍고 다시 들어간다"는 **동작**이다.
    /// uGUI로 만들면 같은 기능에 씬 빌더 코드가 수백 줄 붙는데, 그 코드는 나중에 실제 아트가 들어올 때
    /// 어차피 전부 갈린다. UI 층은 아래 층을 읽고 공개 메서드를 부르기만 하므로(README 참고)
    /// 나중에 uGUI로 바꿔도 **이 층 밖은 하나도 안 바뀐다.**
    ///
    /// ## 빠진 탭
    /// - **윤회 ☸** — 영구 재화(요괴의 혼)와 윤회 보너스 테이블이 아직 없다.
    /// - **업적 🏆** — 업적 판정/집계가 없다.
    /// 둘 다 원본에 있는 기능이고 폐기가 아니다. 해당 시스템을 만들 때 여기에 탭만 추가하면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LobbyScreen : MonoBehaviour
    {
        enum Tab { Character, Stage, Upgrade, Spec, Rebirth }

        static readonly string[] TabNames = { "캐릭터 선택", "스테이지", "강화", "전문화", "윤회 ☸" };

        Tab tab = Tab.Character;
        RunController run;
        PlayerRig rig;
        Vector2 scroll;
        string toast = "";
        float toastLeft;
        bool confirmingReset;
        bool confirmingRebirth;

        void Awake()
        {
            run = Object.FindFirstObjectByType<RunController>();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) rig = player.GetComponent<PlayerRig>();
        }

        void Update()
        {
            // 로비는 `Time.timeScale = 0`에서 돌아간다(RunController 설계) — 스케일된 시간은 흐르지 않는다.
            if (toastLeft > 0f) toastLeft -= Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            if (GameState.Current != GameScene.Lobby) return;
            if (run == null) run = Object.FindFirstObjectByType<RunController>();

            var profile = ProfileService.Current;
            GUILayout.BeginArea(new Rect(20f, 20f, Screen.width - 40f, Screen.height - 40f));

            DrawHeader(profile);
            DrawTabs();

            scroll = GUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case Tab.Character: DrawCharacterTab(profile); break;
                case Tab.Stage: DrawStageTab(profile); break;
                case Tab.Upgrade: DrawUpgradeTab(profile); break;
                case Tab.Spec: DrawSpecTab(profile); break;
                case Tab.Rebirth: DrawRebirthTab(profile); break;
            }
            GUILayout.EndScrollView();

            if (toastLeft > 0f)
            {
                GUILayout.Space(6f);
                GUILayout.Label(toast);
            }

            GUILayout.EndArea();
        }

        void DrawHeader(PlayerProfile profile)
        {
            GUILayout.Label("<size=22><b>요괴전선 — 로비</b></size>", RichLabel());
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"Lv.{profile.level}   경험치 {profile.exp} / {PlayerProfile.RequiredExp(profile.level)}   " +
                $"골드 {profile.gold} G   SP {profile.SpAvailable} (총 {profile.SpTotal})   " +
                $"윤회 {profile.rebirths}회   ☸ {profile.rp}");
            GUILayout.FlexibleSpace();

            // 원본 로비 헤더의 "전체 초기화"(project_test.html:524). 실수로 누르면 큰일이라 두 번 묻는다
            // — 원본은 confirm() 대화상자를 쓰는데 IMGUI엔 그게 없어서 버튼 상태로 대신한다.
            if (!confirmingReset)
            {
                if (GUILayout.Button("전체 초기화", GUILayout.Width(100f))) confirmingReset = true;
            }
            else
            {
                GUILayout.Label("정말 지울까?");
                if (GUILayout.Button("네, 지웁니다", GUILayout.Width(110f)))
                {
                    SaveService.DeleteSave();
                    confirmingReset = false;
                    ShowToast("전체 초기화 완료");
                }
                if (GUILayout.Button("취소", GUILayout.Width(60f))) confirmingReset = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        void DrawTabs()
        {
            int picked = GUILayout.Toolbar((int)tab, TabNames, GUILayout.Height(28f));
            if (picked != (int)tab) { tab = (Tab)picked; scroll = Vector2.zero; }
            GUILayout.Space(8f);
        }

        // ────────────────────────── 캐릭터 선택 ──────────────────────────

        /// <summary>
        /// 원본 `renderCharacterSelect()`(project_test.html:6389 경로). 고르면 곧바로
        /// <see cref="PlayerRig.Select"/>로 키트가 교체된다 — **여기서 게임 상태를 직접 만지지 않는다.**
        /// </summary>
        void DrawCharacterTab(PlayerProfile profile)
        {
            GUILayout.Label("이 캐릭터로 사냥에 나간다. 캐릭터마다 이동 방식과 Z 기본공격이 다르다.");
            GUILayout.Space(6f);

            foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
            {
                bool implemented = rig != null && rig.HasKit(id);
                bool current = profile.character == id;

                GUILayout.BeginHorizontal();
                string label = CharacterLabel(id) + (current ? "   ← 선택됨" : "");
                if (!implemented) label += "   (미구현)";

                GUI.enabled = implemented && !current;
                if (GUILayout.Button(label, GUILayout.Height(30f), GUILayout.Width(360f)))
                {
                    if (rig.Select(id)) ShowToast($"{CharacterLabel(id)} 선택");
                }
                GUI.enabled = true;

                GUILayout.Label(CharacterHint(id));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label("섬영·드루이드는 팀원이 구현 중이라 아직 목록에 안 뜬다 — 키트를 붙이면 자동으로 나타난다.");
        }

        static string CharacterLabel(CharacterId id) => id switch
        {
            CharacterId.Mage => "마법사",
            CharacterId.Gunner => "메카닉",
            CharacterId.Blade => "섬영",
            CharacterId.Druid => "드루이드",
            _ => id.ToString(),
        };

        static string CharacterHint(CharacterId id) => id switch
        {
            CharacterId.Mage => "Z 차지 마법탄 · X 전문화 스킬",
            CharacterId.Gunner => "Z 자동 사격(누르고 있으면 연사)",
            CharacterId.Blade => "관성 이동 — 달릴수록 강해진다",
            CharacterId.Druid => "클로 3타 콤보 · 마나",
            _ => "",
        };

        // ────────────────────────── 스테이지 ──────────────────────────

        /// <summary>원본 `renderStageTab()`(project_test.html:6441).</summary>
        void DrawStageTab(PlayerProfile profile)
        {
            GUILayout.Label($"일반 사냥: 제한시간 {RunState.NormalTime:0}초 동안 요괴를 잡아 재화를 모은다. " +
                            "지역이 높을수록 강한 종이 나오고 몹 체력·피해가 지수로 오른다.");
            GUILayout.Space(6f);

            for (int r = 1; r <= RegionConfig.Count; r++)
            {
                bool open = profile.IsRegionOpen(r);
                int fee = RegionConfig.EntryFee(r);
                bool canPay = profile.gold >= fee;
                bool bossReady = profile.IsBossUnlocked(r);

                GUILayout.BeginHorizontal();

                GUI.enabled = open && canPay;
                if (GUILayout.Button($"{r}. {RegionConfig.NameOf(r)}", GUILayout.Height(28f), GUILayout.Width(200f)))
                    EnterRegion(profile, r, fee, RunMode.Normal);

                // 원본도 지역마다 일반/보스 버튼을 나란히 둔다(:6464). 보스는 토벌 100마리 뒤에 열린다.
                GUI.enabled = open && canPay && bossReady;
                if (GUILayout.Button($"👹 보스", GUILayout.Height(28f), GUILayout.Width(80f)))
                    EnterRegion(profile, r, fee, RunMode.Boss);
                GUI.enabled = true;

                string info = $"권장 Lv.{RegionConfig.RecommendedLevel(r)}+   입장료 {(fee == 0 ? "무료" : fee + " G")}" +
                              $"   토벌 {profile.RegionKills(r)} / {RunState.RegionKillTarget}";
                if (profile.IsBossCleared(r)) info += "   ✔ 격파";
                else if (bossReady) info += "   👹 보스 도전 가능";
                if (!open) info += "   🔒 이전 지역 보스를 잡아야 열린다";
                else if (!canPay) info += "   ← 골드 부족";
                GUILayout.Label(info);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label($"지역에서 {RunState.RegionKillTarget}마리를 토벌하면 보스가 열리고, 보스를 잡으면 다음 지역이 열린다.");
        }

        /// <summary>원본 `startRun`의 입장료 처리(project_test.html:4297~4303).</summary>
        void EnterRegion(PlayerProfile profile, int region, int fee, RunMode mode)
        {
            if (fee > 0)
            {
                if (profile.gold < fee) { ShowToast($"입장료 부족 — {fee} G 필요"); return; }
                profile.gold -= fee;
                ShowToast($"입장료 {fee} G 지불");
            }
            run?.StartRun(region, mode);
        }

        // ────────────────────────── 강화 ──────────────────────────

        /// <summary>
        /// 원본 `renderUpgradeTab()`(project_test.html:6974) — **이게 원래 강화가 있어야 할 자리**다.
        /// 인게임 숫자키 강화(`PlayerDebugController`)는 원본에 없는 임시방편이라 곧 삭제된다.
        /// </summary>
        void DrawUpgradeTab(PlayerProfile profile)
        {
            GUILayout.Label("골드로 영구 강화를 산다. **효과는 선형인데 비용은 지수로 오른다** — 원본 그대로다.");
            GUILayout.Space(6f);

            foreach (UpgradeStat stat in System.Enum.GetValues(typeof(UpgradeStat)))
            {
                int level = profile.GetUpgradeLevel(stat);
                int cost = GoldUpgradeConfig.Cost(stat, level);
                bool canBuy = profile.gold >= cost;

                GUILayout.BeginHorizontal();
                GUI.enabled = canBuy;
                if (GUILayout.Button($"{UpgradeLabel(stat)}  +{level}   →   {cost} G",
                                     GUILayout.Height(28f), GUILayout.Width(320f)))
                {
                    // 골드 차감과 단계 상승은 프로필이 원자적으로 처리한다(원본 `buyGoldUpgrade` :7042).
                    if (profile.TryBuyUpgrade(stat))
                    {
                        ShowToast($"{UpgradeLabel(stat)} +{level + 1}");
                        SaveService.Save(); // 원본도 구매 직후 `saveMeta()`(:7097)
                    }
                }
                GUI.enabled = true;
                GUILayout.Label(UpgradeHint(stat));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label("체력 강화는 **다음 사냥에 입장할 때** 반영된다 — 원본도 최대체력을 레벨업과 런 시작에만 " +
                            "다시 계산한다(:1850·:1515). 로비에서 산 즉시 최대체력이 오르지 않는 게 정상이다.");
        }

        static string UpgradeLabel(UpgradeStat stat) => stat switch
        {
            UpgradeStat.Atk => "공격력",
            UpgradeStat.Hp => "체력",
            UpgradeStat.Ms => "이동속도",
            UpgradeStat.AtkSpeed => "공격속도",
            UpgradeStat.Crit => "치명타",
            _ => stat.ToString(),
        };

        static string UpgradeHint(UpgradeStat stat) => stat switch
        {
            UpgradeStat.Atk => "모든 피해에 곱해진다",
            UpgradeStat.Hp => "최대 체력",
            UpgradeStat.Ms => "이동 속도",
            UpgradeStat.AtkSpeed => "공격 쿨다운이 짧아진다",
            UpgradeStat.Crit => "치명타 확률",
            _ => "",
        };

        // ────────────────────────── 전문화 ──────────────────────────

        /// <summary>
        /// 원본 `renderSpecTab()`(project_test.html:6971) — SP로 빌드를 **찍는** 곳.
        /// 갈래는 처음 찍을 때 고정되고 되돌릴 수 없으며, 반드시 1→5 순서로만 올라간다
        /// (판정은 전부 `PlayerProfile.TryLearnMageTier`가 한다 — UI는 부르기만).
        /// </summary>
        void DrawSpecTab(PlayerProfile profile)
        {
            if (profile.character != CharacterId.Mage)
            {
                GUILayout.Label($"{CharacterLabel(profile.character)}의 전문화는 아직 구현되지 않았다. " +
                                "현재 스킬트리가 있는 캐릭터는 마법사뿐이다.");
                return;
            }

            GUILayout.Label($"SP {profile.SpAvailable}개 사용 가능 (레벨 1당 1개). " +
                            "**한 갈래만 선택**하며, 각 층에서 Z 공격과 X 스킬이 함께 강화된다.");
            GUILayout.Space(4f);

            string branchNow = profile.mageBranch == MageBranch.None
                ? "아직 안 정함"
                : (profile.mageBranch == MageBranch.Explosion ? "폭발 계열" : "중력 계열");
            GUILayout.Label($"현재: {branchNow} · {profile.mageTier}층");
            if (profile.mageTier == 0)
                GUILayout.Label("⚠️ 0층에서는 X 스킬이 나가지 않는다 — 원본도 `tier < 1`이면 막는다(:2169).");
            GUILayout.Space(8f);

            DrawBranch(profile, MageBranch.Explosion, "폭발 계열", ExplosionTiers);
            GUILayout.Space(10f);
            DrawBranch(profile, MageBranch.Gravity, "중력 계열", GravityTiers);
        }

        void DrawBranch(PlayerProfile profile, MageBranch branch, string title, string[] tiers)
        {
            bool otherPicked = profile.mageBranch != MageBranch.None && profile.mageBranch != branch;

            GUILayout.Label($"<b>{title}</b>{(otherPicked ? "   (다른 갈래를 골라서 잠김)" : "")}", RichLabel());

            for (int t = 1; t <= tiers.Length; t++)
            {
                bool learned = profile.mageBranch == branch && profile.mageTier >= t;
                bool isNext = !otherPicked && profile.mageTier == t - 1;
                int cost = t; // 원본 SPEC.bow.*.tiers[].cost = 1,2,3,4,5

                GUILayout.BeginHorizontal();
                GUI.enabled = isNext && profile.SpAvailable >= cost;
                string mark = learned ? "✔" : (isNext ? "▶" : "·");
                if (GUILayout.Button($"{mark} {t}층   (SP {cost})", GUILayout.Height(26f), GUILayout.Width(180f)))
                {
                    if (profile.TryLearnMageTier(branch))
                    {
                        ShowToast($"{title} {t}층 습득");
                        SaveService.Save(); // 원본 `learnSkill` 직후 저장(:7020)
                    }
                }
                GUI.enabled = true;
                GUILayout.Label(tiers[t - 1]);
                GUILayout.EndHorizontal();
            }
        }

        // 원본 SPEC.bow.move / SPEC.bow.conv 의 티어 설명 그대로(project_test.html:900~913).
        static readonly string[] ExplosionTiers =
        {
            "폭발 각인 — Z가 화상 폭발 표식을 남긴다. X는 이동하며 경로에 불길.",
            "연쇄 폭심 — 착탄 폭발과 불길 이동 피해 증가.",
            "폭발 파동 — 충전 없이도 착탄 폭발. 불길 범위 확대.",
            "압축 화약진 — 폭발 범위 확대, X 쿨타임 감소.",
            "대폭염 — 폭발·불길 피해가 크게 강화된다.",
        };

        static readonly string[] GravityTiers =
        {
            "특이점 생성 — Z가 원 범위 피해와 중력점을 만든다. ↑ 위로 발사, ↓ 제자리 소환.",
            "중첩 특이점 — 중력점 근처에 다시 맞히면 중첩. 차징은 즉시 2~3중첩.",
            "다중 중력점 — 동시 3개, 블랙홀 안 모든 적에게 지속 피해.",
            "중력 취약 — 지속시간·흡입력 증가, 오래 노출된 적은 받는 피해 증가.",
            "대붕괴 — 최대 5중첩·5개, 밀집 시 대형 2차 폭발.",
        };

        // ────────────────────────── 윤회 ──────────────────────────

        /// <summary>
        /// 원본 윤회 탭(project_test.html:6495~). **이 게임의 유일한 영구 성장 축**이다 —
        /// 입장료와 몹 체력이 지역당 지수로 올라서 한 생의 강화만으로는 반드시 벽에 부딪히고,
        /// 그 벽을 넘는 수단이 윤회뿐이다.
        /// </summary>
        void DrawRebirthTab(PlayerProfile profile)
        {
            int gain = profile.RebirthPointPreview();
            int cleared = profile.ClearedRegionCount();

            GUILayout.Label("윤회하면 이번 생을 처음부터 다시 시작하는 대신 **윤회 포인트**를 얻는다. " +
                            "포인트는 뽑기(아이템)에 쓰고, 윤회 횟수 자체가 상위 지역의 벽을 낮춘다.");
            GUILayout.Space(6f);
            GUILayout.Label($"이번 생 정복한 지역   {cleared} / {RegionConfig.Count}");
            GUILayout.Label($"윤회하면 받을 포인트   ☸ {gain}");
            GUILayout.Label($"지금까지 윤회   {profile.rebirths}회   ·   보유 포인트   ☸ {profile.rp}");
            GUILayout.Space(4f);
            GUILayout.Label("보상은 **어디까지 뚫었나**로만 정해진다 — 오래 플레이한다고 늘지 않는다.");

            GUILayout.Space(10f);
            GUILayout.Label("<b>초기화</b>: 레벨 · 경험치 · 골드 · 골드 강화 · 전문화(SP) · 지역 진행", RichLabel());
            GUILayout.Label("<b>유지</b>: 윤회 포인트 · 윤회 횟수", RichLabel());

            GUILayout.Space(10f);
            if (gain < 1)
            {
                GUILayout.Label("아직 정복한 지역이 없어서 윤회할 수 없다 — 보스를 하나라도 잡아야 한다.");
            }
            else if (!confirmingRebirth)
            {
                if (GUILayout.Button($"☸ 윤회하기 (+{gain})", GUILayout.Height(32f), GUILayout.Width(220f)))
                    confirmingRebirth = true;
            }
            else
            {
                // 되돌릴 수 없는 조작이라 한 번 더 묻는다(원본도 confirm() 대화상자를 쓴다).
                GUILayout.Label("정말 윤회할까? 이번 생의 진행은 전부 사라진다.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"네, 윤회합니다 (+{gain})", GUILayout.Height(30f), GUILayout.Width(200f)))
                {
                    int got = profile.DoRebirth();
                    confirmingRebirth = false;
                    if (got > 0)
                    {
                        ShowToast($"☸ 윤회 — 포인트 +{got}");
                        SaveService.Save();
                        tab = Tab.Stage; // 지역 진행이 초기화됐으니 그쪽을 보여준다
                    }
                }
                if (GUILayout.Button("취소", GUILayout.Height(30f), GUILayout.Width(80f))) confirmingRebirth = false;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(14f);
            DrawRebirthWallTable(profile);
        }

        /// <summary>
        /// 지역별 권장 윤회와 지금 걸리는 벽. 원본은 스테이지 탭에 "윤회 N회"로 적어두는데,
        /// **얼마나 불리해지는지**는 안 보여준다 — 숫자를 직접 보여주는 편이 판단에 도움이 된다.
        /// </summary>
        void DrawRebirthWallTable(PlayerProfile profile)
        {
            GUILayout.Label("<b>지역별 윤회 장벽</b>  (권장에 모자라면 몹이 단단해지고 내 피해가 줄어든다)", RichLabel());
            for (int r = 3; r <= RegionConfig.Count; r++) // 1·2지역은 권장 0회라 벽이 없다
            {
                int req = RebirthConfig.RequiredRebirths(r);
                int gap = RebirthConfig.Gap(r, profile.rebirths);
                string line = $"{r}. {RegionConfig.NameOf(r)}   권장 윤회 {req}회";
                if (gap == 0) line += "   ✔ 벽 없음";
                else line += $"   ⚠ {gap}회 부족 → 몹 체력 ×{RebirthConfig.WallEnemyHp(r, profile.rebirths):0.0}" +
                             $" · 내 피해 ×{RebirthConfig.WallPlayerDamage(r, profile.rebirths):0.00}";
                GUILayout.Label(line);
            }
        }

        // ────────────────────────── 잡동사니 ──────────────────────────

        void ShowToast(string message)
        {
            toast = message;
            toastLeft = 2f;
        }

        static GUIStyle richLabel;
        static GUIStyle RichLabel()
        {
            if (richLabel == null) richLabel = new GUIStyle(GUI.skin.label) { richText = true };
            return richLabel;
        }
    }
}
