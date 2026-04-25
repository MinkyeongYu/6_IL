using UnityEngine;
using UnityEngine.SceneManagement;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 씬 간 공유 상태(자원·사이클·세이브 등). DontDestroyOnLoad 싱글톤.
    /// SnowfieldController/VillageController가 Instance를 통해 접근.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public ResourceStore Resources { get; private set; }
        public DayNightController Cycle { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Resources = new ResourceStore();
            Cycle = new DayNightController(BalanceConfig.Instance);

            // 시작 자원 (세이브가 없을 때)
            var b = BalanceConfig.Instance;
            Resources.Add(ResourceKind.Wood, b.StartingWood);
            Resources.Add(ResourceKind.Meat, b.StartingMeat);
            Resources.Add(ResourceKind.Food, b.StartingFood);
            Resources.Add(ResourceKind.Frostbloom, b.StartingFrostbloom);

            // 세이브 있으면 덮어씌움
            var save = SaveLoad.Load();
            if (save != null)
            {
                Resources.Restore(save.resources);
                Cycle.Restore(new DayNightSnapshot { day = save.currentDay, phase = Phase.Day, elapsedInPhase = 0 });
            }
        }

        public int LastFoodEaten { get; private set; }
        public int LastFoodShortage { get; private set; }
        public int TotalKills { get; private set; }
        public int CompanionsLost { get; private set; }
        public int MaxCompanionsAtOnce { get; private set; }

        public int Score => Mathf.Max(0,
            (Cycle != null ? (Cycle.Day - 1) * 10 : 0)
            + TotalKills * 3
            + MaxCompanionsAtOnce * 5
            - CompanionsLost * 4);

        public void OnZombieKilled() { TotalKills++; }
        public void OnCompanionLost() { CompanionsLost++; }

        private System.Action _unsubDay;

        private void Start()
        {
            _unsubDay = EventBus.Instance.Subscribe<DayStartedPayload>(p => OnDayStarted(p.Day));
        }

        private void OnDestroy()
        {
            _unsubDay?.Invoke();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // 동시 동료 최대치 추적
            int n = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None).Length;
            if (n > MaxCompanionsAtOnce) MaxCompanionsAtOnce = n;
        }

        private void OnDayStarted(int day)
        {
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int needed = comps != null ? comps.Length : 0;
            int have = Resources.Get(ResourceKind.Food);
            int eat = Mathf.Min(have, needed);
            if (eat > 0) Resources.Spend(ResourceKind.Food, eat);
            int hungry = needed - eat;
            LastFoodEaten = eat;
            LastFoodShortage = hungry;
            if (hungry > 0 && comps != null)
            {
                for (int i = 0; i < hungry && i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null) continue;
                    c.Morale -= 15;
                    if (c.Morale <= 0)
                    {
                        OnCompanionLost();
                        Destroy(c.gameObject);
                    }
                }
            }

            // 새벽 회복: Player + 모든 살아있는 동료 풀 HP
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.Heal(pc.MaxHp);
            }
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    if (c != null) c.Heal(c.MaxHp);
                }
            }
        }

        public void SaveNow()
        {
            SaveLoad.Save(new SaveFileV1
            {
                version = 1,
                currentDay = Cycle.Day,
                resources = Resources.Snapshot(),
                weatherRng = 42,
            });
        }

        public void HardReset()
        {
            SaveLoad.Clear();
            Instance = null;
            Destroy(gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
