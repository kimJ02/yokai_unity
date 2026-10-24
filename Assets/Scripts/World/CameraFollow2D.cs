using UnityEngine;
using UnityEngine.Rendering.Universal;
using YokaiFront.Core;

namespace YokaiFront.World
{
    /// <summary>
    /// 카메라가 플레이어를 따라간다. 원본은 mapW 2600px(26유닛)짜리 옆으로 긴
    /// 레벨이라 스프린트 1 스펙(docs/sprints/01-combat-core.md)이 처음 가정한 "필드 전체가 한 화면에 고정" 카메라로는
    /// 발판/플레이어가 너무 작아져서 실질적으로 원본과 달라진다 — 실제 발판을 넣으면서
    /// 스크롤 카메라로 바꾼 의도적 변경이다(PROGRESS.md 로그 참고).
    ///
    /// **2026-10-24 C안**(`StageArtConfig`): Figma 도트를 선명하게 보이려고 월드를 960×540px(1유닛 = 128px) 그대로
    /// 그려서 정수배로 키운다(`PixelPerfectCamera`). 한 화면에 발판이 두 층 정도만 들어와서 **Y도 따라간다** —
    /// 예전엔 모든 발판이 세로로 한 화면에 들어와서 X만 따라갔다.
    /// Y는 서 있을 때만 그 높이로 맞추고, 공중에선 화면 밖으로 나가려 할 때만 움직인다(점프마다 화면이 출렁이지 않게).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.15f;

        float velocityX;
        float velocityY;
        float followY;
        bool snapped;
        Rigidbody2D targetBody;

        void Awake()
        {
            var cam = GetComponent<Camera>();
            var ppc = GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = StageArtConfig.PixelsPerUnit;
            ppc.refResolutionX = StageArtConfig.RefWidth;
            ppc.refResolutionY = StageArtConfig.RefHeight;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill; // 어떤 창 크기에서도 보이는 범위는 960×540px 그대로
            cam.orthographicSize = StageArtConfig.ViewHalfHeight;     // 첫 프레임용 — 이후엔 PPC가 같은 값으로 맞춘다
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (targetBody == null) targetBody = target.GetComponent<Rigidbody2D>();

            float halfWidth = StageArtConfig.ViewHalfWidth;
            float min = FieldBounds.MinX + halfWidth;
            float max = FieldBounds.MaxX - halfWidth;
            float desiredX = (min <= max) ? Mathf.Clamp(target.position.x, min, max) : (FieldBounds.MinX + FieldBounds.MaxX) * 0.5f;

            float feetY = target.position.y - EntitySizeConfig.PlayerRadius;
            bool standing = targetBody == null || Mathf.Abs(targetBody.linearVelocity.y) < 0.05f;
            if (!snapped) followY = DesiredY(feetY);
            followY = NextFollowY(followY, feetY, standing);

            Vector3 p = transform.position;
            if (!snapped)
            {
                p.x = desiredX;
                p.y = followY;
                snapped = true;
            }
            else
            {
                p.x = Mathf.SmoothDamp(p.x, desiredX, ref velocityX, smoothTime);
                p.y = Mathf.SmoothDamp(p.y, followY, ref velocityY, StageArtConfig.CameraSmoothTimeY);
            }
            transform.position = p;
        }

        /// <summary>카메라 중심 높이의 아래·위 한계 — 땅에 설 때가 아래 끝, 가장 높은 발판에 설 때가 위 끝.</summary>
        public static float MinY => FieldBounds.GroundY + StageArtConfig.CameraYOffset;

        public static float MaxY
        {
            get
            {
                float top = FieldBounds.GroundY;
                var p = FieldLayout.Platforms;
                for (int i = 0; i < p.GetLength(0); i++) top = Mathf.Max(top, FieldLayout.PlatformTopY(i));
                return top + StageArtConfig.CameraYOffset;
            }
        }

        /// <summary>그 발 높이에 서 있을 때 카메라 중심 높이.</summary>
        public static float DesiredY(float feetY) => Mathf.Clamp(feetY + StageArtConfig.CameraYOffset, MinY, MaxY);

        /// <summary>
        /// 다음 목표 높이. 서 있으면 그 높이로, 공중이면 머리가 화면 위 여유(<see cref="StageArtConfig.CameraAirTopMargin"/>)를
        /// 넘거나 발이 화면 아래 여유 밖으로 떨어질 때만 그만큼 옮긴다.
        /// </summary>
        public static float NextFollowY(float followY, float feetY, bool standing)
        {
            if (standing) return DesiredY(feetY);

            float head = feetY + EntitySizeConfig.PlayerHeight;
            float top = followY + StageArtConfig.ViewHalfHeight - StageArtConfig.CameraAirTopMargin;
            float bottom = followY - StageArtConfig.ViewHalfHeight + StageArtConfig.CameraAirBottomMargin;
            if (head > top) followY += head - top;
            else if (feetY < bottom) followY -= bottom - feetY;
            return Mathf.Clamp(followY, MinY, MaxY);
        }
    }
}
