namespace SharePointAnalyserDemo.Entities
{
    public class SPSite
    {
        public string siteName {  get; set; }
        public string siteUrl {  get; set; }
        public int subsitesCount {  get; set; }
        public int listsCount { get; set; }
        public int librariesCount {  get; set; }
    }
}
