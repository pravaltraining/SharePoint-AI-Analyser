using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using SharePointAnalyserDemo.Entities.Inventory;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharePointAnalyserDemo.Analyzer
{
    public class Inventory 
    {
        private readonly Utility _utility;
        private static readonly HttpClient client = new HttpClient();
        private readonly string ApiKey;
        public Inventory()
        {
            _utility = new Utility(Configuration.InventoryConnectionString);
            ApiKey = Configuration.ApiKey;
        }

        public async Task<string> Analyse(System.Object data)
        {
            string siteDataJson = JsonConvert.SerializeObject(data, Formatting.Indented);

            string prompt = $@"
                            The following JSON contains inventory data from one or more SharePoint objects, including:

                            - Site collections, subsites, lists, libraries
                            - Files and folders with version and size details
                            - Metadata such as item count, size in bytes, check-out status, unique permissions, and more

                            Based on this data, list the top 4 SharePoint best practices that should be implemented or followed. 
                            Focus only on the most impactful practices that improve performance, governance, or storage efficiency.

                             Format your response as follows:
                            issues:
                            - List the top 4 most significant best practices, starting with the most critical.

                            Note: Convert sizes to MB/GB for clarity where needed.

                            Data:
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

        public object GetData(string reportId)
        {
            int analysisOnValue = _utility.GetDataFromSqlLite<int>($"SELECT Type FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");

            InventorySPObject inventorySPObject = _utility.GetEnumValue<InventorySPObject>(analysisOnValue);
            int isMultipleSelected = _utility.GetDataFromSqlLite<int>($"SELECT COUNT(*) FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");

            switch(inventorySPObject) 
            {
                case InventorySPObject.SiteCollection:
                case InventorySPObject.Web:
                    if( isMultipleSelected > 1 )
                    {
                        var obj = new SPInventory<TenantInventory>();
                        obj.AnalysisOn = "Mutiple Sites";
                        obj.Data = GetMultipleSitesData(reportId);
                        return obj;
                    }
                    else
                    {
                        var obj = new SPInventory<SiteInventory>();
                        obj.AnalysisOn = "Site";
                        obj.Data = GetSiteData(reportId);
                        return obj;
                    }
                    break;
                case InventorySPObject.List:
                case InventorySPObject.Library:
                    if (isMultipleSelected > 1)
                    {
                        List<SIList> allLists = new List<SIList>();
                        List<string> selectedUrls = new List<string>();
                        var sql = $"SELECT FullPath from SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'";
                        using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
                        connection.Open();
                        using var command = new SqliteCommand(sql, connection);
                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            selectedUrls.Add(reader["FullPath"].ToString());
                        }

                        foreach( var url in selectedUrls )
                        {
                            var isLibrary = inventorySPObject == InventorySPObject.List ? false : true;
                            allLists.Add(GetListData(reportId, url, isLibrary));
                        }

                        var obj = new SPInventory<List<SIList>>();
                        obj.AnalysisOn = "Lists / Libraries";
                        obj.Data = allLists;
                        return obj;
                    }
                    else 
                    {
                        var siteUrl = _utility.GetDataFromSqlLite<string>($"SELECT FullPath from SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");
                        var isLibrary = inventorySPObject == InventorySPObject.List ? false : true;

                        var obj = new SPInventory<SIList>();
                        obj.AnalysisOn = "List / Library";
                        obj.Data = GetListData(reportId, siteUrl, isLibrary);
                        return obj;
                    }
                    break;
                case InventorySPObject.Folder:
                    if( isMultipleSelected > 1)
                    {

                        List<string> folderUrls = new List<string>();
                        List<string> fileUrls = new List<string>();

                        var sql = $"SELECT FullPath, Type FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'";

                        using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
                        connection.Open();
                        using var command = new SqliteCommand(sql, connection);
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            var type = reader["Type"].ToString();
                            var fullPath = reader["FullPath"].ToString();

                            if (type == "11")  
                            {
                                folderUrls.Add(fullPath);
                            }
                            else
                            {
                                fileUrls.Add(fullPath);
                            }
                        }

                        SIFolder folder = new SIFolder();
                        foreach (var url in folderUrls)
                        {
                            folder.Folders.Add(GetFolderData(reportId, url));
                        }

                        foreach (var url in fileUrls)
                        {
                            folder.Items.Add(GetItemData(reportId, url));
                        }


                        var obj = new SPInventory<SIFolder>();
                        obj.AnalysisOn = "Folder";
                        obj.Data = folder;
                        return obj;

                    }
                    else
                    {
                        var data = GetFolderData(reportId);
                        var obj = new SPInventory<SIFolder>();
                        obj.AnalysisOn = "Folder";
                        obj.Data = data;
                        return obj;
                    }
                    break;
                case InventorySPObject.File:
                case InventorySPObject.ListItem:
                    if( isMultipleSelected > 1)
                    {
                        List<string> itemUrls = new();

                        string sql = $"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'";

                        using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
                        connection.Open();
                        using var command = new SqliteCommand(sql, connection);
                        using var reader = command.ExecuteReader();

                        while(reader.Read())
                        {
                            itemUrls.Add(reader["FullPath"].ToString());
                        }

                        List<SPItem> allitems = new List<SPItem>();

                        foreach( var item in itemUrls )
                        {
                            allitems.Add(GetItemData(reportId, item));
                        }

                        var obj = new SPInventory<List<SPItem>>();
                        if (inventorySPObject == InventorySPObject.ListItem)
                        {
                            obj.AnalysisOn = "Multiple List Items";
                        }
                        else
                        {
                            obj.AnalysisOn = "Multiple Library Items";
                        }
                        obj.Data = allitems;

                        return obj;
                    }
                    else
                    {
                        var data = GetItemData(reportId);
                        var obj = new SPInventory<SPItem>();
                        if (inventorySPObject == InventorySPObject.ListItem)
                        {
                            obj.AnalysisOn = "List Items";
                        }
                        else
                        {
                            obj.AnalysisOn = "Library Items";
                        }
                        obj.Data = data;
                        return obj;
                    }
            }
            return null!;
        }

        public TenantInventory GetMultipleSitesData(string reportId)
        {
            var data = new TenantInventory();

            data.SitesCount = _utility.GetDataFromSqlLite<int>($"SELECT COUNT(*) FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");

            var sitesList = new List<(string, string)>();
            string sql = $"SELECT SiteName, SiteUrl FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'";

            using (var connection = new SqliteConnection(Configuration.InventoryConnectionString))
            {
                connection.Open();
                using var command = new SqliteCommand(sql, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string siteName = reader.GetString(0);
                    string siteUrl = reader.GetString(1);
                    sitesList.Add((siteName, siteUrl));
                }
            }

            foreach( var site in sitesList )
            {
                data.sites.Add(GetSiteData(reportId, site.Item1, site.Item2));
            }

            return data;
        }

        //public SiteInventory GetSiteData(string reportId, string siteName = "", string siteUrl = "")
        //{
        //    var data = new SiteInventory();

        //    if (!string.IsNullOrEmpty(siteName) && !string.IsNullOrEmpty(siteUrl))
        //    {
        //        data.siteUrl = siteUrl;
        //        data.siteName = siteName;
        //    }
        //    else
        //    {
        //        data.siteName = _utility.GetDataFromSqlLite<string>($"SELECT SiteName FROM SPInventoryItem WHERE SessionId = {reportId} AND IsSelectedResouce = '1'");
        //        data.siteUrl = _utility.GetDataFromSqlLite<string>($"SELECT SiteUrl FROM SPInventoryItem WHERE SessionId = {reportId} AND IsSelectedResouce = '1'");
        //    }

        //    data.subsitesCount = _utility.GetDataFromSqlLite<int>($"SELECT DirectSubsitesCount FROM SPInventoryItem WHERE SessionId = {reportId} AND SiteUrl = '{data.siteUrl}' AND type IN (3,4)");
        //    data.Lists = GetListsDataFromSite(reportId, data.siteUrl, false);
        //    data.Libraries = GetListsDataFromSite(reportId, data.siteUrl, true);
        //    data.listsCount = data.Lists.Count;
        //    data.librariesCount = data.Libraries.Count;

        //    string sql = $"SELECT SiteName, SiteUrl, DirectSubsitesCount FROM SPInventoryItem WHERE SessionId = {reportId} AND Type = '4' AND SiteUrl LIKE '{data.siteUrl}/%' AND SiteUrl NOT LIKE '{data.siteUrl}/%/%'";
        //    var sitesList = new List<(string, string)>();

        //    using (var connection = new SqliteConnection(Configuration.InventoryConnectionString))
        //    {
        //        connection.Open();
        //        using var command = new SqliteCommand(sql, connection);
        //        using var reader = command.ExecuteReader();

        //        while (reader.Read())
        //        {
        //            string Name = reader.GetString(0);
        //            string Url = reader.GetString(1);
        //            sitesList.Add((Name, Url));
        //        }
        //    }

        //    for( int i = 0; i < sitesList.Count; i++ )
        //    {
        //        data.SubSites.Add(GetSiteData(reportId, sitesList[i].Item1, sitesList[i].Item2));
        //    }

        //    return data;
        //}

        public SiteInventory GetSiteData(string reportId, string siteName = "", string siteUrl = "")
        {
            var data = new SiteInventory();

            if (!string.IsNullOrEmpty(siteName) && !string.IsNullOrEmpty(siteUrl))
            {
                data.siteUrl = siteUrl;
                data.siteName = siteName;
            }
            else
            {
                data.siteName = _utility.GetDataFromSqlLite<string>("SELECT SiteName FROM SPInventoryItem WHERE SessionId = @reportId AND IsSelectedResouce = '1'",new Dictionary<string, object> { ["@reportId"] = reportId });

                data.siteUrl = _utility.GetDataFromSqlLite<string>("SELECT SiteUrl FROM SPInventoryItem WHERE SessionId = @reportId AND IsSelectedResouce = '1'",new Dictionary<string, object> { ["@reportId"] = reportId });
            }

            data.subsitesCount = _utility.GetDataFromSqlLite<int>("SELECT DirectSubsitesCount FROM SPInventoryItem WHERE SessionId = @reportId AND SiteUrl = @siteUrl AND type IN (3,4)",new Dictionary<string, object> { ["@reportId"] = reportId, ["@siteUrl"] = data.siteUrl });

            data.Lists = GetListsDataFromSite(reportId, data.siteUrl, false);
            data.Libraries = GetListsDataFromSite(reportId, data.siteUrl, true);
            data.listsCount = data.Lists.Count;
            data.librariesCount = data.Libraries.Count;

            string sql = @"SELECT SiteName, SiteUrl, DirectSubsitesCount FROM SPInventoryItem WHERE SessionId = @reportId AND Type = '4' AND SiteUrl LIKE @likePattern1 AND SiteUrl NOT LIKE @likePattern2";

            var sitesList = new List<(string, string)>();

            using (var connection = new SqliteConnection(Configuration.InventoryConnectionString))
            {
                connection.Open();
                using var command = new SqliteCommand(sql, connection);

                command.Parameters.AddWithValue("@reportId", reportId);
                command.Parameters.AddWithValue("@likePattern1", $"{data.siteUrl}/%");
                command.Parameters.AddWithValue("@likePattern2", $"{data.siteUrl}/%/%");

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string Name = reader.GetString(0);
                    string Url = reader.GetString(1);
                    sitesList.Add((Name, Url));
                }
            }

            for (int i = 0; i < sitesList.Count; i++)
            {
                data.SubSites.Add(GetSiteData(reportId, sitesList[i].Item1, sitesList[i].Item2));
            }

            return data;
        }


        //private List<SIList> GetListsDataFromSite(string reportId, string siteUrl, bool isLibrary)
        //{
        //    var type = isLibrary ? '7' : '6';
        //    var list = new List<SIList>();

        //    var sql = $"SELECT Name, ItemsCount, Size, HasUniquePermissions, NoOfCheckOutFiles, TagsContent, LastModified FROM SPInventoryItem WHERE SessionId = {reportId} AND Type = {type} AND ((FullPath Like '{siteUrl}/%'  AND FullPath NOT Like '{siteUrl}/%/%')OR (FullPath Like '{siteUrl}/Lists/%' AND FullPath NOT Like '{siteUrl}/Lists/%/%') )";
        //    using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
        //    connection.Open();
        //    using var command = new SqliteCommand(sql, connection);
        //    using var reader = command.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        list.Add(new SIList
        //        {
        //            Name = reader["Name"].ToString(),
        //            ItemsCount = Convert.ToInt32(reader["ItemsCount"].ToString()),
        //            Size = reader["Size"].ToString(),
        //            ItemHasUniquePermissions = Convert.ToInt32(reader["HasUniquePermissions"]) == 1,
        //            NoofCheckOutFiles = Convert.ToInt32(reader["NoOfCheckOutFiles"]),
        //            OtherInfo = reader["TagsContent"].ToString(),
        //            LastModifiedDate = reader["LastModified"].ToString()
        //        });
        //    }

        //    return list;
        //}

        private List<SIList> GetListsDataFromSite(string reportId, string siteUrl, bool isLibrary)
        {
            var type = isLibrary ? '7' : '6';
            var list = new List<SIList>();

            var sql = @" SELECT Name, ItemsCount, Size, HasUniquePermissions, NoOfCheckOutFiles, TagsContent, LastModified FROM SPInventoryItem WHERE SessionId = @reportId AND Type = @type AND ((FullPath LIKE @siteUrlPattern1 AND FullPath NOT LIKE @siteUrlPattern2) OR (FullPath LIKE @siteUrlPattern3 AND FullPath NOT LIKE @siteUrlPattern4))";

            using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);

            command.Parameters.AddWithValue("@reportId", reportId);
            command.Parameters.AddWithValue("@type", type);
            command.Parameters.AddWithValue("@siteUrlPattern1", $"{siteUrl}/%");
            command.Parameters.AddWithValue("@siteUrlPattern2", $"{siteUrl}/%/%");
            command.Parameters.AddWithValue("@siteUrlPattern3", $"{siteUrl}/Lists/%");
            command.Parameters.AddWithValue("@siteUrlPattern4", $"{siteUrl}/Lists/%/%");

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new SIList
                {
                    Name = reader["Name"].ToString(),
                    ItemsCount = Convert.ToInt32(reader["ItemsCount"]),
                    Size = reader["Size"].ToString(),
                    ItemHasUniquePermissions = Convert.ToInt32(reader["HasUniquePermissions"]) == 1,
                    NoofCheckOutFiles = Convert.ToInt32(reader["NoOfCheckOutFiles"]),
                    OtherInfo = reader["TagsContent"].ToString(),
                    LastModifiedDate = reader["LastModified"].ToString()
                });
            }

            return list;
        }


        //public SIList GetListData(string reportId, string listUrl, bool isLibrary)
        //{
        //    var list = new SIList();
        //    var sql = $"SELECT Name, ItemsCount, Size, HasUniquePermissions, NoOfCheckOutFiles, TagsContent, LastModified FROM SPInventoryItem WHERE SessionId = {reportId} AND FullPath = '{listUrl}'";
        //    using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
        //    connection.Open();
        //    using var command = new SqliteCommand(sql, connection);
        //    using var reader = command.ExecuteReader();

        //    while (reader.Read())
        //    {
        //        list.Name = reader["Name"].ToString();
        //        list.ItemsCount = Convert.ToInt32(reader["ItemsCount"].ToString());
        //        list.Size = reader["Size"].ToString();
        //        list.ItemHasUniquePermissions = Convert.ToInt32(reader["HasUniquePermissions"]) == 1;
        //        list.NoofCheckOutFiles = Convert.ToInt32(reader["NoOfCheckOutFiles"]);
        //        list.OtherInfo = reader["TagsContent"].ToString();
        //        list.LastModifiedDate = reader["LastModified"].ToString();
        //    }

        //    return list;
        //}

        public SIList GetListData(string reportId, string listUrl, bool isLibrary)
        {
            var list = new SIList();
            var sql = @" SELECT Name, ItemsCount, Size, HasUniquePermissions, NoOfCheckOutFiles, TagsContent, LastModified  FROM SPInventoryItem  WHERE SessionId = @reportId AND FullPath = @listUrl";

            using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@reportId", reportId);
            command.Parameters.AddWithValue("@listUrl", listUrl);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                list.Name = reader["Name"].ToString();
                list.ItemsCount = Convert.ToInt32(reader["ItemsCount"]);
                list.Size = reader["Size"].ToString();
                list.ItemHasUniquePermissions = Convert.ToInt32(reader["HasUniquePermissions"]) == 1;
                list.NoofCheckOutFiles = Convert.ToInt32(reader["NoOfCheckOutFiles"]);
                list.OtherInfo = reader["TagsContent"].ToString();
                list.LastModifiedDate = reader["LastModified"].ToString();
            }

            return list;
        }


        //public SPItem GetItemData( string reportId, string itemUrl = "" )
        //{

        //    var item = new SPItem();

        //    if( itemUrl == "" )
        //    {
        //        itemUrl = _utility.GetDataFromSqlLite<string>($"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");
        //    }

        //    var sql = $"SELECT Name, Size, LastModified, ContentType, NoOfVersions  FROM SPInventoryItem WHERE SessionId = '{reportId}' AND FullPath = '{itemUrl}'";

        //    using var connection = new SqliteConnection( Configuration.InventoryConnectionString);
        //    connection.Open();
        //    using var command = new SqliteCommand( sql, connection);
        //    using var reader = command.ExecuteReader();

        //    while (reader.Read())
        //    {
        //        item.Name = reader["Name"].ToString();
        //        item.Size = Convert.ToInt32(reader["Size"]);
        //        item.LastModified = reader["LastModified"].ToString();
        //        item.ContentType = reader["ContentType"].ToString();
        //        item.TotalVersions = Convert.ToInt32(reader["NoOfVersions"]);

        //        if(Convert.ToInt32(reader["NoOfVersions"]) > 0)
        //        {
        //            item.isVersionInfoIncluded = true;
        //        }
        //    }

        //    return item;
        //}

        public SPItem GetItemData(string reportId, string itemUrl = "")
        {
            var item = new SPItem();

            if (string.IsNullOrEmpty(itemUrl))
            {
                itemUrl = _utility.GetDataFromSqlLite<string>($"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");
            }

            var sql = "SELECT Name, Size, LastModified, ContentType, NoOfVersions FROM SPInventoryItem WHERE SessionId = @reportId AND FullPath = @itemUrl";

            using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@reportId", reportId);
            command.Parameters.AddWithValue("@itemUrl", itemUrl);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                item.Name = reader["Name"].ToString();
                item.Size = Convert.ToInt32(reader["Size"]);
                item.LastModified = reader["LastModified"].ToString();
                item.ContentType = reader["ContentType"].ToString();
                item.TotalVersions = Convert.ToInt32(reader["NoOfVersions"]);
                if (item.TotalVersions > 0)
                {
                    item.isVersionInfoIncluded = true;
                }
            }

            return item;
        }


        //public SIFolder GetFolderData( string reportId, string folderUrl = "" )
        //{
        //    var folder = new SIFolder();

        //    if( folderUrl == "" )
        //    {
        //        folderUrl = _utility.GetDataFromSqlLite<string>($"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND IsSelectedResouce = '1'");
        //    }


        //    var foldersSql = $"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND FullPath like '{folderUrl}/%' AND FullPath NOT like '{folderUrl}/%/%' AND Type = '11'";

        //    List<string> folderUrls = new List<string>();
        //    using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
        //    connection.Open();
        //    using var command = new SqliteCommand(foldersSql, connection);
        //    using var reader = command.ExecuteReader();

        //    while( reader.Read())
        //    {
        //        folderUrls.Add(reader["FullPath"].ToString());
        //    }

        //    foreach( var url in folderUrls )
        //    {
        //        folder.Folders.Add(GetFolderData(reportId, url));
        //    }


        //    var filesSql = $"SELECT FullPath FROM SPInventoryItem WHERE SessionId = '{reportId}' AND FullPath like '{folderUrl}/%' AND FullPath NOT like '{folderUrl}/%/%' AND Type != '11'";

        //    List<string> filesUrls = new List<string>();
        //    using var filesCommand = new SqliteCommand(filesSql, connection);
        //    using var filesReader = filesCommand.ExecuteReader();

        //    while( filesReader.Read())
        //    {
        //        filesUrls.Add(filesReader["FullPath"].ToString());
        //    }

        //    foreach( var file in filesUrls )
        //    {
        //        folder.Items.Add(GetItemData(reportId, file));
        //    }

        //    return folder;
        //}

        public SIFolder GetFolderData(string reportId, string folderUrl = "")
        {
            var folder = new SIFolder();

            if (string.IsNullOrEmpty(folderUrl))
            {
                folderUrl = _utility.GetDataFromSqlLite<string>("SELECT FullPath FROM SPInventoryItem WHERE SessionId = @reportId AND IsSelectedResouce = '1'", new Dictionary<string, object> { { "@reportId", reportId } });
            }

            using var connection = new SqliteConnection(Configuration.InventoryConnectionString);
            connection.Open();

            // Get subfolders
            var foldersSql = @"SELECT FullPath FROM SPInventoryItem WHERE SessionId = @reportId AND FullPath LIKE @folderPathPrefix AND FullPath NOT LIKE @folderPathNested AND Type = '11'";

            List<string> folderUrls = new List<string>();
            using (var command = new SqliteCommand(foldersSql, connection))
            {
                command.Parameters.AddWithValue("@reportId", reportId);
                command.Parameters.AddWithValue("@folderPathPrefix", folderUrl + "/%");
                command.Parameters.AddWithValue("@folderPathNested", folderUrl + "/%/%");

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    folderUrls.Add(reader["FullPath"].ToString());
                }
            }

            foreach (var url in folderUrls)
            {
                folder.Folders.Add(GetFolderData(reportId, url));
            }

            // Get files in folder
            var filesSql = @" SELECT FullPath FROM SPInventoryItem WHERE SessionId = @reportId AND FullPath LIKE @folderPathPrefix AND FullPath NOT LIKE @folderPathNested AND Type != '11'";

            List<string> filesUrls = new List<string>();
            using (var filesCommand = new SqliteCommand(filesSql, connection))
            {
                filesCommand.Parameters.AddWithValue("@reportId", reportId);
                filesCommand.Parameters.AddWithValue("@folderPathPrefix", folderUrl + "/%");
                filesCommand.Parameters.AddWithValue("@folderPathNested", folderUrl + "/%/%");

                using var filesReader = filesCommand.ExecuteReader();
                while (filesReader.Read())
                {
                    filesUrls.Add(filesReader["FullPath"].ToString());
                }
            }

            foreach (var file in filesUrls)
            {
                folder.Items.Add(GetItemData(reportId, file));
            }

            return folder;
        }
    }
}