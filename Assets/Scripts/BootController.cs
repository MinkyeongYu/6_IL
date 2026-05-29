using UnityEngine;
using UnityEngine.SceneManagement;

namespace IL6
{
    /// <summary>
    /// Boot 신: GameSession 초기화 후 OnboardingScene 으로 전환.
    /// 빈 씬에 GameSession 컴포넌트 + 이 BootController 컴포넌트 하나만 두면 됨.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class BootController : MonoBehaviour
    {
        public string OnboardingSceneName = "OnboardingScene";

        private void Start()
        {
            if (GameSession.Instance == null)
            {
                var go = new GameObject("GameSession");
                go.AddComponent<GameSession>();
            }
            SceneManager.LoadScene(OnboardingSceneName);
        }
    }
}
