using UnityEngine;
using UnityEngine.UI;

namespace IL6
{
    /// <summary>
    /// 단일 건설 버튼. Inspector에서 BuildingKind 지정.
    /// 클릭 시 PlacementController.Begin(kind) 호출.
    /// 자원 부족 시 회색.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BuildButton : MonoBehaviour
    {
        public BuildingKind Kind;
        public PlacementController Placement;
        public ResourceStore Store;
        public Text Label;
        public Image Background;

        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            if (Store != null) Store.OnChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (Store != null) Store.OnChanged -= OnResourceChanged;
        }

        private void OnResourceChanged(ResourceKind k, int delta, int total) => Refresh();

        private void Refresh()
        {
            int cost = Kind == BuildingKind.Campfire
                ? BalanceConfig.Instance.CampfireCost
                : BalanceConfig.Instance.BarricadeCost;
            string name = Kind == BuildingKind.Campfire ? "🔥 모닥불" : "🪵 바리게이트";
            int have = Store != null ? Store.Get(ResourceKind.Wood) : 0;
            bool enough = have >= cost;
            if (Label != null) Label.text = $"{name}\n🪵 {cost}";
            if (Background != null) Background.color = enough ? new Color(0.16f, 0.21f, 0.27f) : new Color(0.16f, 0.21f, 0.27f, 0.4f);
            _btn.interactable = enough;
        }

        private void OnClick()
        {
            if (Placement != null) Placement.Begin(Kind);
        }
    }
}
