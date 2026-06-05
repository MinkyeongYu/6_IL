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
            go.AddComponent<Building>().Initialize(BuildingKind.Barricade);
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
            go.AddComponent<Building>().Initialize(BuildingKind.House);
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
            go.AddComponent<Building>().Initialize(BuildingKind.Storage);
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
            go.AddComponent<Building>().Initialize(BuildingKind.Farm);
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
            go.AddComponent<Building>().Initialize(BuildingKind.Watchtower);
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
            go.AddComponent<Building>().Initialize(BuildingKind.Infirmary);
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
            go.AddComponent<Building>().Initialize(BuildingKind.HuntersHut);
            return go;
        }

        public static GameObject SpawnSeedStorage(Vector3 pos)
        {
            var go = new GameObject("SeedStorage");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Logs());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.62f, 0.52f, 0.28f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.18f, 0.08f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.SeedStorage);
            return go;
        }

        public static GameObject SpawnCarpenter(Vector3 pos)
        {
            var go = new GameObject("Carpenter");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Logs());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.58f, 0.38f, 0.18f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.22f, 0.12f, 0.05f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.Carpenter);
            return go;
        }

        public static GameObject SpawnBrazier(Vector3 pos)
        {
            var go = new GameObject("Brazier");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            ApplySprite(sr, SpriteBank.Campfire());
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(1f, 0.62f, 0.16f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.35f, 0.16f, 0.04f, 1f);
            var aura = go.AddComponent<CampfireAura>();
            aura.ApplyBuildingLevel(BuildingKind.Brazier, 1);
            go.AddComponent<Building>().Initialize(BuildingKind.Brazier);
            return go;
        }

        public static GameObject SpawnBlacksmith(Vector3 pos)
        {
            var go = new GameObject("Blacksmith");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Cabin());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.42f, 0.34f, 0.32f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.7f, 0.22f, 0.08f, 1f);
            var aura = go.AddComponent<CampfireAura>();
            aura.ApplyBuildingLevel(BuildingKind.Blacksmith, 1);
            go.AddComponent<Building>().Initialize(BuildingKind.Blacksmith);
            return go;
        }

        public static GameObject SpawnTrainingCamp(Vector3 pos)
        {
            var go = new GameObject("TrainingCamp");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.1f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.WoodBarricade());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.62f, 0.28f, 0.18f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.25f, 0.08f, 0.04f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.TrainingCamp);
            return go;
        }

        public static GameObject SpawnFoodStorage(Vector3 pos)
        {
            var go = new GameObject("FoodStorage");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.0f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Logs());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.82f, 0.62f, 0.28f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.32f, 0.2f, 0.08f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.FoodStorage);
            return go;
        }

        public static GameObject SpawnLookoutPost(Vector3 pos)
        {
            var go = new GameObject("LookoutPost");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.65f, 1.25f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            ApplySprite(sr, SpriteBank.Watchtower());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.42f, 0.52f, 0.58f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.12f, 0.18f, 0.22f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.LookoutPost);
            return go;
        }

        public static GameObject SpawnSawmill(Vector3 pos)
        {
            var go = new GameObject("Sawmill");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.15f, 0.9f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Logs());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.5f, 0.32f, 0.16f);
            cf.Shape = FallbackShape.Square; cf.Circle = false; cf.PixelSize = 48;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.2f, 0.1f, 0.04f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.Sawmill);
            return go;
        }

        public static GameObject SpawnChurch(Vector3 pos)
        {
            var go = new GameObject("Church");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.05f, 1.15f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            ApplySprite(sr, SpriteBank.Cabin());
            var col = go.AddComponent<BoxCollider2D>(); col.size = Vector2.one;
            var cf = go.AddComponent<ColorFallback>();
            cf.Tint = new Color(0.78f, 0.78f, 0.95f);
            cf.Shape = FallbackShape.Rounded; cf.Circle = false; cf.PixelSize = 64;
            cf.OutlineWidth = 2; cf.OutlineColor = new Color(0.42f, 0.36f, 0.72f, 1f);
            go.AddComponent<Building>().Initialize(BuildingKind.Church);
            return go;
        }
    }
}
