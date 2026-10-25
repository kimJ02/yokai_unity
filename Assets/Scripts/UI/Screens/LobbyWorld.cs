using UnityEngine;
using UnityEngine.UI;
using YokaiFront.Core;
using YokaiFront.World;

namespace YokaiFront.UI
{
    /// <summary>
    /// 걸어 다니는 로비 — Figma "Page 1"(로비 뼈대): 땅 위에 건물 넷(환생 · 가챠 · 훈련소 · 던전), 캐릭터가 걸어가서 들어간다
    /// (사용자 2026-10-24 "ㅇㅇ 바꾸고"). 원본·예전 로비는 탭 화면이었다.
    ///   - 환생 → 시간 회귀 · 가챠 → 유물 개방 · 훈련소 → 각성창(전문화·원정 강화) · 던전 → 스테이지 선택.
    ///   - **로비는 지금처럼 시간이 멈춘 채(timeScale 0)** 캐릭터만 실제 시간으로 좌우로 걷는다 — 공격·몹·스포너가 전부 "로비에선 시간이
    ///     멈춘다"는 전제로 돌아가서(로비에서 Z를 눌러도 아무것도 안 나가게), 물리 이동 대신 위치만 옮긴다. 점프는 없다.
    ///   - 건물 그림이 아직 없어 Figma 공용 창 틀에 이름·아이콘을 붙인 자리표시로 세웠다(뼈대의 회색 상자 자리).
    ///   - 발판은 로비에서 숨긴다(사냥을 시작하면 다시 켜진다). 들어가기: 건물 앞에서 F 또는 ↑.
    /// </summary>
    public class LobbyWorld : MonoBehaviour
    {
        public struct Building { public string Name, Icon; public float X; public System.Action Open; }

        /// <summary>건물 자리(필드 0~26유닛) — 뼈대의 왼쪽→오른쪽 순서.</summary>
        public static readonly Building[] Buildings =
        {
            new Building { Name = "환생", Icon = "Icons/shard", X = 4.5f, Open = RegressionScreen.Open },
            new Building { Name = "가챠", Icon = "Icons/shard_dark", X = 9.5f, Open = RelicScreen.Open },
            new Building { Name = "훈련소", Icon = "Hud/icon_attack", X = 15f, Open = AwakenScreen.Open },
            new Building { Name = "던전", Icon = "Hud/boss_warning", X = 20.5f, Open = StageSelectScreen.Open },
        };

        public const float StartX = 2f;
        /// <summary>이만큼 가까우면 건물 앞이다(유닛).</summary>
        public const float EnterRange = 1.1f;
        const float WorldPx = 1f / 128f; // 그림 1도트 = 월드 1/128유닛(C안)

        public static LobbyWorld Instance { get; private set; }
        public int NearIndex { get; private set; } = -1;

        GameObject root;
        Transform player;
        float walkSpeed = 2.7f;
        bool wasLobby;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (root != null) Destroy(root);
        }

        void Update()
        {
            bool lobby = GameState.Current == GameScene.Lobby;
            if (lobby && !wasLobby) EnterLobby();
            if (!lobby && wasLobby && root != null) root.SetActive(false);
            wasLobby = lobby;
            if (!lobby) { NearIndex = -1; return; }

            FindPlayer();
            if (player == null) return;
            bool windowOpen = UiModal.Open;
            if (!windowOpen)
            {
                int dir = (GameInput.Right ? 1 : 0) - (GameInput.Left ? 1 : 0);
                if (dir != 0)
                {
                    var pos = player.position;
                    pos.x = Mathf.Clamp(pos.x + dir * walkSpeed * Time.unscaledDeltaTime, FieldBounds.MinX + 0.3f, FieldBounds.MaxX - 0.3f);
                    player.position = pos;
                    if (player.TryGetComponent(out Rigidbody2D rb)) rb.position = pos; // 다음 물리 단계가 되돌리지 않게
                }
            }
            NearIndex = Nearest(player.position.x);
            if (!windowOpen && NearIndex >= 0 && (GameInput.InteractDown || GameInput.MenuUpDown)) Buildings[NearIndex].Open();
        }

        /// <summary>그 자리에서 들어갈 수 있는 건물(없으면 -1).</summary>
        public static int Nearest(float x)
        {
            int best = -1;
            float bestD = EnterRange;
            for (int i = 0; i < Buildings.Length; i++)
            {
                float d = Mathf.Abs(Buildings[i].X - x);
                if (d <= bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void FindPlayer()
        {
            if (player != null) return;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
            if (go.TryGetComponent(out YokaiFront.Characters.CharacterMover2D mover)) walkSpeed = mover.moveSpeed;
        }

        /// <summary>로비에 들어올 때 — 발판을 숨기고, 건물을 세우고, 캐릭터를 시작 자리 땅 위에.</summary>
        public void EnterLobby()
        {
            Instance = this; // 편집 모드 캡처에선 Awake가 안 불린다
            PlatformSet.HideAll();
            if (root == null) Build();
            root.SetActive(true);
            FindPlayer();
            if (player != null)
            {
                var pos = new Vector3(StartX, FieldBounds.GroundY + EntitySizeConfig.PlayerRadius, player.position.z);
                player.position = pos;
                if (player.TryGetComponent(out Rigidbody2D rb)) { rb.position = pos; rb.linearVelocity = Vector2.zero; }
            }
        }

        void Build()
        {
            root = new GameObject("LobbyBuildings");
            foreach (var b in Buildings) BuildBuilding(b);
            root.SetActive(false);
        }

        void BuildBuilding(Building b)
        {
            // 월드 캔버스 하나 = 건물 하나. 1캔버스 단위 = 그림 1도트 = 1/128유닛이라 게임 도트와 같은 크기로 보인다.
            var go = new GameObject("Building_" + b.Name, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = -50; // 배경(-100) 위, 땅(-5)·캐릭터 아래
            var rt = (RectTransform)go.transform;
            const float w = 240f, h = 272f;
            rt.sizeDelta = new Vector2(w, h);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.localScale = Vector3.one * WorldPx;
            rt.position = new Vector3(b.X, FieldBounds.GroundY, 0f);

            Px.Image(rt, "Facade", PxArt.Get("Stage/frame_info"), 0f, 0f, w, h);
            Px.Text(rt, "Name", b.Name, 0f, 18f, w, 40f, 32, Color.white).WithOutline(Px.Ink, 2f);
            Px.Image(rt, "Icon", PxArt.Get(b.Icon), w / 2f - 48f, 90f, 96f, 96f);
            Px.Text(rt, "Door", "F / ↑", 0f, 210f, w, 30f, 20, Px.Hex(0xfac70e)).WithOutline(Px.Ink, 2f);
        }
    }
}
