using UnityEngine;
using UnityEngine.SceneManagement;

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
