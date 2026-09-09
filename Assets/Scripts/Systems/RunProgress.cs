using YokaiFront.Core;

namespace YokaiFront.Systems
{
    /// <summary>
    /// 난이도 스케일링 대리 지표 "가상 지역 레벨"(`docs/sprints/03-growth-curve-worksplit.md` 트랙 A 1번,
    /// `docs/sprints/03-growth-curve.md` 5번). 실제 9지역(진행 UI·경계·입장료)은 이번 스프린트 범위 밖이라,
    /// 원본 `regionKillTarget`(project_test.html:699)만 그대로 가져와 누적 처치 수로 대체한다.
    /// 트리거를 "누적 처치 수"로 할지 "생존 시간"으로 할지는 팀 논의로 확정된 사항(2026-09-08,
    /// PROGRESS.md 로그) — 시간/레벨 기준은 원본에 없는 설계라 기각됐다.
    ///
    /// 기준 상수(`KillsPerRegionLevel`)는 여기 두지 않고 `Core/DifficultyScalingConfig`에 모아둔다
    /// (2026-09-08 "0. 착수 전 필독" 추가 — 숫자는 전부 그 파일 하나로).
    ///
    /// `FieldBounds`와 같은 패턴의 정적 클래스 — 씬에서 오브젝트를 찾아다닐 필요 없이
    /// `RunProgress.RegionLv`로 어디서든 바로 읽는다.
    /// </summary>
    public static class RunProgress
    {
        public static int TotalKills { get; private set; }

        /// <summary>1부터 시작(원본 지역은 1지역부터). 100마리째 처치 직후 2로 올라간다.</summary>
        public static int RegionLv => 1 + TotalKills / DifficultyScalingConfig.KillsPerRegionLevel;

        /// <summary>몹 1마리 처치 시 호출(`EnemySpawner.HandleEnemyDied` 참고).</summary>
        public static void RegisterKill() => TotalKills++;

        /// <summary>
        /// 새 런 시작 시 초기화용. 세이브/로드가 이번 스프린트 범위 밖이라 지금은 호출하는 곳이
        /// 없지만(인메모리 상태로 충분, `docs/sprints/03-growth-curve.md` "범위 밖" 참고), PlayMode 테스트가
        /// 정적 상태를 테스트 간에 격리하는 데도 쓴다.
        /// </summary>
        public static void Reset() => TotalKills = 0;
    }
}
