namespace YokaiFront.Core
{
    /// <summary>
    /// 현재 플레이어 프로필에 대한 정적 접근점 — `FieldBounds`와 같은 패턴. 씬에서 오브젝트를
    /// 찾아다니지 않고 어느 도메인에서든 `ProfileService.Current`로 바로 읽고 쓸 수 있다.
    ///
    /// 세이브/로드(파일 입출력)는 이번 스프린트 범위 밖(`HANDOFF_sprint2_draft.md` "범위 밖" 참고,
    /// "인메모리 상태로 충분") — 그래서 지금은 시작할 때 기본값 하나만 만들어두는 게 전부다.
    /// 나중에 세이브 시스템(`Systems/SaveSystem`)이 생기면 여기서 로드한 인스턴스로 교체한다.
    /// </summary>
    public static class ProfileService
    {
        public static PlayerProfile Current = new PlayerProfile();
    }
}
