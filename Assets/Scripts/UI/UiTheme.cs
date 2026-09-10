using UnityEngine;

namespace YokaiFront.UI
{
    /// <summary>
    /// 화면 밖 UI(로비 · 결과 · HUD)의 색·판·글자 모양 — 원본 CSS를 그대로 옮긴 것이다.
    /// (`.btn` project_test.html:18, `#hud` `:29`~`:47`, `#lobbyHead` `:85`, `.tabbtn` `:178`,
    ///  `#lobbyBody` `:180`, `#result` `:223`~`:227`, `#toast` `:230`)
    ///
    /// ## 왜 필요한가
    /// 우리 UI는 전부 사냥터 **위에** 그대로 얹힌다. IMGUI 기본 스킨은 밝은 회색 판에 검은 글씨를
    /// 전제로 만들어져 있어서, 그 위에 얹으면 버튼이 반투명 회색으로 보이고 글씨가 요괴·발판과
    /// 섞여 읽히지 않는다. 원본이 이 문제를 푸는 방식은 **자리에 따라 다른 두 가지**다:
    ///
    ///   1. **읽어야 할 덩어리는 판으로 덮는다** — 로비 본문 `rgba(14,11,20,.92)`(`:180`),
    ///      결과 화면은 화면 전체를 `rgba(5,4,10,.88)`로 덮는다(`:223`).
    ///   2. **덮을 수 없는 자리(헤더·HUD)는 글자에 그림자를 준다** — `text-shadow:0 1px 2px #000`.
    ///      HUD는 게임을 가리면 안 되니 판을 깔 수 없고, 그림자가 유일한 대비 수단이다.
    ///
    /// 자리마다 맞는 쪽을 써야 한다. HUD에 판을 깔면 화면을 가리고, 로비 본문에 그림자만 주면
    /// 긴 글이 배경 위에서 계속 흔들린다.
    ///
    /// ## 테두리를 8×8 텍스처로 만드는 이유
    /// CSS의 `border:1px`을 IMGUI로 옮기려면 9-슬라이스가 필요하다. 가장자리 1px만 테두리색인
    /// 작은 텍스처를 만들고 <see cref="GUIStyle.border"/>를 1로 두면, 판이 아무리 커져도 테두리는
    /// 1px로 유지되고 가운데만 늘어난다. 단색 텍스처를 통째로 늘리면 테두리가 같이 두꺼워진다.
    /// </summary>
    public static class UiTheme
    {
        /// <summary>원본 캔버스 크기(`CONFIG.canvas`). 이 좌표계로 그리고 화면에 맞춰 확대한다.</summary>
        public const float DesignWidth = 1280f;
        public const float DesignHeight = 720f;

        // ── 원본 CSS 색 ──────────────────────────────────────────
        static readonly Color PanelBg     = Hex(0x0e, 0x0b, 0x14, 0.92f); // #lobbyBody
        static readonly Color PanelBorder = Hex(0x6a, 0x4a, 0x3a);
        static readonly Color TabOffBg    = Hex(0x0e, 0x0b, 0x14, 0.85f); // .tabbtn
        static readonly Color TabOffBorder= Hex(0x4a, 0x3a, 0x3a);
        static readonly Color TabOffText  = Hex(0xb8, 0xa8, 0x88);
        static readonly Color TabOnBg     = Hex(0x1e, 0x16, 0x1e, 0.95f); // .tabbtn.on
        static readonly Color TabOnBorder = Hex(0xc9, 0xa2, 0x4a);
        static readonly Color TabOnText   = Hex(0xff, 0xe9, 0xc0);
        static readonly Color BtnBg       = Hex(0x3a, 0x25, 0x20);        // .btn
        static readonly Color BtnBorder   = Hex(0x8a, 0x5a, 0x3a);
        static readonly Color BtnHoverBg  = Hex(0x5a, 0x35, 0x25);        // .btn:hover
        static readonly Color BtnHoverBd  = Hex(0xc9, 0xa2, 0x4a);
        static readonly Color ToastBg     = Hex(0x1e, 0x14, 0x0a, 0.95f); // #toast
        static readonly Color ToastBorder = Hex(0xc9, 0xa2, 0x4a);
        static readonly Color RowsBg      = Hex(0x00, 0x00, 0x00, 0.40f); // #resultRows
        static readonly Color RowsBorder  = Hex(0x4a, 0x3a, 0x3a);

