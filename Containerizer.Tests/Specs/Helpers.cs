#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.Web.Administration;
using Newtonsoft.Json.Linq;
using IronFoundry.Container;
using Containerizer.Services.Implementations;
using System.Text;
using System.Net.Sockets;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Net.Http.Headers;
using Containerizer.Factories;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs
{
    public static class Controller
    {
        public const string Index = "Index";
        public const string Show = "Show";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Destroy = "Destroy";
    }

    public static class Helpers
    {
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <returns>The newly created container's id.</returns>
        public static string CreateContainer(HttpClient client)
        {
            var handle = Guid.NewGuid().ToString();
            var postTask = client.PostAsync("/api/Containers", new StringContent("{Handle: \"" + handle + "\"}", Encoding.UTF8, "application/json"));
            postTask.Wait();
            var postResult = postTask.Result;
            var readTask = postResult.Content.ReadAsStringAsync();
            readTask.Wait();
            var response = readTask.Result;
            var json = JObject.Parse(response);
            return json["id"].ToString();
        }

        public static string GetContainerPath(string handle)
        {
            return Path.Combine(ContainerServiceFactory.GetContainerRoot(), new ContainerHandleHelper().GenerateId(handle), "user");
        }

        public class ContainarizerProcess : IDisposable
        {
            private Process process;
            public readonly int Port;

            public ContainarizerProcess(int port)
            {
                this.Port = port;
            }

            public void Start()
            {
                process = new System.Diagnostics.Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = Path.Combine(Helpers.AssemblyDirectory, "..", "..", "..", "Containerizer", "bin",
                    "Containerizer.exe");
                process.StartInfo.Arguments = Port.ToString();
                process.Start();
                process.StandardOutput.ReadLine().should_start_with("SUCCESS");
            }

            public HttpClient GetClient()
            {
                var client = new HttpClient { BaseAddress = new Uri("http://localhost:" + Port) };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return client;
            }

            public void Dispose()
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        public static ContainarizerProcess CreateContainerizerProcess()
        {
            var port = new Random().Next(10000, 50000);
            var process = new ContainarizerProcess(port);
            process.Start();
            return process;
        }

        public static void DestroyContainer(HttpClient client, string handle)
        {
            var response = client.DeleteAsync("/api/Containers/" + handle).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        /*
        public static bool PortIsUsed(int port)
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    s.Connect("localhost", port);
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }

        public static void RemoveAllWardenUsers()
        {
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        var de = result.GetUnderlyingObject() as DirectoryEntry;
                        if (de.Name.StartsWith("c_"))
                        {
                            Console.WriteLine("DELETE: " + de.Name);
                            result.Delete();
                        }
                    }
                }
            }
        }
        */
    }
}