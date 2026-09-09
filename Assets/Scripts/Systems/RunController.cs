using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Systems
{
    /// <summary>
    /// 게임의 뼈대 — **로비 ↔ 사냥 ↔ 일시정지 ↔ 결과**를 오가게 하는 주체.
    /// 원본 `startRun`(:4293) / `pauseRun`(:4332) / `resumeRun`(:4346) / `endRun`(:4354) /
    /// `toLobby`(:4382)를 한 컴포넌트로 옮긴 것이다.
    ///
    /// 상태 자체는 <see cref="GameState"/>·<see cref="RunState"/>가 이미 들고 있고, 이 클래스는
    /// **전이를 실행**한다(필드 정리·플레이어 리셋·시간 정지).
    ///
    /// ## 왜 `Time.timeScale`로 멈추는가 (확정 설계, docs/sprints/04-run-cycle.md 3번)
    /// 원본은 게임 전체가 한 개의 `frame()` 루프라 `state.scene !== 'run'`이면 `dt = 0`을 줘서
    /// **한 곳에서** 전부 멈춘다(`:7169`). 우리는 컴포넌트마다 `Update()`가 따로 돌아 그게 안 되므로
    /// `Time.timeScale = 0`으로 물리와 `Time.deltaTime`을 통째로 멈춘다 — 같은 효과다.
    ///
    /// **덕분에 적·스폰너·투사체·장판에 `IsRunning` 가드를 하나도 안 넣어도 된다.**
    /// (`EnemySpawner.waveTimer -= Time.deltaTime`이 0을 빼게 되므로 로비에서 몹이 안 나온다.)
    /// 대신 이 컨트롤러와 UI만 `timeScale`에 영향받지 않아야 한다 — `Update()`는 `timeScale`이 0이어도
    /// 계속 돌기 때문에 여기서 ESC를 읽는 건 문제없다.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunController : MonoBehaviour
    {
        [Header("시작 설정")]
        [Tooltip("로비에서 '사냥 시작'을 눌렀을 때 들어갈 지역. 정식 지역 선택은 로비 스테이지 탭이 맡는다.")]
        public int defaultRegion = 1;

        /// <summary>마지막 런이 끝난 이유. 결과 화면이 제목을 고르는 데 쓴다(원본 `endRun(reason)`).</summary>
        public RunEndReason LastEndReason { get; private set; } = RunEndReason.Timeout;

        void OnEnable() => CombatEvents.PlayerDied += HandlePlayerDied;
        void OnDisable() => CombatEvents.PlayerDied -= HandlePlayerDied;

        void Start()
        {
            // 원본도 로비에서 시작한다(`state.scene`의 초기값 'lobby', :1466).
            EnterLobby();
        }

        void Update()
        {
            // ⚠️ `timeScale = 0`이어도 `Update`는 계속 돈다 — 멈추는 건 `Time.deltaTime`과 물리다.
            // 그래서 일시정지 중에도 ESC를 읽을 수 있다.
            if (GameInput.PauseDown)
            {
                if (GameState.Current == GameScene.Run) Pause();
                else if (GameState.Current == GameScene.Pause) Resume();
            }

            if (GameState.Current != GameScene.Run) return;

            // 원본 `run.timeLeft -= dt; if (<= 0) endRun('timeout')`(:4412).
            if (RunState.Tick(Time.deltaTime)) EndRun(RunEndReason.Timeout);
        }

        /// <summary>원본 `startRun(region, mode)`(project_test.html:4293).</summary>
        public void StartRun(int region, RunMode mode)
        {
            RunState.Begin(region, mode);

            // 원본 `enemies = []; projectiles = []; zones = []; ...`(:4318) — 지난 런의 잔해를 지운다.
            RunTransient.DestroyAll();

            ResetPlayer();

            GameState.Set(GameScene.Run);
            Time.timeScale = 1f;
        }

        /// <summary>로비에서 기본 지역으로 바로 시작(임시 진입점 — 정식 지역 선택은 로비 스테이지 탭).</summary>
        public void StartRun() => StartRun(defaultRegion, RunMode.Normal);

        /// <summary>원본 `pauseRun()`(:4332) — 사냥 중에만, 그리고 이미 끝난 런에선 안 걸린다.</summary>
        public void Pause()
        {
            if (RunState.Over || GameState.Current != GameScene.Run) return;
            GameState.Set(GameScene.Pause);
            Time.timeScale = 0f;
        }

        /// <summary>원본 `resumeRun()`(:4346).</summary>
        public void Resume()
        {
            if (GameState.Current != GameScene.Pause) return;
            GameState.Set(GameScene.Run);
            Time.timeScale = 1f;
        }

        /// <summary>원본 `endRun(reason)`(:4354). 이미 끝난 런은 두 번 끝나지 않는다(`if (run.over) return`).</summary>
        public void EndRun(RunEndReason reason)
        {
            if (RunState.Over) return;
            RunState.MarkOver();
            LastEndReason = reason;
            GameState.Set(GameScene.Result);
            Time.timeScale = 0f;
        }

        /// <summary>원본 `toLobby()`(:4382).</summary>
        public void EnterLobby()
        {
            RunTransient.DestroyAll(); // 결과 화면을 거치지 않고 나가도 필드가 남지 않게
            GameState.Set(GameScene.Lobby);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 원본 `resetPlayerForRun()`(:1511). 구체 타입을 모르고 <see cref="IRunResettable"/>만 보고 부른다
        /// — `Systems`(3층)가 `Characters`(2층)의 클래스를 직접 알 필요가 없다(CLAUDE.md 계층 규칙).
        /// 섬영·드루이드 키트도 이 인터페이스만 구현하면 자동으로 같이 초기화된다.
        /// </summary>
        void ResetPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            foreach (var resettable in player.GetComponents<IRunResettable>())
                resettable.ResetForRun();
        }

        void HandlePlayerDied() => EndRun(RunEndReason.Dead); // 원본 :1927
    }
}
