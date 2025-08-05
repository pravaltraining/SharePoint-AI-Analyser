namespace SharePointAnalyserDemo.Entities.Inventory
{
    public class SPInventory<T> where T : class
    {
        public string AnalysisOn { get; set; }

        public T Data { get; set; }
    }
}
