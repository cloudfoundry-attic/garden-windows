using Microsoft.Owin.Hosting;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Owin.WebSocket;
using Owin.WebSocket.Extensions;
using System.Threading.Tasks;
using Microsoft.Owin.Builder;
using System.Net;
using Containerizer.Services.Implementations;

namespace Containerizer
{
    public class Program
    {
        private static readonly ManualResetEvent ExitLatch = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            if (args.Length != 2) {
                Console.WriteLine("Usage: {0} [ExternalIP] [Port]", "Containerizer.exe");
                return;
            }

            var externalIP = args[0];
            var port = args[1];

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                ExitLatch.Set();
            };

            // Start OWIN host
            DependencyResolver.ExternalIP = new ExternalIP(externalIP);
            using (WebApp.Start<Startup>("http://*:" + port + "/"))
            {         
                Console.WriteLine("SUCCESS: started on {0} with externalIP {1}", port, externalIP);

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