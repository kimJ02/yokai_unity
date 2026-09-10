using UnityEngine;
using YokaiFront.Characters;
using YokaiFront.Core;

namespace YokaiFront.UI
{
    /// <summary>
    /// 사냥 중 HUD — 원본 `#hud`(project_test.html:487)와 `syncHUD()`(`:6255`)에 대응한다.
    ///
    /// ## 왜 판을 안 깔았나
    /// 로비·결과 화면은 어두운 판으로 배경을 덮어서 글씨를 읽히게 만든다. **HUD는 그럴 수 없다** —
    /// 판을 깔면 정작 봐야 할 사냥터를 가린다. 원본이 쓰는 방법은 둘이다:
    ///
    ///   1. 수치는 **바**로 보여준다 — 반투명 검정 바탕(`rgba(0,0,0,.55)`)에 색 채움(`.bar` `:31`).
    ///      바 자체가 작은 대비 영역이라 그 위의 글씨가 읽힌다.
    ///   2. 바 밖의 글자에는 전부 **그림자**를 준다 — `text-shadow:0 1px 2px #000`(`:35`~`:47`).
    ///
    /// 예전엔 IMGUI 기본 라벨을 그대로 썼는데, 그건 밝은 판에 검은 글씨를 전제로 한 스킨이라
    /// 어두운 사냥터 위에서 요괴·발판과 섞였다.
    ///
    /// ## 아직 없는 것
    /// 보스 체력바(`#bossbar` `:49`)와 스킬 쿨다운 슬롯(`.slot` `:60`)은 원본에 있지만 여기 없다.
    /// 가독성 문제가 아니라 **따로 만들어야 하는 기능**이라 이번 범위 밖으로 뒀다.
    ///
    /// 일시정지·결과 화면에서도 계속 그린다 — 원본도 `#hud`를 로비에서만 숨긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHud : MonoBehaviour
    {
        // 원본 좌표 그대로(`#topbar` :30, `#timerbox` :39, `#combo` :52, `#hints` :74).
        const float BarWidth = 320f;   // 원본 `#topbar { width:320px }`
        const float BarHeight = 20f;   // 원본 `.bar { height:20px }`
        const float BarGap = 26f;      // 20 + margin-bottom 6
        const float LeftX = 16f;       // 원본 `left:16px`
        const float TopY = 14f;        // 원본 `top:14px`

        /// <summary>이 이하로 남으면 타이머가 붉어진다. 원본 `classList.toggle('low', t <= 10)`(`:6267`).</summary>
        const int LowTimeSeconds = 10;

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

            using var scaled = UiTheme.Scaled();

            var profile = ProfileService.Current;

            DrawTopBar(profile);
            DrawTimerBox();
            DrawCombo();
            DrawHints();
        }

        /// <summary>원본 `#topbar`(`:30`) — 체력·경험치 바와 그 아래 정보줄.</summary>
        void DrawTopBar(PlayerProfile profile)
        {
            float y = TopY;

            if (health != null)
            {
                float ratio = health.MaxHp > 0f ? health.CurrentHp / health.MaxHp : 0f;
                UiTheme.Bar(new Rect(LeftX, y, BarWidth, BarHeight), ratio, UiTheme.HpFill,
                            $"HP {Mathf.Ceil(health.CurrentHp)} / {Mathf.Ceil(health.MaxHp)}");
            }
            y += BarGap;

            int need = PlayerProfile.RequiredExp(profile.level);
            UiTheme.Bar(new Rect(LeftX, y, BarWidth, BarHeight),
                        need > 0 ? profile.exp / (float)need : 0f, UiTheme.ExpFill,
                        $"EXP {profile.exp} / {need}");
            y += BarGap;

            // 원본 `#infoline`(`:36`) — 수치만 금색(`b { color:#ffd76e }`).
            UiTheme.ShadowLabel(new Rect(LeftX, y, 520f, 20f),
                $"Lv.<b>{profile.level}</b>     골드 <b>{profile.gold:N0}</b> G", UiTheme.Hud);
            y += 22f;

            // 원본 `#buffs`(`:37`) — 지금 몸에 걸린 것들. 원본은 캐릭터별 자원(마나·충전 등)까지
            // 여기 붙이는데, 우리는 아직 전투 배수 체인만 있다.
            string buffs = BuffText();
            if (!string.IsNullOrEmpty(buffs))
                UiTheme.ShadowLabel(new Rect(LeftX, y, 700f, 20f), buffs, UiTheme.HudSmall, UiTheme.BuffLine);
        }

        /// <summary>원본 `buffTxt`(`:6288`~`:6318`) 중 우리가 가진 것들.</summary>
        string BuffText()
        {
            string s = "";

            // 원본 `:6288` — 성소가 살아 있으면 가장 먼저 경고한다.
            if (CombatModifiers.ShrineActive) s += "⚠ 요괴들이 성소의 힘을 받는 중!   ";
            if (CombatModifiers.ShrineBuffLeft > 0f)
                s += $"⛩ 성소의 가호 {CombatModifiers.ShrineBuffLeft:0.0}초   ";

            s += $"살기 {RunState.Fury}   피해 ×{CombatModifiers.DamageMultiplier:0.00}";
            if (CombatModifiers.ChainKills >= CombatModifiers.ChainKillMin)
                s += $"   연쇄 ×{CombatModifiers.ChainKills}";

            // 원본 `:6290` — 권장 윤회에 모자란 지역이면 몹이 단단해지고 내 피해가 줄어든다.
            int gap = RebirthConfig.Gap(RunState.Region, ProfileService.Current.rebirths);
            if (gap > 0) s += $"   ⚠ 윤회 부족 {gap}회";

            return s;
        }

