using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 채집 가능 마커. GatherController가 가까운 Gatherable을 찾아 채집 시작.
    /// </summary>
    public sealed class Gatherable : MonoBehaviour
    {
        public ResourceKind YieldKind = ResourceKind.Wood;
        public int YieldAmount = 4;
        public float DurationSec = 9f;
        [Tooltip("채집 완료 시 GameObject 파괴 여부")]
        public bool DestroyOnGather = true;

        public void OnGathered(ResourceStore store)
        {
            Color tint = ResourceTint(YieldKind);
            if (YieldAmount > 0)
            {
                store.Add(YieldKind, YieldAmount);
                GameFeel.FloatText(transform.position, $"+{YieldAmount} {YieldKind}", tint);
                Sfx.Pickup();
            }
            else
            {
                // 드랍 실패 (예: 늑대 75%) — 가벼운 피드백만
                GameFeel.FloatText(transform.position, "—", new Color(0.7f, 0.7f, 0.75f));
            }

            // 보너스 자원 (예: 눈토끼는 Frostbloom 도 같이)
            var bonus = GetComponent<BonusYieldOnGather>();
            if (bonus != null && bonus.Amount > 0)
            {
                store.Add(bonus.Kind, bonus.Amount);
                GameFeel.FloatText(transform.position + new Vector3(0f, 0.3f, 0f),
                    $"+{bonus.Amount} {bonus.Kind}", ResourceTint(bonus.Kind));
            }

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
