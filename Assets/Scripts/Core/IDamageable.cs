using UnityEngine;

namespace YokaiFront.Core
{
    /// <summary>
    /// 피해를 받을 수 있는 대상(플레이어·적). 원본 `dealDamage()`(적)/`damagePlayer()`(플레이어)에 대응한다.
    ///
    /// 이 인터페이스가 Core에 있는 이유는 `ISpawnProtectable`과 같다 — 공격 스크립트(`Combat`/`Characters`)가
    /// 맞는 대상의 구체 타입(`Enemies`의 클래스 등)을 몰라도 되게 하려는 것이다. asmdef 계층 규칙상
    /// `Characters`↔`Enemies`는 서로 참조할 수 없으므로, 서로 때리는 건 태그(`CompareTag`)로 대상을 고르고
    /// 이 인터페이스로 피해만 넘기는 방식으로만 가능하다(CLAUDE.md asmdef 계층표 참고).
    ///
    /// 원본과 달리 "누가 때렸는지"를 `source`로 넘기는 이유: 넉백/피격 반동 방향이 가해자 위치 기준이다
    /// (원본은 전역 `player`를 직접 참조했지만, 우리는 전역이 없으므로 명시적으로 전달한다).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 피해를 적용한다. 이미 계산이 끝난 최종 피해량을 받는다(치명타·난수 변동은 공격 쪽에서 이미 적용됨 —
        /// 원본 `dealDamage`가 공격자 스탯으로 먼저 계산한 뒤 `e.hp -= dmg`를 하는 것과 같은 순서).
        /// </summary>
        /// <param name="amount">최종 피해량(1 이상 정수 권장).</param>
        /// <param name="source">가해자 오브젝트. 넉백 방향 계산에 쓰인다. 없으면 null 허용.</param>
        void TakeDamage(float amount, GameObject source);

        /// <summary>이미 죽었는지. 원본 `e.dead` — 죽은 대상에 중복으로 피해가 들어가지 않게 공격 쪽에서 확인한다.</summary>
        bool IsDead { get; }
    }
}
