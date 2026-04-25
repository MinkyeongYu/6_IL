using System;
using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 밭. 밤이 NightsToRipe 만큼 지나면 HarvestReady=true.
    /// 동료를 배치하면 수확량이 증가하고 동료는 Farming 모드로 묶임.
    /// 수확하면 식량 적립, 동료 해제, 카운트 리셋.
    /// </summary>
    public sealed class FarmBuilding : MonoBehaviour
    {
        public int NightsToRipe = 2;
        public int BaseYield = 4;
        public int PerWorkerBonus = 3;
        public int MaxWorkers = 3;

        public bool HarvestReady { get; private set; }
        public int NightsPassed { get; private set; }
        public readonly List<Companion> Workers = new();

        private Action _unsub;

        private void Start()
        {
            _unsub = EventBus.Instance.Subscribe<NightStartedPayload>(_ => OnNight());
        }

        private void OnDestroy()
        {
            _unsub?.Invoke();
            foreach (var w in Workers) if (w != null) w.ReleaseFarm();
        }

        private void OnNight()
        {
            if (HarvestReady) return;
            NightsPassed++;
            if (NightsPassed >= NightsToRipe) HarvestReady = true;
        }

        public bool TryAssignWorker(Companion c)
        {
            if (c == null || Workers.Contains(c)) return false;
            if (Workers.Count >= MaxWorkers) return false;
            Workers.Add(c);
            c.AssignFarm(transform);
            return true;
        }

        public int Harvest()
        {
            if (!HarvestReady) return 0;
            int yield = BaseYield + Workers.Count * PerWorkerBonus;
            var session = GameSession.Instance;
            if (session != null) session.Resources.Add(ResourceKind.Food, yield);
            GameFeel.FloatText(transform.position, $"+{yield} Food", new Color(0.7f, 0.95f, 0.5f));
            foreach (var w in Workers) if (w != null) w.ReleaseFarm();
            Workers.Clear();
            NightsPassed = 0;
            HarvestReady = false;
            return yield;
        }
    }
}
