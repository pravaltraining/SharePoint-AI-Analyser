using System;
using SQLitePCL; // Required for Batteries.Init()

namespace SharePointAnalyserDemo
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Batteries.Init();

            var obj = new Analyser(@"C:\Users\sanju.c\AppData\Roaming\Saketa\Governance\GovernanceHistory.prn");

            var data = obj.GetConnectedSiteInfo();

            var response = await obj.Analyse(data);

            Console.WriteLine(response);
        }
    }
}
