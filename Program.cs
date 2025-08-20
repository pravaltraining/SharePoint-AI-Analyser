using SharePointAnalyserDemo.Analyzer;
using SQLitePCL;

namespace SharePointAnalyserDemo
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Batteries.Init();

            //var obj = new Goverence();
            //var data = obj.GetData();
            //var response = await obj.Analyse(data);
            //Console.WriteLine(response);

            var obj = new Inventory();
            //Tenant
            //var data = obj.GetData("1540");
            
            //2 subsites
            //var data = obj.GetData("1544");

            //2 Lists 1 Library
            //var data = obj.GetData("1546");

            //1 Folder
            //var data = obj.GetData("1549");

            //Nested Folders with Files
            //var data = obj.GetData("1554");

            //List Items
            //var data = obj.GetData("1555");

            //Deep Nested Folders
            var data = obj.GetData("1562");

            var response = await obj.Analyse(data);

            Console.WriteLine(response);


        }
    }
}
