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
            Color tint = ResourceTint(YieldKind);
            GameFeel.FloatText(transform.position, $"+{YieldAmount} {YieldKind}", tint);
            Sfx.Pickup();
            if (DestroyOnGather)
            {
                GameFeel.DeathPoof(transform.position, tint, 0.5f);
                Destroy(gameObject);
            }
        }

        private static Color ResourceTint(ResourceKind k) => k switch
        {
            ResourceKind.Wood => new Color(0.35f, 0.7f, 0.35f, 0.9f),
            ResourceKind.Stone => new Color(0.7f, 0.7f, 0.75f, 0.9f),
            ResourceKind.Meat => new Color(0.85f, 0.45f, 0.4f, 0.9f),
            ResourceKind.Food => new Color(0.95f, 0.78f, 0.4f, 0.9f),
            ResourceKind.Frostbloom => new Color(0.7f, 0.85f, 1f, 0.9f),
            _ => new Color(0.9f, 0.9f, 0.9f, 0.9f),
        };
    }
}