        /// <summary>결과·일시정지 화면이 화면 전체를 덮는 색. 원본 `#result`(`:223`).</summary>
        public static readonly Color Scrim = Hex(0x05, 0x04, 0x0a, 0.88f);

        /// <summary>본문 글자색. 원본 `#lobbyHead .stat`(`:87`).</summary>
        public static readonly Color TextColor  = Hex(0xc8, 0xb8, 0xa0);
        /// <summary>버튼 글자색. 원본 `.btn`(`:19`).</summary>
        public static readonly Color BtnText    = Hex(0xff, 0xd8, 0xa0);
        /// <summary>강조 수치(골드·SP 등). 원본 `.goldc`(`:26`).</summary>
        public static readonly Color GoldColor  = Hex(0xff, 0xd7, 0x6e);
        /// <summary>제목. 원본 `#lobbyHead h1`(`:86`) · `#result h1`(`:224`).</summary>
        public static readonly Color TitleColor = Hex(0xe8, 0xd0, 0xa0);
        /// <summary>쓰러졌을 때의 결과 제목. 원본 `#result h1.dead`(`:225`).</summary>
        public static readonly Color DeadColor  = Hex(0xff, 0x6a, 0x5a);
        /// <summary>격파했을 때의 결과 제목. 원본 `#result h1.clear`(`:226`).</summary>
        public static readonly Color ClearColor = Hex(0x8e, 0xf0, 0xc8);
        /// <summary>토스트 글자색. 원본 `#toast`(`:230`).</summary>
        public static readonly Color ToastText  = Hex(0xff, 0xe9, 0xb0);

        // HUD 색 — 원본 `:35`~`:47`
        /// <summary>체력 바. 원본 `#hpfill` 그라데이션의 중간값(`:32`).</summary>
        public static readonly Color HpFill    = Hex(0xd9, 0x38, 0x30);
        /// <summary>경험치 바. 원본 `#expfill` 그라데이션의 중간값(`:33`).</summary>
        public static readonly Color ExpFill   = Hex(0xe4, 0xb2, 0x44);
        /// <summary>지역 줄. 원본 `#stageline`(`:43`).</summary>
        public static readonly Color StageLine = Hex(0xc8, 0xb8, 0x90);
        /// <summary>처치 줄. 원본 `#killline`(`:44`).</summary>
        public static readonly Color KillLine  = Hex(0x9a, 0xd0, 0xff);
        /// <summary>획득 줄. 원본 `#earnline`(`:45`).</summary>
        public static readonly Color EarnLine  = GoldColor;
        /// <summary>버프 줄. 원본 `#buffs`(`:37`).</summary>
        public static readonly Color BuffLine  = Hex(0x8e, 0xf0, 0xc8);
        /// <summary>콤보 숫자. 원본 `#combo .n`(`:54`).</summary>
        public static readonly Color ComboNum  = Hex(0xff, 0xb3, 0x40);
        /// <summary>시간이 얼마 안 남았을 때의 타이머. 원본 `#timer.low`(`:41`).</summary>
        public static readonly Color TimerLow  = DeadColor;

        // ── 스타일 ──────────────────────────────────────────────
        public static GUISkin Skin { get; private set; }
        /// <summary>로비 본문을 덮는 판(`#lobbyBody`).</summary>
        public static GUIStyle Panel { get; private set; }
        /// <summary>결과 화면의 수치 판(`#resultRows`).</summary>
        public static GUIStyle Rows { get; private set; }
        /// <summary>탭 하나(`.tabbtn` / `.tabbtn.on`). 선택 상태는 `onNormal`이 맡는다.</summary>
        public static GUIStyle Tab { get; private set; }
        /// <summary>로비 제목(`#lobbyHead h1`).</summary>
        public static GUIStyle Title { get; private set; }
        /// <summary>결과 화면 제목(`#result h1`, 52px).</summary>
        public static GUIStyle BigTitle { get; private set; }
        /// <summary>헤더 통계줄(`#lobbyHead .stat`).</summary>
        public static GUIStyle HeadStat { get; private set; }
        /// <summary>HUD 기본 글자(`#infoline`, 14px).</summary>
        public static GUIStyle Hud { get; private set; }
        /// <summary>HUD 작은 글자(`#stageline`·`#earnline`, 13px 이하).</summary>
        public static GUIStyle HudSmall { get; private set; }
        /// <summary>가운데 타이머(`#timer`, 38px).</summary>
        public static GUIStyle Timer { get; private set; }
        /// <summary>바 안의 글자(`.bar .txt`, 12px 굵게 흰색).</summary>
        public static GUIStyle BarText { get; private set; }
        /// <summary>토스트 판(`#toast`).</summary>
        public static GUIStyle Toast { get; private set; }

