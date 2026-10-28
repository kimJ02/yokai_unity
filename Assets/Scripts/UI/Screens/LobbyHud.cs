using UnityEngine;
using UnityEngine.UI;
using YokaiFront.Characters;
using YokaiFront.Core;

namespace YokaiFront.UI
{
    /// <summary>
    /// 로비 화면 위 표시 — Figma 로비 뼈대("Page 1")의 자리대로: 위 가운데 **멸망까지 남은 기간**(사용자 결정 7일)과 다음 원정 지역 이름,
    /// 왼쪽 위 캐릭터 정보, 왼쪽 아래 버튼 셋(캐릭터 · 업적 · 설정), 오른쪽 아래 미니맵(건물·캐릭터 자리), 맨 아래 경험치 바(전투 HUD와 같은 색).
    /// 건물 앞에 서면 "F / ↑ — 들어가기" 안내. 예전 IMGUI 로비(탭 화면)는 이 화면이 있으면 안 그린다.
    /// </summary>
    public class LobbyHud : PxScreen
    {
        protected override PixelCanvas.Layer Layer => PixelCanvas.Layer.Hud;
        protected override bool WantsVisible => GameState.Current == GameScene.Lobby;
        protected override bool TakesKeys => false;

        static readonly Color Gold = Px.Hex(0xfac70e);
        static readonly Color Plate = new Color(0f, 0f, 0f, 0.38f);
        static readonly Color ExpBorder = Px.Hex(0x472e17), ExpBack = Px.Hex(0x57306e), ExpFill = Px.Hex(0x44ff00);
        static readonly Rect Minimap = new Rect(504f, 312f, 128f, 32f);

        Text doom, region, who, money, prompt;
        Image expFill, playerDot;
        static readonly string[] CharacterNames = { "마법사", "메카닉", "섬영", "드루이드" };

        protected override void Build()
        {
            Px.Fill(Content, "DoomPlate", Plate, 190f, 4f, 260f, 38f);
            doom = Px.Text(Content, "Doom", "", 190f, 6f, 260f, 20f, 16, Gold).WithOutline(Px.Ink);
            region = Px.Text(Content, "Region", "", 190f, 26f, 260f, 14f, 10, Color.white);

            Px.Fill(Content, "InfoPlate", Plate, 6f, 6f, 176f, 38f);
            who = Px.Text(Content, "Who", "", 12f, 8f, 168f, 18f, 12, Color.white, TextAnchor.MiddleLeft);
            money = Px.Text(Content, "Money", "", 12f, 26f, 168f, 14f, 10, Gold, TextAnchor.MiddleLeft);

            QuickButton(0, "캐릭터", CharacterSelectScreen.Open);
            QuickButton(1, "업적", AchievementsScreen.Open);
            QuickButton(2, "설정", SettingsWindow.Open);

            Px.Fill(Content, "MinimapPlate", Plate, Minimap.x, Minimap.y, Minimap.width, Minimap.height);
            Px.Fill(Content, "MinimapGround", Px.Ink, Minimap.x + 2f, Minimap.y + 24f, Minimap.width - 4f, 1f);
            for (int i = 0; i < LobbyWorld.Buildings.Length; i++)
                Px.Fill(Content, "MinimapBuilding", Px.Hex(0xc7cfdd), Minimap.x + MapX(LobbyWorld.XOf(i)) - 2f, Minimap.y + 16f, 5f, 8f);
            playerDot = Px.Fill(Content, "MinimapPlayer", Color.white, 0f, 0f, 2f, 2f);

            prompt = Px.Text(Content, "Prompt", "", 170f, 292f, 300f, 14f, 10, Gold).WithOutline(Px.Ink);

            Px.Fill(Content, "ExpBorder", ExpBorder, 0f, 356f, 640f, 4f);
            Px.Fill(Content, "ExpBack", ExpBack, 1f, 357f, 638f, 2f);
            expFill = Px.Fill(Content, "ExpFill", ExpFill, 1f, 357f, 0f, 2f);
        }

        void QuickButton(int i, string label, System.Action onClick)
        {
            var b = Px.TextButton(Content, "Quick" + i, PxArt.Sliced("Common/button", 4), 8f + i * 58f, 326f, 54f, 24f,
                                  label, 10, Color.white, onClick);
            b.Art.type = Image.Type.Sliced;
        }

        static float MapX(float worldX)
        {
            float span = Mathf.Max(0.01f, FieldBounds.MaxX - FieldBounds.MinX);
            return Mathf.Round((worldX - FieldBounds.MinX) / span * (Minimap.width - 4f)) + 2f;
        }

        protected override void Refresh()
        {
            var p = ProfileService.Current;
            doom.text = $"멸망까지 남은 기간 {p.doomDays}일";
            int s = RegressionScreen.HighestStage(p);
            region.text = $"다음 원정 stage {s} · {RegionConfig.NameOf(s)}";
            int id = (int)p.character;
            who.text = $"{(id < CharacterNames.Length ? CharacterNames[id] : "?")}  Lv.{p.level}  ·  SP {p.SpAvailable}";
            money.text = $"{p.gold:N0} G  ·  시간의 파편 {p.shards:N0}";

            long need = PlayerProfile.RequiredExp(p.level);
            float ratio = need > 0 ? Mathf.Clamp01(p.exp / (float)need) : 0f;
            ((RectTransform)expFill.transform).sizeDelta = new Vector2(Mathf.Round(ratio * 638f), 2f);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                ((RectTransform)playerDot.transform).anchoredPosition =
                    new Vector2(Minimap.x + MapX(player.transform.position.x) - 1f, -(Minimap.y + 22f));

            var world = LobbyWorld.Instance;
            int near = world != null ? world.NearIndex : -1;
            prompt.enabled = near >= 0 && !UiModal.Open;
            if (prompt.enabled) prompt.text = $"F / ↑ — {LobbyWorld.Buildings[near].Name} 들어가기";
        }
    }
}