        /// <summary>원본 `#timerbox`(`:39`) — 화면 위 가운데. 남은 시간이 가장 크게 온다.</summary>
        void DrawTimerBox()
        {
            int t = Mathf.CeilToInt(RunState.TimeLeft);
            UiTheme.ShadowLabel(new Rect(0f, 12f, UiTheme.DesignWidth, 46f), t.ToString(), UiTheme.Timer,
                                t <= LowTimeSeconds ? UiTheme.TimerLow : Color.white);

            string stage = $"{RunState.Region}지역 · {RegionConfig.NameOf(RunState.Region)}"
                           + (RunState.Mode == RunMode.Boss ? " — 보스전" : "");
            UiTheme.ShadowLabel(Centered(60f, 18f), stage, Center(UiTheme.HudSmall), UiTheme.StageLine);

            // 원본 `:6271` — 일반 사냥에서만 지역 토벌 진행도를 같이 보여준다(보스전은 진행도가 안 오른다).
            string kills = RunState.Mode == RunMode.Normal
                ? $"이번 사냥 {RunState.Kills}마리 · 토벌 " +
                  $"{ProfileService.Current.RegionKills(RunState.Region)}/{RunState.RegionKillTarget}"
                : $"이번 사냥 {RunState.Kills}마리";
            UiTheme.ShadowLabel(Centered(80f, 20f), kills, Center(UiTheme.Hud), UiTheme.KillLine);

            UiTheme.ShadowLabel(Centered(102f, 18f),
                $"+{RunState.GoldEarned:N0} G · +{RunState.ExpEarned:N0} EXP",
                Center(UiTheme.HudSmall), UiTheme.EarnLine);
        }

        /// <summary>
        /// 원본 `#combo`(`:52`) — 오른쪽 위에 크게. 콤보가 0이면 아예 안 보인다(`:1633`).
        /// 왼쪽 정보줄에 작게 끼워 넣으면 "쌓을수록 이득"이라는 신호가 안 온다.
        /// </summary>
        void DrawCombo()
        {
            int n = CombatModifiers.Combo;
            if (n <= 0) return;

            const float right = 28f;
            var numRect = new Rect(UiTheme.DesignWidth - right - 400f, 52f, 400f, 50f);
            UiTheme.ShadowLabel(numRect, n.ToString(), Right(ComboNumber()), UiTheme.ComboNum);

            // 원본 라벨은 `COMBO ×(1 + n * dmgPer)` — 콤보 수가 아니라 **지금 배수**를 보여준다.
            float mult = 1f + n * CombatModifiers.ComboDamagePer;
            UiTheme.ShadowLabel(new Rect(numRect.x, numRect.yMax - 2f, numRect.width, 22f),
                                $"COMBO ×{mult:0.00}", Right(UiTheme.Hud), UiTheme.GoldColor);
        }

        /// <summary>원본 `#hints`(`:74`) — 오른쪽 아래 조작 안내.</summary>
        void DrawHints() =>
            UiTheme.ShadowLabel(new Rect(UiTheme.DesignWidth - 16f - 600f, UiTheme.DesignHeight - 34f, 600f, 20f),
                                "ESC — 일시정지 / 로비로 나가기", Right(UiTheme.HudSmall),
                                new Color(0.66f, 0.60f, 0.47f)); // 원본 `#hints { color:#a89878 }`

        static Rect Centered(float y, float h) => new Rect(0f, y, UiTheme.DesignWidth, h);

        // ── 정렬만 바꾼 사본 (원본 스타일을 그 자리에서 고치면 다른 화면까지 따라 바뀐다) ──
        static GUIStyle centerSmall, centerHud, rightHud, rightSmall, comboNum;

        static GUIStyle Center(GUIStyle basis)
        {
            if (basis == UiTheme.HudSmall)
                return centerSmall ??= new GUIStyle(basis) { alignment = TextAnchor.UpperCenter };
            return centerHud ??= new GUIStyle(basis) { alignment = TextAnchor.UpperCenter };
        }

        static GUIStyle Right(GUIStyle basis)
        {
            if (basis == UiTheme.HudSmall)
                return rightSmall ??= new GUIStyle(basis) { alignment = TextAnchor.UpperRight };
            if (basis == UiTheme.Hud)
                return rightHud ??= new GUIStyle(basis) { alignment = TextAnchor.UpperRight };
            return basis; // 이미 정렬이 잡힌 스타일(콤보 숫자)은 그대로
        }

        /// <summary>원본 `#combo .n { font-size:44px; font-style:italic }`(`:54`).</summary>
        static GUIStyle ComboNumber()
        {
            if (comboNum == null)
                comboNum = new GUIStyle(UiTheme.Hud)
                {
                    fontSize = 40,
                    fontStyle = FontStyle.BoldAndItalic,
                    alignment = TextAnchor.UpperRight,
                };
            return comboNum;
        }
    }
}
