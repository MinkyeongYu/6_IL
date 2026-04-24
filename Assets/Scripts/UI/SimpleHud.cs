using UnityEngine;

namespace IL6
{
    /// <summary>
    /// IMGUI 기반 임시 HUD. 자원/HP/위치/채집진행률 표시 + 모닥불 빌드 버튼.
    /// Canvas/TMP 셋업 없이 즉시 작동. 최종 UI 교체 전 디버그·검증용.
    /// </summary>
    public sealed class SimpleHud : MonoBehaviour
    {
        public PlayerController Player;
        public GatherController Gather;

        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;

        private void OnGUI()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } };
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } };
            }

            GUI.Box(new Rect(10, 10, 260, 240), "");
            int y = 18;
            GUI.Label(new Rect(20, y, 240, 24), "=== IL6 Snowfield ===", _titleStyle); y += 26;

            var session = GameSession.Instance;
            if (session != null)
            {
                GUI.Label(new Rect(20, y, 240, 22), $"Wood:  {session.Resources.Get(ResourceKind.Wood)}", _labelStyle); y += 20;
                GUI.Label(new Rect(20, y, 240, 22), $"Meat:  {session.Resources.Get(ResourceKind.Meat)}", _labelStyle); y += 20;
                GUI.Label(new Rect(20, y, 240, 22), $"Food:  {session.Resources.Get(ResourceKind.Food)}", _labelStyle); y += 20;
                GUI.Label(new Rect(20, y, 240, 22), $"Day {session.Cycle.Day}  Phase: {session.Cycle.Phase}", _labelStyle); y += 22;
            }
            else
            {
                GUI.Label(new Rect(20, y, 240, 22), "GameSession: NOT FOUND", _labelStyle); y += 22;
            }

            if (Player != null)
            {
                GUI.Label(new Rect(20, y, 240, 22), $"HP: {Player.CurrentHp} / {Player.MaxHp}", _labelStyle); y += 20;
                var p = Player.transform.position;
                GUI.Label(new Rect(20, y, 240, 22), $"Pos: ({p.x:F1}, {p.y:F1})", _labelStyle); y += 22;
            }
            else
            {
                GUI.Label(new Rect(20, y, 240, 22), "Player: NULL", _labelStyle); y += 22;
            }

            if (Gather != null && Gather.IsActive)
            {
                GUI.Label(new Rect(20, y, 240, 22), $"Gathering: {(Gather.Progress * 100):F0}%", _labelStyle); y += 22;
            }

            GUI.enabled = session != null && Player != null && session.Resources.Get(ResourceKind.Wood) >= 5;
            if (GUI.Button(new Rect(20, y + 4, 200, 30), "Build Campfire (5 Wood)"))
            {
                if (session.Resources.Spend(ResourceKind.Wood, 5))
                {
                    SpawnCampfire(Player.transform.position);
                }
            }
            GUI.enabled = true;
        }

        private void SpawnCampfire(Vector3 playerPos)
        {
            var go = new GameObject("Campfire");
            go.transform.position = playerPos + new Vector3(1.2f, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            sr.color = new Color(1f, 0.5f, 0.1f);
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.5f, 0.1f);
            cf.Circle = false;
            cf.PixelSize = 64;
        }
    }
}
