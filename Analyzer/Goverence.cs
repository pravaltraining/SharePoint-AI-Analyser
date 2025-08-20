
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System.Text;

namespace SharePointAnalyserDemo.Analyzer
{
    public class Goverence 
    {
        private readonly Utility _utility;
        private readonly string _connectionString;
        private static readonly HttpClient client = new HttpClient();
        private readonly string ApiKey;
        public Goverence()
        {
            _utility = new Utility(Configuration.GovernanceConnectionString);
            _connectionString = Configuration.GovernanceConnectionString;
            ApiKey = Configuration.ApiKey;
        }

        public async Task<string> Analyse(Object siteInfo)
        {
            string siteDataJson = JsonConvert.SerializeObject(siteInfo, Formatting.None);

            //string prompt = $@"
            //            Assume you are a SharePoint analyst with over 12 years of experience working with SharePoint 2013, 2016, 2019, and SharePoint Online.
            //            The following JSON represents a SharePoint site's structure. Please analyze the data and provide concise, straightforward recommendations. 
            //            Focus on identifying the top 4 most critical issues and the top 4 most impactful improvements.

            //            Ensure your response is concise and addresses only the highest priority items.

            //            Format your response as follows:
            //            issues:
            //            - List the upto 4 most significant issues, starting with the most critical and ignore minor issues.

            //            improvements:
            //            - Provide the upto 4 most impactful improvements, ranked by priority.

            //            Note: All sizes are in bytes. When providing your response, please convert them as necessary for clarity.

            //            Site data:
            //            {siteDataJson}";

            string prompt = $@"
                    Assume you are a SharePoint analyst with over 12 years of experience working with SharePoint 2013, 2016, 2019, and SharePoint Online.

                    The following JSON represents the structure of a SharePoint site. Analyze the data and identify only the most **critical issues** and **high-impact improvements**, based solely on the current structure.

                    Do not interpret or infer anything from names or labels. Do not reveal or reference any internal properties or criteria used in your assessment.

                    If no critical issues are identified, return:
                    'No major issues detected based on current data.'

                    Provide up to 4 relevant recommendations only if they clearly benefit the performance, user experience, or long-term maintainability of the existing structure.

                    Format your response exactly as follows:

                    issues:
                    - <List up to 4 critical issues, or state: 'No major issues detected based on current data.'>

                    improvements:
                    - <List up to 4 improvements, or leave blank if none>

                    Note: All sizes are in bytes. Convert to MB or GB for readability.

                    Site data:
                    {siteDataJson}";


            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/gemma-3-27b-it:generateContent?key={ApiKey}")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseString);
            string outputText = result?.candidates?[0]?.content?.parts?[0]?.text?.ToString();

            return outputText ?? responseString;
        }

        public Object GetData()
        {
            var connectedSite = new ConnectedSiteInfo
            {
                ConnectedSiteName = "01-07",
                ConnectedSiteUrl = "https://zkny4.sharepoint.com/sites/01-07"
                //ConnectedSiteName = "Governance Dashboard-Subsites",
                //ConnectedSiteUrl = "https://580wlx.sharepoint.com/sites/GovernanceDashboard-Subsites"
            };


            connectedSite.webId = _utility.GetDataFromSqlLite<string>($"SELECT WebId FROM SPO365Web WHERE Url = '{connectedSite.ConnectedSiteUrl}'");

            connectedSite.SubSitesCount = _utility.GetDataFromSqlLite<int>($"SELECT WebsCount FROM SPO365Web WHERE Url = '{connectedSite.ConnectedSiteUrl}'");
            connectedSite.SiteId = _utility.GetDataFromSqlLite<string>($"SELECT SiteId FROM SPO365Web WHERE Url = '{connectedSite.ConnectedSiteUrl}'");
            connectedSite.ServerRelativeUrl = _utility.GetDataFromSqlLite<string>($"SELECT serverRelativeUrl FROM SPO365Web WHERE Url = '{connectedSite.ConnectedSiteUrl}'");

            connectedSite.ListsCount = _utility.GetDataFromSqlLite<int>(
                $"SELECT COUNT(*) FROM SPO365List WHERE SiteId = '{connectedSite.SiteId}' AND ParentWebUrl = '{connectedSite.ServerRelativeUrl}' AND BaseTemplate != '101'"
            );
            connectedSite.LibraryCount = _utility.GetDataFromSqlLite<int>(
                $"SELECT COUNT(*) FROM SPO365List WHERE SiteId = '{connectedSite.SiteId}' AND ParentWebUrl = '{connectedSite.ServerRelativeUrl}' AND BaseTemplate = '101'"
            );

            connectedSite.Lists = GetLists(connectedSite.SiteId, connectedSite.ServerRelativeUrl, isLibrary: false);
            connectedSite.Libraries = GetLibraries(connectedSite.SiteId, connectedSite.ServerRelativeUrl);

            connectedSite.Subsites = GetSubsites(connectedSite.SiteId, connectedSite.webId);

            return connectedSite;
        }

