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
        public class WS : WebSocketConnection
        {
            public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
            {
                Console.WriteLine(message);
                return SendText(message, true);
            }
        }
        static void Main2(string[] args)
        {
            WebApp.Start(new StartOptions("http://localhost:8989"), startup =>
            {
                startup.MapWebSocketPattern<WS>("/captures/(?<capture1>.+)/(?<capture2>.+)");
            });

            var client = new ClientWebSocket();
            client.ConnectAsync(new Uri("ws://localhost:8989/captures/fred/james"), CancellationToken.None).Wait();
            client.SendAsync(new ArraySegment<byte>(new UTF8Encoding(true).GetBytes("foo")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            Console.ReadLine();
        }

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