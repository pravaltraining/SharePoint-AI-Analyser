using System.ComponentModel;

namespace SharePointAnalyserDemo
{
    public enum InventorySPObject
    {

        /// <summary>
        /// The none
        /// </summary>
        [Description("All")]
        All = 0,

        /// <summary>
        /// The web application
        /// </summary>
        [Description("Web Application")]
        WebApplication = 2,

        /// <summary>
        /// The site collection
        /// </summary>
        [Description("Site Collection")]
        SiteCollection = 3,

        /// <summary>
        /// The web
        /// </summary>
        [Description("Web")]
        Web = 4,

        /// <summary>
        /// The list
        /// </summary>

        [Description("List")]
        List = 6,
        /// <summary>
        /// The library
        /// </summary>

        [Description("Library")]
        Library = 7,
        /// <summary>
        /// The list item
        /// </summary>
        [Description("List Item")]
        ListItem = 9,

        /// <summary>
        /// The folder
        /// </summary>
        [Description("Folder")]
        Folder = 11,

        /// <summary>
        /// The file
        /// </summary>
        [Description("File")]
        File = 12,

        /// <summary>
        /// The content type
        /// </summary>
        [Description("Content Type")]
        ContentType = 13,

        /// <summary>
        /// The workflow
        /// </summary>
        [Description("Workflow")]
        Workflow = 21,

        /// <summary>
        /// The permission
        /// </summary>
        [Description("Permission Level")]
        Permission = 20,

        /// <summary>
        /// The info path form
        /// </summary>
        [Description("Info Path Form")]
        InfoPathForm = 77,

        /// <summary>
        /// The SharePoint Solution
        /// </summary>
        [Description("SharePoint Solution")]
        SharePointSolution = 119,

        /// <summary>
        /// The desinger  form
        /// </summary>
        [Description("Designer Form")]
        DesignerForm = 120
    }
}
