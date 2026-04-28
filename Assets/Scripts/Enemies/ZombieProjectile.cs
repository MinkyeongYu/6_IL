using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 원거리 좀비(Archer)의 투사체. PlayerController/Companion/Building 명중 시 데미지.
    /// Projectile.Awake 가 sprite 자동 생성.
    /// </summary>
    public sealed class ZombieProjectile : MonoBehaviour
    {
        public float Speed = 9f;
        public int Damage = 6;
        public float MaxLifetime = 3.5f;
        public float HitRadius = 0.5f;

        private Vector2 _direction = Vector2.right;
        private float _life;

        public void Aim(Transform target)
        {
            if (target == null) return;
            Vector2 d = (Vector2)target.position - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) _direction = d.normalized;
        }

        // Projectile 의 sprite 자동 생성 로직 차용
        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                int N = 32;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                float r = N / 2f;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float dx = x - r, dy = y - r;
                        float d2 = dx * dx + dy * dy;
                        bool inside = d2 < (r - 1) * (r - 1);
                        bool ring = d2 < r * r && !inside;
                        if (inside) tex.SetPixel(x, y, Color.white);
                        else if (ring) tex.SetPixel(x, y, new Color(0f, 0f, 0f, 1f));
                        else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            }
        }

        private void Update()
        {
            _life += Time.deltaTime;
            if (_life > MaxLifetime) { Destroy(gameObject); return; }

            transform.position += (Vector3)(_direction * Speed * Time.deltaTime);

            // 명중 검사 — 짧은 OverlapCircle
            var hits = Physics2D.OverlapCircleAll(transform.position, HitRadius);
            foreach (var h in hits)
            {
                if (h == null) continue;
                var pc = h.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead) { pc.TakeDamage(Damage); Destroy(gameObject); return; }
                var cc = h.GetComponent<Companion>();
                if (cc != null && !cc.IsDead) { cc.TakeDamage(Damage); Destroy(gameObject); return; }
                var bb = h.GetComponent<Building>();
                if (bb != null && bb.CurrentHp > 0) { bb.TakeDamage(Damage); Destroy(gameObject); return; }
            }
        }
    }
}
