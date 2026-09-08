using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>원본 `run.mode`(project_test.html:1471) — 일반 사냥 / 보스 스테이지.</summary>
    public enum RunMode { Normal, Boss }

    /// <summary>
    /// 지금 진행 중인 한 판(런)의 상태. 원본 전역 `run` 오브젝트(project_test.html:1470)에서
    /// **런 사이클에 필요한 부분만** 옮긴 것이다.
    ///
    /// 원본 `run`에 있지만 여기 없는 것과 그 이유:
    /// - `waveTimer`/`minionTimer`/`shrineTimer` → 스폰 주체(`Systems/EnemySpawner`, 팀원 소유)가 각자 들고 있는다.
    /// - `mapW` → 우리는 `Core/FieldBounds`가 이미 소유한다.
    /// - `chainN`/`chainT`(연쇄 처치), 아이템 런 한정 상태(`guardLeft` 등) → 해당 시스템을 만들 때 추가한다
    ///   (지금 없는 기능을 위해 미리 필드를 만들지 않는다).
    ///
    /// **팀원 세션은 이 클래스를 읽기만 한다**(`docs/worksplit.md` 공유 계약). 특히 <see cref="Region"/>으로
    /// 몹 종류(`rollSpawnType`, :3933)와 지역 스케일을 결정한다.
    /// </summary>
    public static class RunState
    {
        // 원본 CONFIG.run(project_test.html:695)
        public const float NormalTime = 90f;
        public const float BossTime = 180f;
        /// <summary>지역 토벌 목표. 원본 `CONFIG.regionKillTarget`(:699).</summary>
        public const int RegionKillTarget = 100;

        /// <summary>현재 지역 1~9. 원본 `run.region`.</summary>
        public static int Region { get; private set; } = 1;
        public static RunMode Mode { get; private set; } = RunMode.Normal;

        /// <summary>남은 시간(초). 0이 되면 원본은 `endRun('timeout')`(:4413).</summary>
        public static float TimeLeft { get; private set; }

        /// <summary>이번 런 처치 수. 원본 `run.kills` — **보스는 세지 않는다**(:1857).</summary>
        public static int Kills { get; private set; }

        /// <summary>살기 스택. 원본 `run.fury`(:1825) — 보스 포함 **모든** 처치마다 1씩. 피해 +0.8%/공속 +0.4%씩 붙는다(:702).</summary>
        public static int Fury { get; private set; }

        /// <summary>이번 런에서 번 골드/경험치(결과 화면 표시용). 원본 `run.goldEarned`/`run.expEarned`.</summary>
        public static int GoldEarned { get; private set; }
        public static int ExpEarned { get; private set; }

        /// <summary>런이 끝났는지. 원본 `run.over` — true면 갱신도 추가 피해도 멈춘다(:1881, :7169).</summary>
        public static bool Over { get; private set; }

        /// <summary>원본 `startRun`(:4293)의 런 상태 초기화 부분.</summary>
        public static void Begin(int region, RunMode mode)
        {
            Region = region;
            Mode = mode;
            TimeLeft = mode == RunMode.Normal ? NormalTime : BossTime;
            Kills = 0;
            Fury = 0;
            GoldEarned = 0;
            ExpEarned = 0;
            Over = false;
        }

        /// <summary>원본 `updateRun`의 `run.timeLeft -= dt`(:4412). 0에 닿으면 true를 돌려준다(호출자가 timeout 처리).</summary>
        public static bool Tick(float dt)
        {
            if (Over) return false;
            TimeLeft = Mathf.Max(0f, TimeLeft - dt);
            return TimeLeft <= 0f;
        }

        public static void MarkOver() => Over = true;

        /// <summary>
        /// 처치 집계. 원본은 `run.fury++`를 보스 포함 전부(:1825), `run.kills++`는 보스 제외(:1857)로
        /// 서로 다르게 센다 — 그대로 옮겼다.
        /// </summary>
        public static void RegisterKill(bool isBoss)
        {
            Fury++;
            if (!isBoss) Kills++;
        }

        /// <summary>결과 화면에 쓸 런 누적 보상. 원본 `run.goldEarned`/`run.expEarned` 누산(:1816, :1820).</summary>
        public static void RegisterReward(int gold, int exp)
        {
            GoldEarned += gold;
            ExpEarned += exp;
        }

        /// <summary>테스트 격리용(`ProfileService.Reset`과 같은 이유).</summary>
        public static void Reset() => Begin(1, RunMode.Normal);
    }
}
