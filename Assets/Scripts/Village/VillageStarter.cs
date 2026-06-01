using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 게임 시작 시 마을 자리에 모닥불 + 울타리 링을 자동 스폰.
    /// SnowfieldController 가 Start 에서 1회 호출. 이미 마을이 있으면(저장 데이터로 복귀 등) 스킵.
    /// </summary>
    public static class VillageStarter
    {
        /// <summary>
        /// 중심점 주변에 모닥불 + 사각 울타리 + 남쪽 문(플레이어만 통과) 스폰.
        /// 이미 근처에 모닥불 있으면 스킵.
        /// </summary>
        /// <summary>현재 마을 펜스 사각형 반경 — 건물 추가 시 단계적으로 확장.</summary>
        public static float CurrentHalfSize { get; private set; } = 5f;
        public const float MinHalfSize = 5f;
        public const float MaxHalfSize = 14f;
        public const float HalfSizePerBuilding = 0.4f; // 비-펜스 건물 1당 +0.4u

        /// <summary>비-펜스 건물 수에 따른 목표 halfSize.</summary>
        public static float TargetHalfSize()
        {
            int built = 0;
            var bs = UnityEngine.Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.CurrentHp <= 0) continue;
                if (b.Kind == BuildingKind.Fence) continue;
                built++;
            }
            return Mathf.Min(MaxHalfSize, MinHalfSize + (built - 1) * HalfSizePerBuilding);
        }

        /// <summary>새 건물 짓고 나면 호출 — 새 외곽 반경에 맞춰 펜스 추가 + 새 게이트.</summary>
        public static void OnBuildingAdded(Vector3 center)
        {
            float target = TargetHalfSize();
            if (target <= CurrentHalfSize + 0.05f) return;
            CurrentHalfSize = target;
            BuildOuterRing(center, target);
        }

        private static void BuildOuterRing(Vector3 center, float halfSize, float spacing = 1.0f)
        {
            int slots = Mathf.Max(3, Mathf.RoundToInt((halfSize * 2f) / spacing) + 1);
            int gateSlot = slots / 2;
            float startOffset = -halfSize;

            // 남쪽 — 가운데는 게이트
            for (int i = 0; i < slots; i++)
            {
                float lx = startOffset + i * spacing;
                Vector3 pos = center + new Vector3(lx, -halfSize, 0f);
                if (TooCloseToFence(pos, 0.4f)) continue;
                if (i == gateSlot && !HasGateNear(pos, 1.2f)) SpawnGate(pos);
                else if (i != gateSlot) SpawnFence(pos, 0f);
            }
            // 북쪽
            for (int i = 0; i < slots; i++)
            {
                float lx = startOffset + i * spacing;
                Vector3 pos = center + new Vector3(lx, halfSize, 0f);
                if (TooCloseToFence(pos, 0.4f)) continue;
                SpawnFence(pos, 0f);
            }
            // 동/서
            for (int i = 0; i < slots; i++)
            {
                float ly = startOffset + i * spacing;
                Vector3 wpos = center + new Vector3(-halfSize, ly, 0f);
                Vector3 epos = center + new Vector3(halfSize, ly, 0f);
                if (!TooCloseToFence(wpos, 0.4f)) SpawnFence(wpos, 90f);
                if (!TooCloseToFence(epos, 0.4f)) SpawnFence(epos, 90f);
            }
        }

        private static bool TooCloseToFence(Vector3 pos, float radius)
        {
            var bs = UnityEngine.Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in bs)
            {
                if (b == null || b.Kind != BuildingKind.Fence) continue;
                if (Vector2.Distance(pos, b.transform.position) < radius) return true;
            }
            return false;
        }

        private static bool HasGateNear(Vector3 pos, float radius)
        {
            var ds = UnityEngine.Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
            foreach (var d in ds)
                if (d != null && Vector2.Distance(pos, d.transform.position) < radius) return true;
            return false;
        }

        public static void SpawnStarterVillage(Vector3 center, float halfSize = 5f, float spacing = 1.0f)
        {
            CurrentHalfSize = halfSize;
            // 이미 모닥불이 가까이 있으면 (씬 재진입 등) 추가 스폰 안 함
            var existing = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in existing)
            {
                if (b == null) continue;
                if (b.Kind != BuildingKind.Campfire) continue;
                if (Vector2.Distance(center, b.transform.position) < 6f) return;
            }

            SpawnCampfire(center);

            // 사각형 울타리 — 4 면. 각 면의 중앙 슬롯은 비워서 (남쪽만) 문으로 대체.
            int slotsPerSide = Mathf.Max(3, Mathf.RoundToInt((halfSize * 2f) / spacing) + 1);
            int gateSlot = slotsPerSide / 2; // 가운데
            float startOffset = -halfSize;

            // 남쪽 변 (y = center.y - halfSize) — 가운데 한 칸은 문
            for (int i = 0; i < slotsPerSide; i++)
            {
                float lx = startOffset + i * spacing;
                Vector3 pos = center + new Vector3(lx, -halfSize, 0f);
                if (i == gateSlot) SpawnGate(pos);
                else SpawnFence(pos, 0f);
            }
            // 북쪽 변
            for (int i = 0; i < slotsPerSide; i++)
            {
                float lx = startOffset + i * spacing;
                Vector3 pos = center + new Vector3(lx, halfSize, 0f);
                SpawnFence(pos, 0f);
            }
            // 서쪽/동쪽 — 코너 포함 전 구간. 모서리에서 가로 펜스와 시각적으로 겹치지만 빈틈 없음.
            for (int i = 0; i < slotsPerSide; i++)
            {
                float ly = startOffset + i * spacing;
                Vector3 wpos = center + new Vector3(-halfSize, ly, 0f);
                Vector3 epos = center + new Vector3(halfSize, ly, 0f);
                SpawnFence(wpos, 90f);
                SpawnFence(epos, 90f);
            }
        }

        public static GameObject SpawnGate(Vector3 pos)
        {
            // 문 — 플레이어/동료는 통과(Door.IgnoreCollision), 좀비는 차단
            var go = new GameObject("Gate");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 0.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.95f, 0.8f);

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.78f, 0.62f, 0.30f); // 골드 — 입구임을 표시
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.35f, 0.25f, 0.1f, 1f);

            go.AddComponent<Door>();

            // 좌우 기둥 (시각용 스프라이트)
            for (int s = -1; s <= 1; s += 2)
            {
                var post = new GameObject("GatePost");
                post.transform.SetParent(go.transform, false);
                post.transform.localPosition = new Vector3(s * 0.5f, 0.7f, 0f);
                post.transform.localScale = new Vector3(0.18f, 4.5f, 1f); // 부모 스케일 0.95×0.18 보정
                var psr = post.AddComponent<SpriteRenderer>();
                psr.sortingOrder = 4;
                var pcf = post.AddComponent<ColorFallback>();
                pcf.Tint = new Color(0.55f, 0.4f, 0.22f);
                pcf.Shape = FallbackShape.Square;
                pcf.Circle = false;
                pcf.PixelSize = 16;
                pcf.OutlineWidth = 1;
                pcf.OutlineColor = new Color(0.2f, 0.13f, 0.05f, 1f);
            }
            return go;
        }

        public static GameObject SpawnFence(Vector3 pos, float rotDeg)
        {
            var go = new GameObject("Fence");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, rotDeg);
            // 수평 펜스 스프라이트(64px@64PPU=1unit) 기준: 1.0u 폭 × 0.4u 높이
            go.transform.localScale = new Vector3(1.0f, 0.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            // 수평 방향 스프라이트 사용 — 폭이 넓은 판자 형태
            var fSpr = SpriteBank.SnowFenceH();
            if (fSpr != null) sr.sprite = fSpr;

            var col = go.AddComponent<BoxCollider2D>();
            // 시각 크기보다 살짝 작게 — 플레이어가 문 옆에서 걸리지 않도록
            col.size = new Vector2(0.9f, 0.8f);

            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.4f, 0.22f);
            cf.Shape = FallbackShape.Square;
            cf.Circle = false;
            cf.PixelSize = 32;
            cf.OutlineWidth = 2;
            cf.OutlineColor = new Color(0.2f, 0.13f, 0.05f, 1f);

            var b = go.AddComponent<Building>();
            b.Kind = BuildingKind.Fence;
            return go;
        }

        public static GameObject SpawnCampfire(Vector3 pos)
        {
            var go = new GameObject("Campfire");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.9f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            var cSpr = SpriteBank.Campfire();
            if (cSpr != null) sr.sprite = cSpr;

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
            return go;
        }
    }
}
