using System;

namespace IL6
{
    /// <summary>
    /// mulberry32 — 32bit 시드 기반 결정적 PRNG.
    /// 도메인 로직(대미지·날씨·스폰)에서는 UnityEngine.Random 대신 이걸 사용.
    /// </summary>
    public sealed class SeededRng
    {
        private uint _state;

        public SeededRng(uint seed) { _state = seed; }

        public uint State { get => _state; set => _state = value; }

        public float Next()
        {
            _state = unchecked(_state + 0x6d2b79f5u);
            uint t = _state;
            t = unchecked((t ^ (t >> 15)) * (t | 1u));
            t = unchecked(t ^ (t + ((t ^ (t >> 7)) * (t | 61u))));
            return ((t ^ (t >> 14)) & 0xFFFFFFFFu) / 4294967296f;
        }

        /// <summary>[min, max] inclusive integer range.</summary>
        public int IntRange(int min, int max)
        {
            if (max < min) throw new ArgumentException($"max({max}) < min({min})");
            return (int)Math.Floor(Next() * (max - min + 1)) + min;
        }

        public T Pick<T>(System.Collections.Generic.IList<T> arr)
        {
            if (arr == null || arr.Count == 0) throw new ArgumentException("Pick: empty list");
            int idx = (int)Math.Floor(Next() * arr.Count);
            return arr[idx];
        }
    }
}
