namespace SharePointAnalyserDemo.Entities.Inventory
{
    public class TenantInventory
    {
        public int SitesCount {  get; set; }
        public List<SiteInventory> sites { get; set; } = new();
    }
}
