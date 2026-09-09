using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 도메인을 가로지르는 전투 신호. asmdef 계층 규칙상 `Enemies`(2층)와 `Characters`(2층)는 서로를
    /// 직접 참조할 수 없어서(CLAUDE.md 계층표), "적 쪽에서 일어난 일 → 플레이어 쪽 반응"은 전부 여기를 거친다.
    /// `IDamageable`/`IElementAfflictable`이 "누가 누구를 때리나"를 인터페이스로 푼 것과 같은 방식이고,
    /// 이건 **대상이 정해지지 않은 알림**이라 인터페이스 대신 이벤트로 둔다.
    ///
    /// 발행/구독 담당은 `docs/worksplit.md` 공유 계약 참고.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>
        /// 적이 죽었을 때. 원본 `killEnemy`(project_test.html:1793)에서 파생되는 것들
        /// — 콤보 적립(:1681), 살기(:1825), 연쇄 처치(:1829), 경험치 구슬(:1860) — 이 구독한다.
        /// </summary>
        public static event System.Action<GameObject> EnemyKilled;

        /// <summary>
        /// 성소를 부숴서 플레이어가 받는 버프의 지속시간(초). 원본 `killEnemy`의 성소 분기
        /// (`player.shrineBuffT = CONFIG.shrine.buffDur`, :1798)에 대응 — 발행은 성소(팀원),
        /// 수신은 플레이어 배수 계산(나).
        /// </summary>
        public static event System.Action<float> ShrineBuffGranted;

        /// <summary>
        /// 처치 보상이 실제로 지급된 뒤, 결과 화면 집계용으로 알린다(원본 `run.goldEarned`/`run.expEarned` 누산).
        /// 지급 자체는 지금처럼 `ProfileService.Current.AddGold/AddExp`가 하고, 이 이벤트는 **알림만** 한다.
        /// </summary>
        public static event System.Action<int, int> RewardGranted;

        /// <summary>
        /// 플레이어가 죽었을 때. 원본 `damagePlayer`가 체력이 0이 되면 바로 `endRun('dead')`를 부르는
        /// 자리(project_test.html:1927)에 대응한다.
        ///
        /// `Systems`(3층)가 `Characters`(2층)를 직접 참조할 수 없어서(계층 규칙) `RunController`는
        /// `PlayerHealth.Died`를 직접 구독하지 못한다 — 그래서 이 이벤트를 거친다.
        /// </summary>
        public static event System.Action PlayerDied;

        /// <summary>
        /// 적이 **직접 타격**을 받았을 때(지속 피해 틱 제외). 히트스톱이 이걸 구독한다 —
        /// 원본도 히트스톱만 틱을 빼고 콤보 등 나머지는 틱에도 적용한다(:1686).
        /// </summary>
        public static event System.Action<GameObject> EnemyDamaged;

        public static void RaiseEnemyKilled(GameObject enemy) => EnemyKilled?.Invoke(enemy);
        public static void RaiseEnemyDamaged(GameObject enemy) => EnemyDamaged?.Invoke(enemy);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseShrineBuffGranted(float duration) => ShrineBuffGranted?.Invoke(duration);
        public static void RaiseRewardGranted(int gold, int exp) => RewardGranted?.Invoke(gold, exp);

        /// <summary>
        /// 테스트 격리용. 정적 이벤트라 구독을 안 끊으면 파괴된 오브젝트로 콜백이 날아가고
        /// 테스트끼리 오염된다(`ProfileService.Reset`/`GameState.Reset`과 같은 이유).
        /// </summary>
        public static void Reset()
        {
            EnemyKilled = null;
            EnemyDamaged = null;
            PlayerDied = null;
            ShrineBuffGranted = null;
            RewardGranted = null;
        }
    }
}
