using UnityEngine;

namespace IL6
{
    /// <summary>
    /// Gatherable 옆에 붙으면, 채집 완료 시 추가 자원도 같이 지급.
    /// 예: 눈토끼 — 고기 1 (Gatherable 본체) + Frostbloom 1 (보너스).
    /// </summary>
    public sealed class BonusYieldOnGather : MonoBehaviour
    {
        public ResourceKind Kind = ResourceKind.Frostbloom;
        public int Amount = 1;
    }
}
