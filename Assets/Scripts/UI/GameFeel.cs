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

        /// <summary>발자국 눈 퍼프: 매우 작고 빠르게 사라짐 (이동 트레일용).</summary>
        public static void SnowPuff(Vector3 worldPos)
        {
            var go = new GameObject("__snowpuff");
            go.transform.position = worldPos;
            float startScale = 0.18f + UnityEngine.Random.value * 0.08f;
            go.transform.localScale = Vector3.one * startScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 1f, 1f, 0.85f);
            cf.Shape = FallbackShape.Circle;
            cf.Circle = true;
            cf.PixelSize = 32;
            cf.OutlineWidth = 0;
            cf.OutlineColor = new Color(0, 0, 0, 0);

            Runner().StartCoroutine(SnowPuffRoutine(go, startScale));
        }

        private static IEnumerator SnowPuffRoutine(GameObject go, float startScale)
        {
            float t = 0f;
            float dur = 0.5f;
            SpriteRenderer sr = null;
            while (t < dur)
            {
                if (go == null) yield break;
                if (sr == null) sr = go.GetComponent<SpriteRenderer>();
                t += Time.deltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 1.8f, k);
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = (1f - k) * 0.7f;
                    sr.color = c;
                }
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        // 캐시된 흰 정사각 sprite (슬래시 등에 재사용)
        private static Sprite _whiteSquare;
        private static Sprite GetWhiteSquare()
        {
            if (_whiteSquare != null) return _whiteSquare;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < 64; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
            return _whiteSquare;
        }

        /// <summary>근접 공격 슬래시 — from→to 방향으로 가로로 늘어난 반투명 막대를 살짝 보여주고 사라짐.</summary>
        public static void Slash(Vector3 from, Vector3 to, Color color)
        {
            Vector2 d = (Vector2)to - (Vector2)from;
            float dist = d.magnitude;
            if (dist < 0.05f) dist = 0.5f;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Vector3 mid = (from + to) * 0.5f;

            var go = new GameObject("__slash");
            go.transform.position = mid;
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            go.transform.localScale = new Vector3(dist, 0.30f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;
            sr.sprite = GetWhiteSquare();
            var c = color; c.a = 0.85f;
            sr.color = c;

            Runner().StartCoroutine(SlashFade(go, 0.13f));
        }

        private static IEnumerator SlashFade(GameObject go, float dur)
        {
            float t = 0f;
            SpriteRenderer sr = null;
            while (t < dur)
            {
                if (go == null) yield break;
                if (sr == null) sr = go.GetComponent<SpriteRenderer>();
                t += Time.deltaTime;
                float k = t / dur;
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = (1f - k) * 0.85f;
                    sr.color = c;
                }
                // 살짝 두께 증가 (호 느낌)
                go.transform.localScale = new Vector3(go.transform.localScale.x, 0.30f + k * 0.20f, 1f);
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        public static void FloatText(Vector3 worldPos, string text, Color color)
        {
            var ft = Runner().gameObject.GetComponent<FloatTextRoot>();
            if (ft == null) ft = Runner().gameObject.AddComponent<FloatTextRoot>();
            ft.Spawn(worldPos, text, color);
        }

        private sealed class GameFeelRunner : MonoBehaviour { }

        public sealed class FloatTextRoot : MonoBehaviour
        {
            private struct Item
            {
                public Vector3 World;
                public string Text;
                public Color Color;
                public float Age;
            }
            private readonly System.Collections.Generic.List<Item> _items = new();
            private GUIStyle _style;
            private const float LifeSec = 0.9f;

            public void Spawn(Vector3 w, string t, Color c)
            {
                _items.Add(new Item { World = w, Text = t, Color = c, Age = 0f });
                if (_items.Count > 64) _items.RemoveAt(0);
            }

            private void Update()
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    var it = _items[i];
                    it.Age += Time.deltaTime;
                    _items[i] = it;
                    if (it.Age >= LifeSec) _items.RemoveAt(i);
                }
            }

            private void OnGUI()
            {
                if (_items.Count == 0) return;
                if (_style == null)
                {
                    _style = new GUIStyle(GUI.skin.label) {
                        fontSize = 16, fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter };
                }
                var cam = Camera.main;
                if (cam == null) return;
                var oldC = GUI.color;
                foreach (var it in _items)
                {
                    float k = it.Age / LifeSec;
                    Vector3 pos = it.World + Vector3.up * (0.6f + k * 1.0f);
                    Vector3 sp = cam.WorldToScreenPoint(pos);
                    if (sp.z < 0) continue;
                    float a = 1f - k;
                    var c = it.Color;
                    c.a = a;
                    GUI.color = c;
                    var r = new Rect(sp.x - 60, Screen.height - sp.y - 12, 120, 24);
                    // 살짝 그림자
                    GUI.color = new Color(0, 0, 0, 0.6f * a);
                    GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), it.Text, _style);
                    GUI.color = c;
                    GUI.Label(r, it.Text, _style);
                }
                GUI.color = oldC;
            }
        }
    }
}