        private List<SPList> GetLists(string siteId, string serverRelativeUrl, bool isLibrary)
        {
            var templateCondition = isLibrary ? "= '101'" : "!= '101'";
            string sql = $"SELECT ListTitle, ItemCount, TotalSize FROM SPO365List WHERE SiteId = '{siteId}' AND ParentWebUrl = '{serverRelativeUrl}' AND BaseTemplate {templateCondition}";

            var result = new List<SPList>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SPList
                {
                    Name = reader["ListTitle"].ToString(),
                    ItemsCount = Convert.ToInt32(reader["ItemCount"]),
                    Size = reader["TotalSize"].ToString()
                });
            }

            return result;
        }

        private List<SPLibrary> GetLibraries(string siteId, string serverRelativeUrl)
        {
            string sql = $"SELECT ListTitle, ItemCount, TotalSize FROM SPO365List WHERE SiteId = '{siteId}' AND ParentWebUrl = '{serverRelativeUrl}' AND BaseTemplate = '101'";

            var result = new List<SPLibrary>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SPLibrary
                {
                    Name = reader["ListTitle"].ToString(),
                    FilesCount = Convert.ToInt32(reader["ItemCount"]),
                    Size = reader["TotalSize"].ToString()
                });
            }

            return result;
        }


        private List<Subsite> GetSubsites(string parentSiteId, string parentWebId)
        {
            string sql = $"SELECT Title, Url, WebsCount, serverRelativeUrl FROM SPO365Web WHERE ParentWebId = '{parentWebId}'";
            var subsites = new List<Subsite>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string subsiteUrl = reader["Url"].ToString();
                string subServerRelUrl = reader["serverRelativeUrl"].ToString();
                string subSiteId = _utility.GetDataFromSqlLite<string>($"SELECT SiteId FROM SPO365Web WHERE Url = '{subsiteUrl}'");
                string subWebId = _utility.GetDataFromSqlLite<string>($"SELECT webId FROM SPO365Web WHERE Url = '{subsiteUrl}'");

                var subsite = new Subsite
                {
                    webId = subSiteId,
                    SubsiteName = reader["Title"].ToString(),
                    SubsiteSiteUrl = subsiteUrl,
                    SubSitesCount = Convert.ToInt32(reader["WebsCount"]),
                    ServerRelativeUrl = subServerRelUrl,
                    ListsCount = _utility.GetDataFromSqlLite<int>($"SELECT COUNT(*) FROM SPO365List WHERE SiteId = '{subSiteId}' AND ParentWebUrl = '{subServerRelUrl}' AND BaseTemplate != '101'"),
                    LibraryCount = _utility.GetDataFromSqlLite<int>($"SELECT COUNT(*) FROM SPO365List WHERE SiteId = '{subSiteId}' AND ParentWebUrl = '{subServerRelUrl}' AND BaseTemplate = '101'"),
                    Lists = GetLists(subSiteId, subServerRelUrl, isLibrary: false),
                    Libraries = GetLibraries(subSiteId, subServerRelUrl),
                    Subsites = GetSubsites(subSiteId, subWebId)
                };

                subsites.Add(subsite);
            }

            return subsites;
        }

    }
}