        static Texture2D barBg, white;

        /// <summary>
        /// 첫 <c>OnGUI</c>에서 한 번만 만든다. `GUI.skin`을 복제해야 해서 OnGUI 밖에서는 부를 수 없다.
        /// </summary>
        public static void Ensure()
        {
            if (Skin != null) return;

            Panel = Boxed(PanelBg, PanelBorder);
            Panel.padding = new RectOffset(18, 18, 14, 14); // 원본 `padding:14px 18px`

            Rows = Boxed(RowsBg, RowsBorder);
            Rows.padding = new RectOffset(40, 40, 16, 16);  // 원본 `#resultRows { padding:16px 40px }`

            var button = Boxed(BtnBg, BtnBorder);
            button.padding = new RectOffset(14, 14, 6, 6);  // 원본 `.btn { padding:6px 14px }`
            button.fontSize = 13;
            button.alignment = TextAnchor.MiddleCenter;
            button.wordWrap = false;
            Paint(button.normal, BtnBg, BtnBorder, BtnText);
            Paint(button.hover, BtnHoverBg, BtnHoverBd, BtnText);
            Paint(button.active, BtnHoverBg, BtnHoverBd, BtnText);
            Paint(button.focused, BtnBg, BtnBorder, BtnText);

            Tab = Boxed(TabOffBg, TabOffBorder);
            Tab.padding = new RectOffset(20, 20, 8, 8);      // 원본 `.tabbtn { padding:8px 20px }`
            Tab.fontSize = 15;
            Tab.alignment = TextAnchor.MiddleCenter;
            Paint(Tab.normal, TabOffBg, TabOffBorder, TabOffText);
            Paint(Tab.hover, TabOffBg, TabOnBorder, TabOnText);
            // 선택된 탭 — IMGUI에서 Toggle/Toolbar의 "켜짐"은 on* 상태가 그린다.
            Paint(Tab.onNormal, TabOnBg, TabOnBorder, TabOnText);
            Paint(Tab.onHover, TabOnBg, TabOnBorder, TabOnText);
            Paint(Tab.onActive, TabOnBg, TabOnBorder, TabOnText);

            var label = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, wordWrap = true };
            label.normal.textColor = TextColor;
            label.hover.textColor = TextColor;

            Title = new GUIStyle(label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = false };
            Title.normal.textColor = TitleColor;

            // 원본 `#result h1 { font-size:52px }`. 그림자로만 대비를 만드는 자리라 굵게 간다.
            BigTitle = new GUIStyle(label)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
            };
            BigTitle.normal.textColor = TitleColor;

            HeadStat = new GUIStyle(label) { fontSize = 15, wordWrap = false };

            Hud = new GUIStyle(label) { fontSize = 14, wordWrap = false };
            HudSmall = new GUIStyle(label) { fontSize = 13, wordWrap = false };

            Timer = new GUIStyle(label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
            };
            Timer.normal.textColor = Color.white;

            BarText = new GUIStyle(label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
            };
            BarText.normal.textColor = Color.white;

            Toast = Boxed(ToastBg, ToastBorder);
            Toast.padding = new RectOffset(26, 26, 10, 10);
            Toast.fontSize = 16;
            Toast.alignment = TextAnchor.MiddleCenter;
            Toast.normal.textColor = ToastText;

            // 원본 `.bar { background:rgba(0,0,0,.55); border:1px solid rgba(255,255,255,.25) }`(`:31`).
            barBg = BoxTexture(Hex(0x00, 0x00, 0x00, 0.55f), new Color(1f, 1f, 1f, 0.25f));
            white = Solid(Color.white);

