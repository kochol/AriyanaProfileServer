using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Data;

namespace Server
{
    public class Program
    {

        public static IHostApplicationLifetime ApplicationLifetime = null;
        public static string ServerIP;

        static void QforExit()
        {
        WaitForQ:
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Q)
            {
                Console.WriteLine("Quit");
                ApplicationLifetime.StopApplication();
                return;
            }
            goto WaitForQ;
        }

        public static string GetPublicIP()
        {
            String direction = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                direction = stream.ReadToEnd();
            }

            //Search for the ip in the html
            int first = direction.IndexOf("Address: ") + 9;
            int last = direction.LastIndexOf("</body>");
            direction = direction.Substring(first, last - first);

            return direction;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Exit the program with q");
            Thread thread = new Thread(new ThreadStart(QforExit));
            thread.Start();

            ServerIP = GetPublicIP();

            Task.Run(async () => await LobbyManager.MakeMatches());

            CreateHostBuilder(args).Build().Run();

            LobbyManager.OnExit();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
