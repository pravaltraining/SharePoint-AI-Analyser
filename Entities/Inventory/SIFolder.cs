namespace SharePointAnalyserDemo.Entities.Inventory
{
    public class SIFolder
    {
        public List<SIFolder> Folders { get; set; } = new();

        public List<SPItem> Items { get; set; } = new();
    }
}