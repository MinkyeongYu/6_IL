using UnityEngine;

namespace IL6
{
    public sealed class DiscoverySite : MonoBehaviour
    {
        public string Title = "Hidden Cache";
        [TextArea(1, 2)] public string Description = "A small cache left in the snow.";
        public ResourceKind RewardKind = ResourceKind.Food;
        public int RewardAmount = 3;

        public void Resolve(ResourceStore store)
        {
            if (store == null) return;
            int amount = Mathf.Max(1, RewardAmount);
            store.Add(RewardKind, amount);
            GameFeel.FloatText(transform.position, $"{Title} +{amount} {RewardKind}", TintFor(RewardKind));
            Sfx.Pickup();
        }

        public static Color TintFor(ResourceKind kind) => kind switch
        {
            ResourceKind.Wood => new Color(0.35f, 0.7f, 0.35f, 0.9f),
            ResourceKind.Stone => new Color(0.7f, 0.7f, 0.75f, 0.9f),
            ResourceKind.Meat => new Color(0.85f, 0.45f, 0.4f, 0.9f),
            ResourceKind.Food => new Color(0.95f, 0.78f, 0.4f, 0.9f),
            ResourceKind.Frostbloom => new Color(0.7f, 0.85f, 1f, 0.9f),
            _ => new Color(0.9f, 0.9f, 0.9f, 0.9f),
        };

        public static Sprite SpriteFor(ResourceKind kind)
        {
            return SpriteBank.DiscoveryByReward(kind);
        }
    }
}
