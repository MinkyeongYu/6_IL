using UnityEngine;
using UnityEngine.UI;

namespace IL6
{
    /// <summary>
    /// 단순 HUD: 자원 텍스트, HP 바, 단계 배너.
    /// Unity Canvas + UI Text/Image 컴포넌트를 Inspector에서 연결.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [Header("References")]
        public PlayerController Player;
        public ResourceStore Store; // 코드에서 주입
        public DayNightController Cycle; // 코드에서 주입

        [Header("UI Elements")]
        public Text ResourceText;
        public Slider HealthSlider;
        public Text PhaseText;
        public Text WeaponNameText;
        public Slider WeaponCooldownSlider;

        public PlayerAttackController AttackController;

        private void OnEnable()
        {
            if (Store != null) Store.OnChanged += OnResourceChanged;
        }

        private void OnDisable()
        {
            if (Store != null) Store.OnChanged -= OnResourceChanged;
        }

        private void Start()
        {
            RefreshResource();
        }

        private void Update()
        {
            if (Player != null && HealthSlider != null)
            {
                HealthSlider.maxValue = Player.MaxHp;
                HealthSlider.value = Player.CurrentHp;
            }
            if (Cycle != null && PhaseText != null)
            {
                string label = Cycle.Phase switch
                {
                    Phase.Day => "낮",
                    Phase.Evening => "저녁",
                    Phase.Night => "밤",
                    Phase.Dawn => "새벽",
                    _ => "?"
                };
                PhaseText.text = $"Day {Cycle.Day} — {label}";
            }
            if (AttackController != null && WeaponCooldownSlider != null && AttackController.Weapon != null)
            {
                WeaponNameText.text = AttackController.Weapon.DisplayName;
                WeaponCooldownSlider.maxValue = AttackController.Weapon.CooldownSec;
                WeaponCooldownSlider.value = AttackController.Weapon.CooldownSec - AttackController.CurrentCooldown;
            }
        }

        private void OnResourceChanged(ResourceKind k, int delta, int total) => RefreshResource();

        private void RefreshResource()
        {
            if (Store == null || ResourceText == null) return;
            var s = Store.Snapshot();
            ResourceText.text = $"🪵 {s.wood}  🥩 {s.meat}  🌾 {s.food}  ❄ {s.frostbloom}";
        }
    }
}
