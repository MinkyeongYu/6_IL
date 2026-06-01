using System;
using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    public enum ResourceKind { Wood, Meat, Food, Frostbloom, Stone }

    [Serializable]
    public class ResourceSnapshot
    {
        public int wood;
        public int meat;
        public int food;
        public int frostbloom;
        public int stone;

        public int Get(ResourceKind k) => k switch
        {
            ResourceKind.Wood => wood,
            ResourceKind.Meat => meat,
            ResourceKind.Food => food,
            ResourceKind.Frostbloom => frostbloom,
            ResourceKind.Stone => stone,
            _ => 0,
        };

        public void Set(ResourceKind k, int v)
        {
            switch (k)
            {
                case ResourceKind.Wood: wood = v; break;
                case ResourceKind.Meat: meat = v; break;
                case ResourceKind.Food: food = v; break;
                case ResourceKind.Frostbloom: frostbloom = v; break;
                case ResourceKind.Stone: stone = v; break;
            }
        }
    }

    /// <summary>
    /// 게임 자원 단일 저장소. 변경 시 EventBus로 ResourceChangedPayload 방출.
    /// </summary>
    public sealed class ResourceStore
    {
        private readonly ResourceSnapshot _totals = new();
        private readonly Dictionary<ResourceKind, int> _caps = new()
        {
            { ResourceKind.Wood, 100 },
            { ResourceKind.Stone, 100 },
            { ResourceKind.Meat, 100 },
            { ResourceKind.Food, 100 },
            { ResourceKind.Frostbloom, 50 },
        };
        public event Action<ResourceKind, int, int> OnChanged; // kind, delta, total

        public int Get(ResourceKind k) => _totals.Get(k);
        public int GetCap(ResourceKind k) => _caps.TryGetValue(k, out var v) ? v : 100;
        public void IncreaseCap(int delta) { foreach (var k in new[] { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Meat, ResourceKind.Food }) _caps[k] += delta; }
        public void IncreaseCap(ResourceKind kind, int delta)
        {
            if (!_caps.ContainsKey(kind)) _caps[kind] = 100;
            _caps[kind] += delta;
        }

        public void Add(ResourceKind k, int amount)
        {
            if (amount <= 0) return;
            int cap = GetCap(k);
            int newTotal = Mathf.Min(cap, _totals.Get(k) + amount);
            int actualDelta = newTotal - _totals.Get(k);
            if (actualDelta == 0) return;
            _totals.Set(k, newTotal);
            OnChanged?.Invoke(k, actualDelta, newTotal);
            EventBus.Instance.Emit(new ResourceChangedPayload(k.ToString().ToLower(), actualDelta, newTotal));
        }

        public bool Spend(ResourceKind k, int amount)
        {
            if (_totals.Get(k) < amount) return false;
            int newTotal = _totals.Get(k) - amount;
            _totals.Set(k, newTotal);
            OnChanged?.Invoke(k, -amount, newTotal);
            EventBus.Instance.Emit(new ResourceChangedPayload(k.ToString().ToLower(), -amount, newTotal));
            return true;
        }

        public ResourceSnapshot Snapshot()
        {
            return new ResourceSnapshot
            {
                wood = _totals.wood,
                meat = _totals.meat,
                food = _totals.food,
                frostbloom = _totals.frostbloom,
                stone = _totals.stone,
            };
        }

        public void Restore(ResourceSnapshot snap)
        {
            if (snap == null) return;
            _totals.wood = snap.wood;
            _totals.meat = snap.meat;
            _totals.food = snap.food;
            _totals.frostbloom = snap.frostbloom;
            _totals.stone = snap.stone;
        }
    }
}
