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
        static void Main(string[] args)
        {
            // Start OWIN host 
            try
            {
                using (WebApp.Start<Startup>("http://*:" + args[0] + "/"))
                {
                    Console.WriteLine("SUCCESS: started");

                    // Create HttpCient and make a request to api/values 
                    HttpClient client = new HttpClient();

                    var response = client.GetAsync("http://localhost:" + args[0] + "/api/ping").Result;

                    Console.WriteLine(response);
                    Console.WriteLine(response.Content.ReadAsStringAsync().Result);

                    Console.WriteLine("Hit a key");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERRROR: " + ex.Message);
            }
        }
    }
}