# UI 레이어 (asmdef 4층)

HUD·로비·결과 화면. CLAUDE.md 계층표의 **최상위 층**이라 0~3층(`Core`/`World`·`Combat`/
`Characters`·`Enemies`/`Systems`)을 전부 참조할 수 있고, **아무도 이 층을 참조하지 않는다.**

그래서 게임 로직을 여기에 두면 안 된다 — UI는 아래 층의 상태를 **읽어서 그리고**,
버튼을 누르면 아래 층의 **공개 메서드를 부르기만** 한다.
(예: 사냥 시작은 여기서 계산하지 않고 `Systems/RunController.StartRun()`을 부른다.)
