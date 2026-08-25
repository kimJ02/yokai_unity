using UnityEngine;

/// <summary>
/// ⚠ 임시 스텁 (Part B가 컴파일/독립 검증용으로 추가함).
///
/// 이 파일은 원래 Part A(feature/player-field)가 소유하는 계약 파일이다 — PROGRESS.md에 정의된
/// 인터페이스: "Part A가 먼저 만들어 푸시하면 그걸 그대로 가져다 쓸 것."
///
/// main에 Part A가 먼저 병합된 뒤 이 브랜치에서 git pull origin main을 받으면
/// 이 파일은 Part A의 실제 버전(필드 경계를 실제로 계산해서 채워주는 버전)과 충돌할 것이다 —
/// 그때는 이 스텁을 버리고 Part A 버전을 그대로 쓰면 된다. 지금은 MonsterSpawner가
/// 참조하는 API 형태만 맞춰서 이 브랜치 단독으로 컴파일/테스트가 가능하게 하려고 넣어둔 것.
/// </summary>
public static class FieldBounds
{
    // TODO(확인 필요): 임시 기본값. Part A가 실제 필드 크기를 정하면 교체됨.
    public static Vector2 Min = new Vector2(-8f, -4.5f);
    public static Vector2 Max = new Vector2(8f, 4.5f);
}
