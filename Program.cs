using Newtonsoft.Json;
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
            //var data = obj.GetData("1540");
            //var data = obj.GetData("1544");
            //var data = obj.GetData("1546");
            //var data = obj.GetData("1549");
            //var data = obj.GetData("1554");
            var data = obj.GetData("1555");
            //var data = obj.GetData("1562");
            var response = await obj.Analyse(data);

            Console.WriteLine(response);


        }
    }
}
