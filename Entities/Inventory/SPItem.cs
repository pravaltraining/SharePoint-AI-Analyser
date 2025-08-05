namespace SharePointAnalyserDemo.Entities.Inventory
{
    public class SPItem
    {
        public string Name { get; set; }

        public int Size { get; set; }

        public string LastModified { get; set; }

        public string ContentType {  get; set; }

        public bool isVersionInfoIncluded {  get; set; }

        public int TotalVersions { get; set; }
    }
}
