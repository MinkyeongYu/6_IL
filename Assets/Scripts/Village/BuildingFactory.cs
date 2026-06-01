using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 모든 건물 프리팹/런타임 스폰을 한 곳에 모아둔 정적 팩토리.
    /// SimpleHud / ConstructionSite OnComplete / PrefabGenerator 모두 여기서 호출.
    /// </summary>
    public static class BuildingFactory
    {
        private static void ApplySprite(SpriteRenderer sr, Sprite spr)
        {
            if (spr != null) sr.sprite = spr;
        }

        public static GameObject SpawnBarricade(Vector3 pos)
        {
            var go = new GameObject("Barricade");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.WoodBarricade());
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.45f, 0.28f, 0.15f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Barricade;
            return go;
        }

        public static GameObject SpawnHouse(Vector3 pos)
        {
            var go = new GameObject("House");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.1f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Cabin());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.85f, 0.6f, 0.4f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.4f, 0.2f, 0.1f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.House;
            return go;
        }

        public static GameObject SpawnStorage(Vector3 pos)
        {
            var go = new GameObject("Storage");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Logs());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.45f, 0.3f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.18f, 0.1f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Storage;
            return go;
        }

        public static GameObject SpawnFarm(Vector3 pos)
        {
            var go = new GameObject("Farm");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.35f, 0.55f, 0.25f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.18f, 0.3f, 0.12f, 1f);
            go.AddComponent<FarmBuilding>();
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Farm;
            return go;
        }

        public static GameObject SpawnWatchtower(Vector3 pos)
        {
            var go = new GameObject("Watchtower");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.7f, 1.4f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            ApplySprite(sr, SpriteBank.Watchtower());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.5f, 0.4f, 0.28f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 32;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.15f, 0.08f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Watchtower;
            go.AddComponent<Watchtower>();
            return go;
        }

        public static GameObject SpawnInfirmary(Vector3 pos)
        {
            var go = new GameObject("Infirmary");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.95f, 0.97f, 0.95f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.5f, 0.7f, 0.5f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.Infirmary;
            go.AddComponent<HealingShrine>();
            return go;
        }

        public static GameObject SpawnHuntersHut(Vector3 pos)
        {
            var go = new GameObject("HuntersHut");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Cabin());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.55f, 0.4f, 0.25f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.15f, 0.05f, 1f);
            var b = go.AddComponent<Building>(); b.Kind = BuildingKind.HuntersHut;
            return go;
        }
    }
}
