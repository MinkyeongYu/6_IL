using UnityEngine;

namespace IL6
{
    /// <summary>
    /// Y축 월드 좌표 기반으로 SpriteRenderer 의 sortingOrder 를 설정.
    /// 아래쪽(더 큰 -y)에 있는 오브젝트가 위쪽 오브젝트보다 앞에 그려짐.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class YSort : MonoBehaviour
    {
        [SerializeField] private int orderOffset = 0;
        [SerializeField] private bool updateEveryFrame = true;

        private SpriteRenderer[] _renderers;

        public static int ComputeOrder(float y) => Mathf.RoundToInt(-y * 100f);

        private void Awake() { _renderers = GetComponentsInChildren<SpriteRenderer>(); }

        private void Start()
        {
            UpdateSortingOrder();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
            {
                UpdateSortingOrder();
            }
        }

        public void UpdateSortingOrder()
        {
            int order = ComputeOrder(transform.position.y) + orderOffset;

            foreach(SpriteRenderer renderer in _renderers)
            {
                renderer.sortingOrder = order;
            }
        }
    }
}
