namespace SharePointAnalyserDemo
{
    public class ConnectedSiteInfo
    {
        public string ConnectedSiteName { get; set; }
        public string ConnectedSiteUrl { get; set; }
        public int SubSitesCount { get; set; }
        public string SiteId { get; set; }
        public string ServerRelativeUrl { get; set; }
        public int ListsCount { get; set; }
        public int LibraryCount { get; set; }
        public string webId { get; set; }
        public List<SPList> Lists { get; set; } = new();
        public List<SPLibrary> Libraries { get; set; } = new();
        public List<Subsite> Subsites { get; set; } = new();
    }

    public class SPList
    {
        public string Name { get; set; }
        public int ItemsCount { get; set; }
        public string Size { get; set; }
        public int foldersCount { get; set; }
        public bool hasUniquePermissions {  get; set; }
        public bool hasCheckedOutFiles { get; set; }
    }

    public class SPLibrary
    {
        public string Name { get; set; }
        public int FilesCount { get; set; }
        public string Size { get; set; }
        public int foldersCount { get; set; }
        public bool hasUniquePermissions {  get; set; }
        public bool hasCheckedOutFiles { get; set; }
    }

    public class Subsite
    {
        public string SubsiteName { get; set; }
        public string SubsiteSiteUrl { get; set; }
        public int SubSitesCount { get; set; }
        public string ServerRelativeUrl { get; set; }
        public int ListsCount { get; set; }
        public int LibraryCount { get; set; }
        public string webId { get; set; }

        public List<SPList> Lists { get; set; } = new();
        public List<SPLibrary> Libraries { get; set; } = new();
        public List<Subsite> Subsites { get; set; } = new();
    }
}
