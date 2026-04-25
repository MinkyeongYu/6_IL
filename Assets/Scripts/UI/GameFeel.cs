using System.Collections;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 게임 필 헬퍼: 피격 시 짧은 흰색 플래시. 죽음 시 페이드 퍼프.
    /// </summary>
    public static class GameFeel
    {
        private static GameObject _runner;

        private static MonoBehaviour Runner()
        {
            if (_runner == null)
            {
                _runner = new GameObject("__GameFeelRunner");
                Object.DontDestroyOnLoad(_runner);
                _runner.AddComponent<GameFeelRunner>();
            }
            return _runner.GetComponent<GameFeelRunner>();
        }

        public static void HitFlash(MonoBehaviour owner, SpriteRenderer sr)
        {
            if (sr == null || owner == null || !owner.gameObject.activeInHierarchy) return;
            owner.StartCoroutine(FlashRoutine(sr));
        }

        private static IEnumerator FlashRoutine(SpriteRenderer sr)
        {
            if (sr == null) yield break;
            Color original = sr.color;
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (sr != null) sr.color = original;
        }

        /// <summary>죽음 위치에 일시적인 색 퍼프 — 0.4초 동안 페이드되며 살짝 확장.</summary>
        public static void DeathPoof(Vector3 worldPos, Color color, float startScale = 0.6f)
        {
            var go = new GameObject("__poof");
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * startScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = color;
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 64;
            cf.OutlineWidth = 0;
            cf.OutlineColor = new Color(0, 0, 0, 0);

            Runner().StartCoroutine(PoofRoutine(go, startScale));
        }

        private static IEnumerator PoofRoutine(GameObject go, float startScale)
        {
            float t = 0f;
            float dur = 0.4f;
            SpriteRenderer sr = null;
            while (t < dur)
            {
                if (go == null) yield break;
                if (sr == null) sr = go.GetComponent<SpriteRenderer>();
                t += Time.deltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 1.6f, k);
                if (sr != null && sr.color.a > 0f)
                {
                    var c = sr.color;
                    c.a = 1f - k;
                    sr.color = c;
                }
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        private sealed class GameFeelRunner : MonoBehaviour { }
    }
}
