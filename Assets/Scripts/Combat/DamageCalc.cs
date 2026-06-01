using System;

namespace IL6
{
    public static class DamageCalc
    {
        public struct Input
        {
            public int Base;
            public int Armor;
            public float CritRoll;     // 0..1, normally rng.Next()
            public float CritChance;   // 0..1
            public float CritMult;
        }

        /// <summary>최소 1 보장. 결정적 (RNG는 호출자가 주입).</summary>
        public static int Compute(in Input input)
        {
            float crit = input.CritRoll < input.CritChance ? input.CritMult : 1f;
            float raw = input.Base * crit - input.Armor;
            return Math.Max(1, (int)Math.Floor(raw));
        }
    }
}
