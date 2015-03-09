using Microsoft.Owin.Hosting;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Owin.WebSocket;
using Owin.WebSocket.Extensions;
using System.Threading.Tasks;

namespace Containerizer
{
    public class Program
    {
        private static readonly ManualResetEvent ExitLatch = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            var port = (args.Length == 1 ? args[0] : "80");
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                ExitLatch.Set();
            };

            // Start OWIN host
            using (WebApp.Start<Startup>("http://*:" + port + "/"))
            {
                Console.WriteLine("SUCCESS: started");

                // Create HttpCient and make a request to api/values 
                HttpClient client = new HttpClient();

                var response = client.GetAsync("http://localhost:" + port + "/api/ping").Result;

                Console.WriteLine(response);
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);

                Console.WriteLine("Control-C to quit.");
                ExitLatch.WaitOne();
            }
        }
    }
}