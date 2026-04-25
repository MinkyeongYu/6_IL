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
        public PlayerProgression Progression;

        private System.Collections.Generic.List<RuneKind> _runeOffer;

        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _weaponStyle;
        private GUIStyle _resStyle;

        private void OnGUI()
        {
            EnsureStyles();
            DrawLeftPanel();
            DrawRightPanel();
            DrawWorldChopButton();
            DrawRecruitDialog();
            DrawRuneModal();
            DrawDeathOverlay();
        }

        private void DrawLeftPanel()
        {
            GUI.Box(new Rect(10, 10, 280, 420), "");
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

            if (Progression != null)
            {
                GUI.Label(new Rect(20, y, 260, 22), $"Lv {Progression.Level}    XP {Progression.Xp}/{Progression.XpToNext}", _weaponStyle); y += 20;
                float pct = Progression.XpToNext > 0 ? (float)Progression.Xp / Progression.XpToNext : 0f;
                GUI.Box(new Rect(20, y, 200, 10), "");
                GUI.DrawTexture(new Rect(22, y + 2, 196 * pct, 6), Texture2D.whiteTexture);
                y += 16;
            }

            if (Attacker != null && Attacker.Weapon != null)
            {
                var w = Attacker.Weapon;
                if (GUI.Button(new Rect(20, y, 24, 22), "<")) Attacker.CycleWeapon(-1);
                GUI.Label(new Rect(48, y, 200, 22), $"[Weapon] {w.DisplayName}", _weaponStyle);
                if (GUI.Button(new Rect(248, y, 24, 22), ">")) Attacker.CycleWeapon(+1);
                y += 22;
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

            GUI.enabled = session != null && Player != null && session.Resources.Get(ResourceKind.Wood) >= 8;
            if (GUI.Button(new Rect(20, y, 220, 30), "Build Storage (8 Wood, +50 cap)"))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 8))
                {
                    SpawnStorage(Player.transform.position);
                    session.Resources.IncreaseCap(50);
                }
            }
            GUI.enabled = true;
            y += 34;

            GUI.enabled = session != null && Player != null && session.Resources.Get(ResourceKind.Wood) >= 6;
            if (GUI.Button(new Rect(20, y, 220, 30), "Build Farm (6 Wood, +1 Food/12s)"))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 6))
                {
                    SpawnFarm(Player.transform.position);
                }
            }
            GUI.enabled = true;
        }

        // 나무 위에 떠 있는 "Chop Tree" 월드 스페이스 버튼.
        private void DrawWorldChopButton()
        {
            if (Player == null) return;
            var tree = FindNearestTreeInRange(Player.transform.position, 3.5f);
            if (tree == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var worldAnchor = tree.transform.position + new Vector3(0f, 0.8f, 0f);
            Vector3 sp = cam.WorldToScreenPoint(worldAnchor);
            if (sp.z < 0) return;
            float guiY = Screen.height - sp.y;

            var companions = Object.FindObjectsByType<Companion>(FindObjectsSortMode.None);
            int workers = companions.Length;
            string label = workers > 0 ? $"Chop Tree ({workers})" : "Chop (no crew)";

            var rect = new Rect(sp.x - 70, guiY - 16, 140, 30);
            GUI.enabled = workers > 0;
            if (GUI.Button(rect, label))
            {
                foreach (var c in companions) if (c != null) c.AssignGather(tree);
            }
            GUI.enabled = true;
        }

        // 하단 중앙 영입 다이얼로그: 초상 + 이름 + 대사 + 스탯 + Accept/Reject
        private void DrawRecruitDialog()
        {
            if (Player == null) return;
            var npc = FindNearestRecruitable(Player.transform.position, 2.2f);
            if (npc == null) return;

            const int W = 520;
            const int H = 150;
            int x = Screen.width / 2 - W / 2;
            int y = Screen.height - H - 20;

            GUI.Box(new Rect(x, y, W, H), "");

            var sr = npc.GetComponent<SpriteRenderer>();
            var col = sr != null ? sr.color : Color.white;
            var portrait = new Rect(x + 12, y + 12, 126, 126);
            var oldC = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(portrait, Texture2D.whiteTexture);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(portrait.x, portrait.y, portrait.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(portrait.x, portrait.yMax - 2, portrait.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(portrait.x, portrait.y, 2, portrait.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(portrait.xMax - 2, portrait.y, 2, portrait.height), Texture2D.whiteTexture);
            GUI.color = oldC;

            int tx = x + 152;
            int tw = W - 164;

            GUI.Label(new Rect(tx, y + 12, tw, 24), $"{npc.DisplayNamePublic} ({npc.Role})", _titleStyle);
            GUI.Label(new Rect(tx, y + 40, tw, 44), $"\"{npc.DialogText}\"", _labelStyle);
            GUI.Label(new Rect(tx, y + 84, tw, 20), $"전투 {Stars(npc.CombatRating)}", _labelStyle);
            GUI.Label(new Rect(tx, y + 104, tw, 20), $"농사 {Stars(npc.FarmRating)}", _labelStyle);

            if (GUI.Button(new Rect(tx, y + H - 34, 110, 26), "영입 (F)"))
            {
                npc.Recruit();
            }
            if (GUI.Button(new Rect(tx + 120, y + H - 34, 110, 26), "거절"))
            {
                // 대화 닫기는 범위 벗어나면 자동
            }
        }

        private static string Stars(int n)
        {
            n = Mathf.Clamp(n, 0, 5);
            return new string('★', n) + new string('☆', 5 - n);
        }

        private GUIStyle _bigStyle;
        // 사망 시 풀스크린 검은 오버레이 + Restart 버튼
        private void DrawDeathOverlay()
        {
            if (Player == null || !Player.IsDead) return;
            Time.timeScale = 0f;

            var dim = new Rect(0, 0, Screen.width, Screen.height);
            var oldC = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(dim, Texture2D.whiteTexture);
            GUI.color = oldC;

            if (_bigStyle == null)
            {
                _bigStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.25f, 0.25f) },
                };
            }
            GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 100), "YOU DIED", _bigStyle);

            int bx = Screen.width / 2 - 80;
            int by = Screen.height / 2 + 40;
            if (GUI.Button(new Rect(bx, by, 160, 44), "Restart"))
            {
                Time.timeScale = 1f;
                if (GameSession.Instance != null) GameSession.Instance.HardReset();
            }
        }

        // 레벨업 시 화면 가운데 모달: 3개 룬 중 선택
        private void DrawRuneModal()
        {
            if (Progression == null || !Progression.LevelUpPending)
            {
                _runeOffer = null;
                Time.timeScale = 1f;
                return;
            }

            if (_runeOffer == null)
            {
                _runeOffer = Progression.PickThreeOffer((uint)(Time.frameCount + Progression.Level * 113));
            }
            Time.timeScale = 0f;

            // 어두운 배경
            var dim = new Rect(0, 0, Screen.width, Screen.height);
            var oldC = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(dim, Texture2D.whiteTexture);
            GUI.color = oldC;

            const int W = 600;
            const int H = 240;
            int x = Screen.width / 2 - W / 2;
            int y = Screen.height / 2 - H / 2;
            GUI.Box(new Rect(x, y, W, H), "");
            GUI.Label(new Rect(x, y + 12, W, 28), $"LEVEL {Progression.Level} — 룬 선택", _titleStyle);

            int btnW = 180;
            int btnH = 140;
            int gap = 12;
            int total = btnW * 3 + gap * 2;
            int bx = x + (W - total) / 2;
            int by = y + 60;
            for (int i = 0; i < _runeOffer.Count; i++)
            {
                var rune = _runeOffer[i];
                var rect = new Rect(bx + i * (btnW + gap), by, btnW, btnH);
                if (GUI.Button(rect, ""))
                {
                    Progression.ApplyRune(rune);
                    _runeOffer = null;
                    return;
                }
                GUI.Label(new Rect(rect.x + 8, rect.y + 12, rect.width - 16, 24), rune.ToString(), _weaponStyle);
                GUI.Label(new Rect(rect.x + 8, rect.y + 44, rect.width - 16, 80), PlayerProgression.Describe(rune), _labelStyle);
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

            var hp = go.AddComponent<HpBarUi>();
            hp.Building = b;
            hp.Offset = new Vector2(0f, 0.6f);
            hp.Size = new Vector2(1.0f, 0.1f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = new Color(0.6f, 0.4f, 0.2f);
        }

        private void DrawRightPanel()
        {
            const int W = 240;
            int rx = Screen.width - W - 10;
            GUI.Box(new Rect(rx, 10, W, 290), "");
            int y = 18;
            GUI.Label(new Rect(rx + 10, y, W - 20, 24), "=== Resources ===", _titleStyle); y += 26;

            var session = GameSession.Instance;
            if (session != null)
            {
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Wood       {session.Resources.Get(ResourceKind.Wood)}/{session.Resources.GetCap(ResourceKind.Wood)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Stone      {session.Resources.Get(ResourceKind.Stone)}/{session.Resources.GetCap(ResourceKind.Stone)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Meat       {session.Resources.Get(ResourceKind.Meat)}/{session.Resources.GetCap(ResourceKind.Meat)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Food       {session.Resources.Get(ResourceKind.Food)}/{session.Resources.GetCap(ResourceKind.Food)}", _resStyle); y += 20;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Frostbloom {session.Resources.Get(ResourceKind.Frostbloom)}/{session.Resources.GetCap(ResourceKind.Frostbloom)}", _resStyle); y += 22;
                GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Day {session.Cycle.Day}  {session.Cycle.Phase}", _labelStyle); y += 22;
                if (session.LastFoodShortage > 0)
                {
                    GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"⚠ 식량 부족 {session.LastFoodShortage}!", _weaponStyle); y += 22;
                }

                if (Night != null)
                {
                    GUI.Label(new Rect(rx + 10, y, W - 20, 22), $"Zombies: {Night.ActiveZombies}  Pending: {Night.WavePending}", _labelStyle); y += 22;
                    if (Night.IsBlizzard)
                    {
                        GUI.Label(new Rect(rx + 10, y, W - 20, 22), "❄ BLIZZARD — 모닥불 밖 위험", _weaponStyle); y += 22;
                    }
                }

                // Phase skip debug
                if (GUI.Button(new Rect(rx + 10, y, (W - 30) / 2, 26), "▶ Skip"))
                {
                    session.Cycle.Update(session.Cycle.PhaseDurationSec + 0.1f);
                }
                if (GUI.Button(new Rect(rx + 10 + (W - 30) / 2 + 10, y, (W - 30) / 2, 26), "Force Night"))
                {
                    if (Night != null) Night.StartNight(session.Cycle.Day);
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

        private void SpawnStorage(Vector3 playerPos)
        {
            var go = new GameObject("Storage");
            go.transform.position = playerPos + new Vector3(-1.4f, 0f, 0f);
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.45f, 0.3f);
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.25f, 0.18f, 0.1f, 1f);
        }

        private void SpawnFarm(Vector3 playerPos)
        {
            var go = new GameObject("Farm");
            go.transform.position = playerPos + new Vector3(0f, -1.4f, 0f);
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.35f, 0.55f, 0.25f);
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.18f, 0.3f, 0.12f, 1f);
            go.AddComponent<FarmBuilding>();
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

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            var aura = go.AddComponent<CampfireAura>();
            aura.Radius = 2.5f;
            aura.DamagePerSecond = 6f;
            aura.TickInterval = 0.5f;

            var b = go.AddComponent<Building>();
            b.Kind = BuildingKind.Campfire;

            var hp = go.AddComponent<HpBarUi>();
            hp.Building = b;
            hp.Offset = new Vector2(0f, 0.7f);
            hp.Size = new Vector2(1.0f, 0.12f);
            hp.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            hp.FillColor = new Color(1f, 0.55f, 0.2f);
        }
    }
}
