using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IL6
{
    /// <summary>
    /// DayNightController 의 Phase 이벤트를 구독해 Global Light 2D 의 intensity/color 를
    /// 현재 → 다음 페이즈 타겟 값으로 Lerp 전환.
    /// DayNightController 자체는 pure 로 유지하고 이 MonoBehaviour 가 Unity 와 접착.
    /// </summary>
    public sealed class DayNightLightBinder : MonoBehaviour
    {
        public Light2D GlobalLight;

        private Phase _currentPhase = Phase.Day;
        private Phase _targetPhase = Phase.Day;
        private float _transitionDuration = 5f;
        private float _elapsed;

        private void Start()
        {
            if (GlobalLight == null)
            {
                Debug.LogWarning("[DayNightLightBinder] GlobalLight 미할당");
                return;
            }
            var t = DayNightLightCalc.Target(_currentPhase);
            GlobalLight.intensity = t.intensity;
            GlobalLight.color = t.color;

            EventBus.Instance.Subscribe<EveningStartedPayload>(_ => BeginTransition(Phase.Evening));
            EventBus.Instance.Subscribe<NightStartedPayload>(_ => BeginTransition(Phase.Night));
            EventBus.Instance.Subscribe<DawnStartedPayload>(_ => BeginTransition(Phase.Dawn));
            EventBus.Instance.Subscribe<DayStartedPayload>(_ => BeginTransition(Phase.Day));
        }

        private void BeginTransition(Phase to)
        {
            _targetPhase = to;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (GlobalLight == null) return;
            if (_currentPhase == _targetPhase) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _transitionDuration);
            var v = DayNightLightCalc.Lerp(_currentPhase, _targetPhase, t);
            GlobalLight.intensity = v.intensity;
            GlobalLight.color = v.color;
            if (t >= 1f) _currentPhase = _targetPhase;
        }
    }
}
