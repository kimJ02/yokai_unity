using UnityEngine;

/// <summary>
/// 카메라가 플레이어의 X만 따라간다(Y는 고정). 원본은 mapW 2600px(26유닛)짜리 옆으로 긴
/// 레벨이라 HANDOFF.md가 처음 가정한 "필드 전체가 한 화면에 고정" 카메라로는
/// 발판/플레이어가 너무 작아져서 실질적으로 원본과 달라진다 — 실제 발판을 넣으면서
/// 스크롤 카메라로 바꾼 의도적 변경이다(PROGRESS.md 로그 참고).
/// 모든 발판이 세로로는 한 화면에 들어오는 높이라 Y까지 따라갈 필요는 없어서 X만 따라간다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;

    float halfWidth;
    float velocityX;

    void Start()
    {
        var cam = GetComponent<Camera>();
        halfWidth = cam.orthographicSize * cam.aspect;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float min = FieldBounds.MinX + halfWidth;
        float max = FieldBounds.MaxX - halfWidth;
        float desiredX = (min <= max) ? Mathf.Clamp(target.position.x, min, max) : (FieldBounds.MinX + FieldBounds.MaxX) * 0.5f;

        Vector3 p = transform.position;
        p.x = Mathf.SmoothDamp(p.x, desiredX, ref velocityX, smoothTime);
        transform.position = p;
    }
}
