namespace DragoonMayCry.Record.Model
{
    public class ExtensionCategory(Category type, string[] subcategories)
    {
        public Category Type { get; private set; } = type;
        public string[] Subcategories { get; private set; } = subcategories;
    }
}