            Skin = Object.Instantiate(GUI.skin);
            Skin.hideFlags = HideFlags.HideAndDontSave;
            Skin.label = label;
            Skin.button = button;
            Skin.box = Panel;
            Skin.toggle = button;
        }

        /// <summary>
        /// 원본 `fitScreen()`(project_test.html:7176)과 같은 방식 — 1280×720으로 그리고 화면에 맞춰
        /// 통째로 확대한다. 이걸 안 하면 해상도마다 글자 크기와 여백 비율이 달라져서, 원본 CSS
        /// 픽셀값을 옮겨온 게 의미를 잃는다.
        /// </summary>
        /// <example><code>using (UiTheme.Scaled()) { /* 1280×720 좌표로 그린다 */ }</code></example>
        public static ScaledGui Scaled()
        {
            Ensure();
            var saved = new ScaledGui(GUI.matrix, GUI.skin);

            float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - DesignWidth * scale) * 0.5f,
                            (Screen.height - DesignHeight * scale) * 0.5f, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.skin = Skin;
            return saved;
        }

        /// <summary>`using`이 끝날 때 원래 행렬·스킨으로 되돌린다 — 다른 OnGUI에 영향이 안 가게.</summary>
        public readonly struct ScaledGui : System.IDisposable
        {
            readonly Matrix4x4 matrix;
            readonly GUISkin skin;
            public ScaledGui(Matrix4x4 m, GUISkin s) { matrix = m; skin = s; }
            public void Dispose() { GUI.matrix = matrix; GUI.skin = skin; }
        }

        /// <summary>원본 `.bar`(`:31`) — 바탕 위에 비율만큼 채운다. `text`가 있으면 가운데에 얹는다.</summary>
        public static void Bar(Rect r, float ratio, Color fill, string text = null)
        {
            GUI.DrawTexture(r, barBg);

            float w = Mathf.Clamp01(ratio) * (r.width - 2f);
            if (w > 0f)
            {
                Color prev = GUI.color;
                GUI.color = fill;
                GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, w, r.height - 2f), white);
                GUI.color = prev;
            }

            if (!string.IsNullOrEmpty(text)) ShadowLabel(r, text, BarText);
        }

        /// <summary>
        /// 화면 전체를 덮는 어두운 막. 원본 `#result`(`:223`).
        ///
        /// **디자인 좌표가 아니라 실제 화면 좌표로 그린다.** 1280×720 비율이 아닌 화면에서는
        /// 확대된 디자인 영역 바깥에 레터박스가 남는데, 거기까지 안 덮으면 화면 위아래(또는 좌우)로
        /// 사냥터가 그대로 비쳐서 "화면을 덮는다"는 의도가 깨진다.
        /// </summary>
        public static void FullScreenScrim()
        {
            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;

            GUI.matrix = Matrix4x4.identity;
            GUI.color = Scrim;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), white);

            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        /// <summary>
        /// 원본 `text-shadow:0 1px 2px #000`. **판으로 덮을 수 없는 자리(HUD·헤더·제목)에만** 쓴다 —
        /// 사냥터 위에 그대로 얹히기 때문에 그림자 말고는 대비를 만들 방법이 없다.
        /// </summary>
        public static void ShadowLabel(Rect r, string text, GUIStyle style) =>
            ShadowLabel(r, text, style, style.normal.textColor);

        /// <summary>색을 그 자리에서 정해야 할 때(타이머의 위험 색 등).</summary>
        public static void ShadowLabel(Rect r, string text, GUIStyle style, Color color)
        {
            Color prev = style.normal.textColor;

            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x, r.y + 1f, r.width, r.height), text, style);

            style.normal.textColor = color;
            GUI.Label(r, text, style);

            style.normal.textColor = prev;
        }

        // ── 만들기 ──────────────────────────────────────────────

        static GUIStyle Boxed(Color fill, Color border)
        {
            var s = new GUIStyle
            {
                border = new RectOffset(1, 1, 1, 1), // 9-슬라이스: 가장자리 1px은 안 늘어난다
                richText = true,
                wordWrap = false,
            };
            Paint(s.normal, fill, border, TextColor);
            return s;
        }

        static void Paint(GUIStyleState state, Color fill, Color border, Color text)
        {
            state.background = BoxTexture(fill, border);
            state.textColor = text;
        }

        /// <summary>가장자리 1px만 테두리색인 8×8 텍스처. `border = 1`과 짝을 이룬다.</summary>
        static Texture2D BoxTexture(Color fill, Color border)
        {
            const int n = 8;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = (x == 0 || y == 0 || x == n - 1 || y == n - 1) ? border : fill;
            tex.SetPixels(px);
            tex.filterMode = FilterMode.Point; // 흐려지면 1px 테두리가 뭉갠다
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        static Texture2D Solid(Color c)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        static Color Hex(int r, int g, int b, float a = 1f) => new Color(r / 255f, g / 255f, b / 255f, a);
    }
}
