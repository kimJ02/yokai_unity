using UnityEngine;
using YokaiFront.Core;

namespace YokaiFront.Enemies
{
    /// <summary>
    /// 분열귀(splitter)가 죽을 때 새끼 2마리를 남긴다 — 원본 `killEnemy()`의 splitter 분기
    /// (project_test.html:1840~1844) 그대로.
    ///
    /// 스폰을 **직접 하지 않고** <see cref="EnemySpawnRequestBus"/>에 요청만 던진다. `Enemies`(2층)가
    /// 스포너(`Systems`, 3층)를 직접 참조하면 두 asmdef가 서로를 참조해 **컴파일이 거부된다**
    /// (CLAUDE.md "낮은 층이 높은 층의 기능을 요청" 항목 — 그래서 이 패턴이 미리 확정돼 있었다).
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemySplitOnDeath : MonoBehaviour
    {
        [Header("원본 killEnemy의 splitter 분기 (project_test.html:1840)")]
        [Tooltip("죽을 때 남기는 새끼 수. 원본은 좌우로 2마리.")]
        public int childCount = 2;
        [Tooltip("좌우로 벌어지는 거리. 원본 ±24px ÷100.")]
        public float spawnOffsetX = 0.24f;
        [Tooltip("남길 새끼의 종류.")]
        public EnemyType childType = EnemyType.Splitlet;

        EnemyHealth health;

        void Awake()
        {
            health = GetComponent<EnemyHealth>();
            health.Died += HandleDied;
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        void HandleDied(EnemyHealth _)
        {
            Vector2 pos = transform.position;
            for (int i = 0; i < childCount; i++)
            {
                // 원본 `for (const off of [-24, 24])` — 2마리를 좌우 대칭으로. 3마리 이상이면 균등 분배.
                float t = childCount == 1 ? 0f : (i / (float)(childCount - 1)) * 2f - 1f; // -1 ~ +1
                float x = Mathf.Clamp(pos.x + t * spawnOffsetX, FieldBounds.MinX, FieldBounds.MaxX);
                // 원본 `Math.min(e.y, CONFIG.world.groundY)` — 지면보다 아래에서는 안 나온다.
                // 원본은 Y+가 아래라 min이지만 우리 좌표계에선 max다.
                float y = Mathf.Max(pos.y, FieldBounds.GroundY);
                EnemySpawnRequestBus.Request(new Vector2(x, y), childType);
            }
        }
    }
}
