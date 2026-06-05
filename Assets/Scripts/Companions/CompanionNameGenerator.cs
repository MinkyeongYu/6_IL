using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    public static class CompanionNameGenerator
    {
        private static readonly string[] MaleNames =
        {
            "Joon", "Seo", "Tae", "Min", "Haru", "Kye",
            "Ian", "Ren", "Noa", "Gun", "Ari", "Sol"
        };

        private static readonly string[] FemaleNames =
        {
            "Ina", "Yuna", "Mira", "Sera", "Rin", "Hana",
            "Nari", "Soo", "Dara", "Lia", "Ara", "Moa"
        };

        private static readonly string[] ChildNames =
        {
            "Bomi", "Nuel", "Roa", "Hayan", "Dami", "Yul",
            "Lumi", "Sori", "Noel", "Bora", "Nuri", "Iru"
        };

        public static string GenerateForRole(string role, SeededRng rng)
        {
            if (rng == null) rng = new SeededRng((uint)Random.Range(1, int.MaxValue));
            var pool = PoolForRole(role);
            var used = CollectUsedNames();

            int attempts = pool.Length * 2;
            for (int i = 0; i < attempts; i++)
            {
                string candidate = pool[rng.IntRange(0, pool.Length - 1)];
                if (!used.Contains(candidate)) return candidate;
            }

            foreach (var candidate in pool)
            {
                if (!used.Contains(candidate)) return candidate;
            }

            return pool[rng.IntRange(0, pool.Length - 1)];
        }

        private static string[] PoolForRole(string role)
        {
            string r = role ?? "";
            if (r.Contains("Child") || r.Contains("child") || r.Contains("아이")) return ChildNames;
            if (r.Contains("Farmer") || r.Contains("Cook") || r.Contains("Elder") || r.Contains("농부") || r.Contains("노인")) return FemaleNames;
            return MaleNames;
        }

        private static HashSet<string> CollectUsedNames()
        {
            var used = new HashSet<string>();

            var npcs = Object.FindObjectsByType<RecruitableNpc>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                AddCleanName(used, npc.DisplayNamePublic);
            }

            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            foreach (var companion in companions)
            {
                if (companion == null || companion.IsDead) continue;
                AddCleanName(used, companion.gameObject.name);
            }

            return used;
        }

        private static void AddCleanName(HashSet<string> used, string raw)
        {
            string clean = CleanName(raw);
            if (!string.IsNullOrEmpty(clean)) used.Add(clean);
        }

        private static string CleanName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string clean = raw.Trim();

            int recruited = clean.IndexOf("(Recruited)", System.StringComparison.Ordinal);
            if (recruited >= 0) clean = clean.Substring(0, recruited).Trim();

            int npcPrefix = clean.LastIndexOf('_');
            if (npcPrefix >= 0 && npcPrefix + 1 < clean.Length)
                clean = clean.Substring(npcPrefix + 1).Trim();

            return clean;
        }
    }
}
