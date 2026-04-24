using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 채집 가능 마커. GatherController가 가까운 Gatherable을 찾아 채집 시작.
    /// </summary>
    public sealed class Gatherable : MonoBehaviour
    {
        public ResourceKind YieldKind = ResourceKind.Wood;
        public int YieldAmount = 3;
        public float DurationSec = 4f;
        [Tooltip("채집 완료 시 GameObject 파괴 여부")]
        public bool DestroyOnGather = true;

        public void OnGathered(ResourceStore store)
        {
            store.Add(YieldKind, YieldAmount);
            if (DestroyOnGather) Destroy(gameObject);
        }
    }
}
