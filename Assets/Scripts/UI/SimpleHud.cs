using UnityEngine;

namespace IL6
{
    /// <summary>
    /// IMGUI 기반 HUD. 좌측: 플레이어 상태/무기/빌드. 우측 상단: 자원/사이클.
    /// Canvas/TMP 셋업 없이 즉시 작동.
    /// </summary>
    public sealed class SimpleHud : MonoBehaviour
    {
        public PlayerController Player;
        public GatherController Gather;
        public PlayerAttackController Attacker;
        public NightController Night;

        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _weaponStyle;
        private GUIStyle _resStyle;

        private void OnGUI()
        {
            EnsureStyles();
            DrawLeftPanel();
            DrawRightPanel();
        }

        private void DrawLeftPanel()
        {
            GUI.Box(new Rect(10, 10, 280, 340), "");
            int y = 18;
            GUI.Label(new Rect(20, y, 260, 24), "=== Player ===", _titleStyle); y += 26;

            if (Player != null)
            {
                GUI.Label(new Rect(20, y, 260, 22), $"HP: {Player.CurrentHp} / {Player.MaxHp}", _labelStyle); y += 20;
                var p = Player.transform.position;
                GUI.Label(new Rect(20, y, 260, 22), $"Pos: ({p.x:F1}, {p.y:F1})", _labelStyle); y += 22;
            }
            else
            {
                GUI.Label(new Rect(20, y, 260, 22), "Player: NULL", _labelStyle); y += 22;
            }

            if (Gather != null && Gather.IsActive)
            {
                GUI.Label(new Rect(20, y, 260, 22), $"Gathering: {(Gather.Progress * 100):F0}%", _labelStyle); y += 22;
            }

            if (Attacker != null && Attacker.Weapon != null)
            {
                var w = Attacker.Weapon;
                GUI.Label(new Rect(20, y, 260, 22), $"[Weapon] {w.DisplayName}", _weaponStyle); y += 22;
                GUI.Label(new Rect(20, y, 260, 22), $"DMG {w.BaseDamage}  RNG {w.Range:F1}u  CD {w.CooldownSec:F2}s", _labelStyle); y += 20;
                float cd = Attacker.CurrentCooldown;
                float ready = 1f - Mathf.Clamp01(cd / Mathf.Max(0.01f, w.CooldownSec));
                var barBg = new Rect(20, y, 200, 14); GUI.Box(barBg, "");
                var fill = new Rect(22, y + 2, 196 * ready, 10);
                GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.Label(new Rect(230, y - 4, 50, 22), ready >= 1f ? "READY" : $"{(cd):F1}s", _labelStyle);
                y += 18;
            }

            y += 6;
            var session = GameSession.Instance;
            GUI.enabled = session != null && Player != null && session.Resources.Get(ResourceKind.Wood) >= 5;
            if (GUI.Button(new Rect(20, y, 220, 30), "Build Campfire (5 Wood)"))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 5))
                {
                    SpawnCampfire(Player.transform.position);
                }
            }
            GUI.enabled = true;
            y += 34;

            GUI.enabled = session != null && Player != null && session.Resources.Get(ResourceKind.Wood) >= 5;
            if (GUI.Button(new Rect(20, y, 220, 30), "Build Barricade (5 Wood)"))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 5))
                {
                    SpawnBarricade(Player.transform.position);
                }
            }
            GUI.enabled = true;
            y += 34;

            // 영입 힌트: 가까운 Recruitable NPC 표시
            var nearest = FindNearestRecruitable(Player != null ? Player.transform.position : Vector3.zero, 2f);
            if (nearest != null)
            {
                GUI.Label(new Rect(20, y, 260, 24),
                    $"Press F to recruit: {nearest.DisplayNamePublic}", _weaponStyle);
                y += 24;
            }

            // 채굴 버튼: 근처 나무가 있고 동료가 있을 때만 활성화
            if (Player != null)
            {
                var nearTree = FindNearestTreeInRange(Player.transform.position, 3.5f);
                var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
                int workers = companions != null ? companions.Length : 0;
                GUI.enabled = nearTree != null && workers > 0;
                string label = nearTree != null
                    ? $"Chop Tree (send {workers} companions)"
                    : "Chop Tree (no tree in range)";
                if (GUI.Button(new Rect(20, y, 220, 30), label))
                {
                    foreach (var c in companions)
                    {
                        if (c != null) c.AssignGather(nearTree);
                    }
                }
                GUI.enabled = true;
            }
        }

        private static Gatherable FindNearestTreeInRange(Vector3 center, float range)
        {
            var all = Object.FindObjectsByType<Gatherable>(FindObjectsSortMode.None);
            Gatherable best = null;
            float bestDist = range;
            foreach (var g in all)
            {
                if (g == null || g.YieldKind != ResourceKind.Wood) continue;
                float d = Vector2.Distance(center, g.transform.position);
                if (d < bestDist) { best = g; bestDist = d; }
            }
            return best;
        }

        private static RecruitableNpc FindNearestRecruitable(Vector3 center, float range)
        {
            var all = Object.FindObjectsByType<RecruitableNpc>(FindObjectsSortMode.None);
            RecruitableNpc best = null;
            float bestDist = range;
            foreach (var n in all)
            {
                if (n == null) continue;
                float d = Vector2.Distance(center, n.transform.position);
                if (d < bestDist) { best = n; bestDist = d; }
            }
            return best;
        }

        private void SpawnBarricade(Vector3 playerPos)
        {
            var go = new GameObject("Barricade");
            go.transform.position = playerPos + new Vector3(0f, 1.2f, 0f);
            go.transform.localScale = new Vector3(1.2f, 0.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.45f, 0.28f, 0.15f);
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.2f, 0.1f, 0.05f, 1f);

            var b = go.AddComponent<Building>();
            b.Kind = BuildingKind.Barricade;
        }

        private void DrawRightPanel()
        {
            const int W = 240;
            int rx = Screen.width - W - 10;
            GUI.Box(new Rect(rx, 10, W, 240), "");
            int y = 18;
            GUI.Label(new Rect(rx + 10, y, W - 20, 24), "=== Resources ===", _titleStyle); y += 26;

            var session = GameSession.Instance;
            if (session != null)
            {
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Wood       {session.Resources.Get(ResourceKind.Wood)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Stone      {session.Resources.Get(ResourceKind.Stone)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Meat       {session.Resources.Get(ResourceKind.Meat)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Food       {session.Resources.Get(ResourceKind.Food)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Frostbloom {session.Resources.Get(ResourceKind.Frostbloom)}", _resStyle); y += 22;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Day {session.Cycle.Day}  {session.Cycle.Phase}", _labelStyle); y += 22;

                if (Night != null)
                {
                    GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Zombies: {Night.ActiveZombies}  Pending: {Night.WavePending}", _labelStyle); y += 22;
                }

                // Phase skip debug button
                if (GUI.Button(new Rect(rx + 10, y, W - 20, 26), "▶ Skip Phase (debug)"))
                {
                    session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
                }
            }
            else
            {
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), "GameSession: NOT FOUND", _labelStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } };
            _weaponStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.95f, 1f) } };
            _resStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = new Color(0.95f, 0.95f, 0.85f) } };
        }

        private void SpawnCampfire(Vector3 playerPos)
        {
            var go = new GameObject("Campfire");
            go.transform.position = playerPos + new Vector3(1.2f, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.5f, 0.1f);
            cf.Shape = FallbackShape.Rounded;
            cf.Circle = false;
            cf.PixelSize = 64;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.3f, 0.1f, 0f, 1f);

            var aura = go.AddComponent<CampfireAura>();
            aura.Radius = 2.5f;
            aura.DamagePerSecond = 6f;
            aura.TickInterval = 0.5f;
        }
    }
}
