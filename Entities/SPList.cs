namespace SharePointAnalyserDemo.Entities
{
    public class SPList
    {
        public string Name { get; set; }
        public int ItemsCount { get; set; }
        public string Size { get; set; }
        public bool ItemHasUniquePermissions { get; set; }
        public string LastModifiedDate { get; set; }
    }
}
