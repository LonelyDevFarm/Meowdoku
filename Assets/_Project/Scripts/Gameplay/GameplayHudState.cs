namespace Meowdoku.Gameplay
{
    public readonly struct GameplayHudState
    {
        public GameplayHudState(int level, int placedCats, int totalCats)
        {
            Level = level;
            PlacedCats = placedCats;
            TotalCats = totalCats;
        }

        public int Level { get; }
        public int PlacedCats { get; }
        public int TotalCats { get; }
    }
}
