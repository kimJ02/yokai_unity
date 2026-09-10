using UnityEngine;

namespace YokaiFront.UI
{
    /// <summary>
    /// 로비의 색·판·버튼 모양 — 원본 CSS(project_test.html:18~25 `.btn`, `:177`~`:180` 탭/본문,
    /// `:84`~`:92` 헤더, `:230` 토스트)를 그대로 옮긴 것이다.
    ///
    /// ## 왜 따로 있나
    /// IMGUI 기본 스킨은 **밝은 회색 판에 검은 글씨**를 전제로 만들어져 있는데, 우리 로비는
    /// 사냥터 위에 그대로 겹쳐 뜬다. 그래서 버튼이 반투명 회색으로 보이고 흰 글씨가 배경의
    /// 요괴·발판과 섞여 읽히지 않았다. 원본이 이 문제를 푸는 방식은 딱 두 가지다:
    ///
    ///   1. **본문을 거의 불투명한 판으로 덮는다** — `#lobbyBody { background:rgba(14,11,20,.92) }`.
    ///      92%면 뒤가 사실상 안 비친다. 이게 가독성의 90%다.
    ///   2. **판 밖에 있는 글자(헤더)에는 그림자를 준다** — `text-shadow:0 1px 3px #000`.
    ///      헤더는 배경을 덮지 않고 그 위에 얹히므로 그림자가 유일한 대비 수단이다.
    ///
    /// 두 가지를 다 해야 한다. 판만 깔면 헤더가 여전히 안 읽히고, 그림자만 주면 본문의
    /// 긴 글이 배경 위에서 계속 흔들린다.
    ///
    /// ## 테두리를 8×8 텍스처로 만드는 이유
    /// CSS의 `border:1px`을 IMGUI로 옮기려면 9-슬라이스가 필요하다. 가장자리 1px만 테두리색인
    /// 작은 텍스처를 만들고 <see cref="GUIStyle.border"/>를 1로 두면, 판이 아무리 커져도
    /// 테두리는 1px로 유지되고 가운데만 늘어난다. 단색 텍스처를 통째로 늘리면 테두리가 같이
    /// 두꺼워져서 원본과 달라진다.
    /// </summary>
    public static class LobbyTheme
    {
        /// <summary>원본 캔버스 크기(project_test.html `CONFIG.canvas`). 이 좌표계로 그리고 화면에 맞춰 확대한다.</summary>
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

        /// <summary>본문 글자색. 원본 `#lobbyHead .stat`(`:87`).</summary>
        public static readonly Color TextColor  = Hex(0xc8, 0xb8, 0xa0);
        /// <summary>버튼 글자색. 원본 `.btn`(`:19`).</summary>
        public static readonly Color BtnText    = Hex(0xff, 0xd8, 0xa0);
        /// <summary>강조 수치(골드·SP 등). 원본 `.goldc`(`:26`).</summary>
        public static readonly Color GoldColor  = Hex(0xff, 0xd7, 0x6e);
        /// <summary>제목. 원본 `#lobbyHead h1`(`:86`).</summary>
        public static readonly Color TitleColor = Hex(0xe8, 0xd0, 0xa0);
        /// <summary>토스트 글자색. 원본 `#toast`(`:230`).</summary>
        public static readonly Color ToastText  = Hex(0xff, 0xe9, 0xb0);
        /// <summary>경험치 바 채움. 원본 `#lobbyExpbar div`의 그라데이션 중간값(`:91`).</summary>
        public static readonly Color ExpFill    = Hex(0xe4, 0xb2, 0x44);

        // ── 스타일 ──────────────────────────────────────────────
        public static GUISkin Skin { get; private set; }
        /// <summary>본문을 덮는 판(`#lobbyBody`).</summary>
        public static GUIStyle Panel { get; private set; }
        /// <summary>탭 하나(`.tabbtn` / `.tabbtn.on`). 선택 상태는 `onNormal`이 맡는다.</summary>
        public static GUIStyle Tab { get; private set; }
        /// <summary>제목(`#lobbyHead h1`).</summary>
        public static GUIStyle Title { get; private set; }
        /// <summary>헤더 통계줄(`#lobbyHead .stat`).</summary>
        public static GUIStyle HeadStat { get; private set; }
        /// <summary>토스트 판(`#toast`).</summary>
        public static GUIStyle Toast { get; private set; }
        /// <summary>경험치 바의 바탕·채움에 쓰는 단색.</summary>
        public static GUIStyle Flat { get; private set; }

        static Texture2D expBarBg, expBarFill;

        /// <summary>
        /// 첫 <c>OnGUI</c>에서 한 번만 만든다. `GUI.skin`을 복제해야 해서 OnGUI 밖에서는 부를 수 없다.
        /// </summary>
        public static void Ensure()
        {
            if (Skin != null) return;

            Panel = Boxed(PanelBg, PanelBorder);
            Panel.padding = new RectOffset(18, 18, 14, 14); // 원본 `padding:14px 18px`

            var button = Boxed(BtnBg, BtnBorder);
            button.padding = new RectOffset(14, 14, 6, 6);  // 원본 `.btn { padding:6px 14px }`
            button.fontSize = 13;                            // 원본 `font-size:13px`
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

            var label = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 14,
                wordWrap = true,
            };
            label.normal.textColor = TextColor;
            // IMGUI는 비활성 상태에 별도 색을 쓴다 — 안 맞추면 `GUI.enabled = false` 구간만 회색으로 튄다.
            label.hover.textColor = TextColor;

            Title = new GUIStyle(label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = false };
            Title.normal.textColor = TitleColor;

            HeadStat = new GUIStyle(label) { fontSize = 15, wordWrap = false };

            Toast = Boxed(ToastBg, ToastBorder);
            Toast.padding = new RectOffset(26, 26, 10, 10);  // 원본 `padding:10px 26px`
            Toast.fontSize = 16;
            Toast.alignment = TextAnchor.MiddleCenter;
            Toast.normal.textColor = ToastText;

            Flat = new GUIStyle();

            expBarBg = Solid(Hex(0x00, 0x00, 0x00, 0.5f));   // 원본 `background:rgba(0,0,0,.5)`
            expBarFill = Solid(ExpFill);

            Skin = Object.Instantiate(GUI.skin);
            Skin.hideFlags = HideFlags.HideAndDontSave;
            Skin.label = label;
            Skin.button = button;
            Skin.box = Panel;
            Skin.toggle = button;
        }

        /// <summary>경험치 바(`#lobbyExpbar`) — 바탕 위에 비율만큼 채운다.</summary>
        public static void DrawExpBar(Rect r, float ratio)
        {
            GUI.DrawTexture(r, expBarBg);
            float w = Mathf.Clamp01(ratio) * (r.width - 2f);
            if (w > 0f) GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, w, r.height - 2f), expBarFill);
        }

        /// <summary>
        /// 원본 `text-shadow:0 1px 3px #000`(`:85`). **판 밖에 있는 글자에만** 쓴다 —
        /// 사냥터 위에 그대로 얹히기 때문에 그림자 말고는 대비를 만들 방법이 없다.
        /// </summary>
        public static void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            Color prev = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x, r.y + 1f, r.width, r.height), text, style);
            style.normal.textColor = prev;
            GUI.Label(r, text, style);
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
