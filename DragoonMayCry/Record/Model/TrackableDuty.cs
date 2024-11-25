namespace DragoonMayCry.Record.Model
{
    public class TrackableDuty(
        string name,
        int lvlSync,
        Difficulty difficulty,
        Category category,
        string? subcategory,
        uint iconId)
    {
        public string Name { get; private set; } = name;
        public int LvlSync { get; private set; } = lvlSync;
        public Difficulty Difficulty { get; private set; } = difficulty;
        public Category Category { get; private set; } = category;
        public string Subcategory { get; private set; } = subcategory ?? "";
        public uint IconId { get; private set; } = iconId;
    }
}
