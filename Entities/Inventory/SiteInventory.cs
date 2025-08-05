namespace SharePointAnalyserDemo.Entities.Inventory
{
    public class SiteInventory : SPSite
    {
        public List<SIList> Lists { get; set; } = new();
        public List<SIList> Libraries { get; set; } = new();
        public List<SiteInventory> SubSites { get; set; } = new();
    }
}
