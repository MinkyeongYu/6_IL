using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 포식자 동물 — 늑대/곰/맘모스. 시야 안 플레이어/동료를 추격해 근접 공격.
    /// </summary>
    public sealed class WolfAi : AnimalAi
    {
        public float SightRange = 9f;
        public float AttackRange = 1.2f;
        public float MoveSpeed = 4.0f;
        public int Damage = 6;
        public float AttackCooldown = 1.0f;

        private float _attackCd;

        private Transform FindTarget()
        {
            Transform best = null;
            float bestDist = SightRange;
            if (_player != null)
            {
                var pc = _player.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead)
                {
                    float d = Vector2.Distance(transform.position, _player.position);
                    if (d < bestDist) { best = _player; bestDist = d; }
                }
            }
            var comps = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var c in comps)
            {
                if (c == null || c.IsDead) continue;
                if (c.CurrentMode == Companion.Mode.Hiding) continue;
                float d = Vector2.Distance(transform.position, c.transform.position);
                if (d < bestDist) { best = c.transform; bestDist = d; }
            }
            return best;
        }

        protected override void DoBehavior()
        {
            var target = FindTarget();
            if (target == null) { _rb.velocity = Vector2.zero; return; }

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= AttackRange)
            {
                _rb.velocity = Vector2.zero;
                _attackCd -= Time.fixedDeltaTime;
                if (_attackCd <= 0f)
                {
                    var pc = target.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(Damage);
                    var c = target.GetComponent<Companion>();
                    if (c != null) c.TakeDamage(Damage);
                    _attackCd = AttackCooldown;
                }
            }
            else
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                _rb.velocity = dir * MoveSpeed;
                if (_attackCd > 0f) _attackCd -= Time.fixedDeltaTime;
            }
        }
    }
}
