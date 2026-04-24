namespace IL6
{
    public static class GameConstants
    {
        public const int GameWidth = 960;
        public const int GameHeight = 540;
        public const int TileSize = 32;
        public const int VillageGridSize = 24;
        public const int SimTickHz = 30;

        public const float VillageCenterX = (VillageGridSize * TileSize) / 2f; // 384
        public const float VillageCenterY = (VillageGridSize * TileSize) / 2f; // 384
    }
}
