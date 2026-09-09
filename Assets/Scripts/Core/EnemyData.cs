using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 적 종류. 원본 `CONFIG.enemyBase`(project_test.html:707~714)의 키 7종 그대로.
    /// </summary>
    public enum EnemyType { Wisp, Oni, BigOni, Charger, Shooter, Splitter, Splitlet }

    /// <summary>
    /// 적 한 종류의 **수치**. 원본 `CONFIG.enemyBase[type]` 한 줄에 해당한다.
    ///
    /// CLAUDE.md "데이터(밸런스 값)" 규칙대로 **SO는 수치만 책임진다** — 행동이 다른 종류
    /// (wisp 비행 / charger 돌진 / shooter 사격 / splitter 분열)는 전용 스크립트로 나누고,
    /// 그 스크립트들이 이 데이터를 공통으로 읽는다. 반대로 `bigOni`처럼 **이동/AI는 오니와 완전히 같고
    /// 수치와 넉백 저항만 다른 종류**는 새 스크립트 없이 이 데이터만 다르게 주면 된다(원본도 그렇다 —
    /// `updateEnemies()`에 bigOni 전용 이동 분기가 없고 `:1678`의 넉백 배율만 갈린다).
    ///
    /// 에셋 위치는 `Assets/Data/Enemies/`(CLAUDE.md 규칙). 에셋은 손으로 만들지 말고
    /// `Assets/Editor/BuildEnemyData.cs`가 원본 수치대로 생성한다 — 원본과 어긋나면 그 파일을 고칠 것.
    /// </summary>
    [CreateAssetMenu(menuName = "YokaiFront/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("종류")]
        public EnemyType type = EnemyType.Oni;
        [Tooltip("표시용 이름(원본에 대응하는 우리말 이름).")]
        public string displayName = "오니";

        [Header("기본 스탯 (원본 CONFIG.enemyBase — 지역 배율은 스폰 때 곱한다)")]
        [Tooltip("기본 체력. 지역당 ×2.15가 스폰 시 곱해진다.")]
        public float maxHp = 38f;
        [Tooltip("접촉 피해. 지역당 ×1.4가 스폰 시 곱해진다.")]
        public float attackPower = 13f;
        [Tooltip("이동 속도. 원본 px/s ÷100 (오니 76 → 0.76).")]
        public float moveSpeed = 0.76f;

        [Header("처치 보상 (지역당 ×1.42가 곱해진다)")]
        public float exp = 8f;
        [Tooltip("골드 드랍 범위(포함 상한). 원본 gold: [min, max].")]
        public int goldMin = 5;
        public int goldMax = 10;

        [Header("피격 반응")]
        [Tooltip("넉백 배율. 원본 :1678 — 대오니만 0.4(덜 밀린다), 나머지는 1. 보스는 0.08.")]
        public float knockbackMultiplier = 1f;

        [Header("몸집")]
        [Tooltip("원형 콜라이더 반지름. 원본은 사각형(w×h)이라 1:1 대응이 없어서, **오니를 현재 값(0.5)에 고정**하고 나머지는 원본 높이 비율로 환산했다(예: 대오니 h=80 → 0.5×80/46≈0.87).")]
        public float colliderRadius = 0.5f;
        public Color color = new Color(0.85f, 0.2f, 0.2f);

        /// <summary>
        /// 이 종류의 그림. 원본 프로토타입의 `draw*()` 결과를 구워낸 임시 스프라이트다
        /// (`Assets/Sprites/Prototype/`, 만든 절차는 `docs/prototype-sprites.md`).
        /// **비어 있으면 프리팹의 기본 원형 스프라이트를 종류색으로 칠해서 쓴다** — 정식 아트로
        /// 갈아끼울 때 같은 이름으로 파일만 덮어쓰면 되고, 없다고 게임이 깨지지는 않는다.
        /// </summary>
        public Sprite sprite;

        [Header("원본 기록용 (환산 근거 — 로직에서 읽지 않는다)")]
        public float originalWidthPx = 42f;
        public float originalHeightPx = 46f;
    }
}
