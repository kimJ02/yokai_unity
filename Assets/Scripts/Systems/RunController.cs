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

        [Header("히트스톱 (원본 CONFIG.hitstop, project_test.html:743)")]
        [Tooltip("일반 타격 시 멈추는 시간. 원본 hit 0.045.")]
        public float hitstopOnHit = 0.045f;
        [Tooltip("처치 시. 원본 kill 0.085.")]
        public float hitstopOnKill = 0.085f;

        float hitstopLeft;

        /// <summary>원본 `setTimeout(() => endRun('bossdead'), 1600)`(project_test.html:4289).</summary>
        public const float BossDeadEndDelay = 1.6f;
        float bossDeadTimer = -1f;

        void OnEnable()
        {
            CombatEvents.PlayerDied += HandlePlayerDied;
            CombatEvents.EnemyKilled += HandleEnemyKilled;
            CombatEvents.EnemyDamaged += HandleEnemyDamaged;
            CombatEvents.ShrineBuffGranted += CombatModifiers.GrantShrineBuff;
            RunEvents.BossPortalRequested += HandleBossPortalRequested;
            RunEvents.BossDefeated += HandleBossDefeated;
        }

        void OnDisable()
        {
            CombatEvents.PlayerDied -= HandlePlayerDied;
            CombatEvents.EnemyKilled -= HandleEnemyKilled;
            CombatEvents.EnemyDamaged -= HandleEnemyDamaged;
            CombatEvents.ShrineBuffGranted -= CombatModifiers.GrantShrineBuff;
            RunEvents.BossPortalRequested -= HandleBossPortalRequested;
            RunEvents.BossDefeated -= HandleBossDefeated;
        }

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

            // 보스 격파 후 결과 화면까지의 지연. **멈춘 시간(unscaled)으로 잰다** — 히트스톱이
            // 걸려 있으면 `Time.deltaTime`이 0이라 타이머가 안 줄고 결과 화면이 영영 안 뜬다.
            if (bossDeadTimer >= 0f)
            {
                bossDeadTimer -= Time.unscaledDeltaTime;
                if (bossDeadTimer <= 0f)
                {
                    bossDeadTimer = -1f;
                    EndRun(RunEndReason.BossDead);
                }
                return; // 격파 뒤엔 런 타이머가 더 흐르지 않는다(timeout으로 뒤집히면 안 된다)
            }

            // 히트스톱은 게임을 멈추는 것이므로 **멈춘 시간(unscaled)으로 재야 한다** —
            // `Time.deltaTime`으로 재면 timeScale이 0인 동안 타이머가 안 줄어 영원히 안 풀린다.
            if (hitstopLeft > 0f)
            {
                hitstopLeft -= Time.unscaledDeltaTime;
                if (hitstopLeft <= 0f) Time.timeScale = 1f;
                return; // 멈춘 동안엔 런 타이머도 안 간다(원본도 dt=0이라 같다)
            }

            // 원본 `run.timeLeft -= dt; if (<= 0) endRun('timeout')`(:4412).
            if (RunState.Tick(Time.deltaTime)) EndRun(RunEndReason.Timeout);

            // 콤보 창·연쇄 처치 창·성소 버프 감소(원본 `updateCombo` :1630 등).
            CombatModifiers.Tick(Time.deltaTime);
        }

        /// <summary>원본 `startRun(region, mode)`(project_test.html:4293).</summary>
        public void StartRun(int region, RunMode mode)
        {
            RunState.Begin(region, mode);

            // 무대를 먼저 바꾼다 — 발판 배치와 맵 폭(`FieldBounds.MaxX`)이 여기서 정해지고,
            // 아래의 플레이어 리셋·필드 정리가 그 값을 보고 좌표를 잡는다.
            // 원본도 `startRun`에서 `run.mapW`와 `platforms`를 먼저 갈아끼운다(:4313·:4316).
            YokaiFront.World.PlatformSet.Activate(mode == RunMode.Boss);

            // 원본 `enemies = []; projectiles = []; zones = []; ...`(:4318) — 지난 런의 잔해를 지운다.
            RunTransient.DestroyAll();
            CombatModifiers.ResetForRun(); // 원본 `combo.n = 0; run.chainN = 0`(:4307·:4319)
            hitstopLeft = 0f;
            bossDeadTimer = -1f;

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
            hitstopLeft = 0f; // 히트스톱 중에 멈추면 그게 timeScale을 다시 1로 되돌려버린다
            GameState.Set(GameScene.Pause);
            Time.timeScale = 0f;
        }

        /// <summary>원본 `resumeRun()`(:4346).</summary>
        public void Resume()
        {
            if (GameState.Current != GameScene.Pause) return;
            GameState.Set(GameScene.Run);
            Time.timeScale = 1f;
            hitstopLeft = 0f;
        }

        /// <summary>원본 `endRun(reason)`(:4354). 이미 끝난 런은 두 번 끝나지 않는다(`if (run.over) return`).</summary>
        public void EndRun(RunEndReason reason)
        {
            if (RunState.Over) return;
            RunState.MarkOver();
            LastEndReason = reason;
            hitstopLeft = 0f; // 히트스톱 중에 멈추면 그게 timeScale을 다시 1로 되돌려버린다
            GameState.Set(GameScene.Result);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 보스 포탈 입장 요청을 처리한다 — **원본에 없는 전환**(사용자 지시 2026-09-16).
        ///
        /// 판단은 전부 여기서 한다(포탈은 요청만 던진다, <see cref="RunEvents"/> 주석 참고):
        /// 사냥 중인지 · 이미 보스 필드가 아닌지 · 그 지역 보스가 해금됐는지.
        /// </summary>
        void HandleBossPortalRequested()
        {
            if (GameState.Current != GameScene.Run) return;
            if (!ProfileService.Current.IsBossUnlocked(RunState.Region)) return;
            EnterBossField();
        }

        /// <summary>
        /// 일반 필드 → 보스 필드. 무대·타이머·필드 정리·플레이어 위치를 한 번에 바꾼다.
        /// <see cref="StartRun"/>과 같은 순서를 쓰는 이유는 거기 주석에 있다(무대를 먼저 바꿔야
        /// `FieldBounds.MaxX`가 갱신되고, 그 뒤 좌표 계산이 맞는다).
        /// </summary>
        /// <returns>실제로 전환됐으면 true.</returns>
        public bool EnterBossField()
        {
            if (!RunState.EnterBossField()) return false;

            YokaiFront.World.PlatformSet.Activate(true);

            // 일반 필드의 잡몹·투사체·장판을 지운다. 안 지우면 좁아진 보스 무대에 이전 필드의
            // 몹이 맵 밖 좌표로 남아, 플레이어가 닿을 수 없는 곳에서 계속 살아 있다.
            RunTransient.DestroyAll();

            ResetPlayer(); // 좁아진 무대의 시작 지점으로
            RunEvents.RaiseBossFieldEntered(); // 스포너가 보스를 세운다
            return true;
        }

        /// <summary>
        /// 보스 격파. 원본은 **1.6초 뒤에** 런을 끝낸다(`:4289`) — 즉사 연출과 보상 표시를
        /// 볼 시간을 준다. 즉시 결과 화면을 띄우면 무엇을 잡았는지 보이지 않는다.
        /// </summary>
        void HandleBossDefeated() => bossDeadTimer = BossDeadEndDelay;

        /// <summary>원본 `toLobby()`(:4382).</summary>
        public void EnterLobby()
        {
            YokaiFront.World.PlatformSet.Activate(false); // 로비에선 일반 무대로 되돌린다
            RunTransient.DestroyAll(); // 결과 화면을 거치지 않고 나가도 필드가 남지 않게
            bossDeadTimer = -1f;
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

        void HandleEnemyKilled(GameObject enemy)
        {
            Hitstop(hitstopOnKill);

            // 원본 `onBossKilled`은 1.6초 뒤에 `endRun('bossdead')`를 부른다(project_test.html:4289) —
            // 격파 연출을 볼 틈을 주려는 것이다. 우리는 연출이 없지만 즉시 결과창이 뜨면
            // 마지막 타격이 안 보여서 같은 지연을 둔다.
            if (enemy != null && enemy.GetComponent<YokaiFront.Enemies.Boss>() != null)
                Invoke(nameof(EndRunAfterBoss), BossClearDelay);
        }

        /// <summary>원본 `setTimeout(..., 1600)`(project_test.html:4289).</summary>
        public const float BossClearDelay = 1.6f;

        void EndRunAfterBoss() => EndRun(RunEndReason.BossDead);
        void HandleEnemyDamaged(GameObject _) => Hitstop(hitstopOnHit);

        /// <summary>
        /// 타격감용 순간 정지 — 원본 `state.freeze = Math.max(state.freeze, ...)`(project_test.html:1685).
        /// **`timeScale`을 만지는 유일한 주체가 이 컨트롤러**라서 여기 둔다. 다른 데서 `timeScale`을
        /// 건드리면 일시정지·결과 화면과 서로 덮어써서 게임이 안 멈추거나 안 풀린다.
        ///
        /// 사냥 중이 아닐 땐 무시한다 — 로비/결과에서 걸리면 그 화면이 `timeScale = 1`로 풀려버린다.
        /// </summary>
        public void Hitstop(float duration)
        {
            if (GameState.Current != GameScene.Run || RunState.Over) return;
            hitstopLeft = Mathf.Max(hitstopLeft, duration); // 원본도 max로 겹친다
            Time.timeScale = 0f;
        }
    }
}
