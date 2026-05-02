using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 도주형 동물 — 토끼/여우/사슴/멧돼지/흰토끼.
    /// 플레이어가 FleeRadius 안에 들어오면 도망, 그 외엔 정지.
    /// </summary>
    public sealed class DeerAi : AnimalAi
    {
        public float FleeRadius = 120f;
        public float FleeSpeed = 140f;

        protected override void DoBehavior()
        {
            Vector2 toMe = (Vector2)(transform.position - _player.position);
            float d = toMe.magnitude;
            if (d < FleeRadius) _rb.velocity = toMe.normalized * FleeSpeed;
            else _rb.velocity = Vector2.zero;
        }
    }
}
