#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Web.Administration;
using Newtonsoft.Json.Linq;
using IronFrame;
using Containerizer.Services.Implementations;
using System.Text;
using System.Net.Sockets;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Net.Http.Headers;
using Containerizer.Factories;
using Containerizer.Tests.Spec;
using NSpec;
using System.Security.Cryptography;

#endregion

namespace Containerizer.Tests.Specs
{
    public static class Controller
    {
        public const string Index = "Index";
        public const string Show = "Show";
        public const string Create = "NetIn";
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
            var postTask = client.PostAsync("/api/Containers", new StringContent("{Handle: \"" + handle + "\", Env: []}", Encoding.UTF8, "application/json"));
            postTask.Wait();
            var postResult = postTask.Result;
            var readTask = postResult.Content.ReadAsStringAsync();
            readTask.Wait();
            var response = readTask.Result;
            var json = JObject.Parse(response);
            return json["handle"].ToString();
        }

        public static string CreateContainerWithGUID(HttpClient client)
        {
            var handle = Guid.NewGuid().ToString();
            var postTask = client.PostAsync("/api/Containers", new StringContent("{Handle: \"" + handle + "\", Env: [\"INSTANCE_GUID=ExcitingGuid\"]}", Encoding.UTF8, "application/json"));
            postTask.Wait();
            var postResult = postTask.Result;
            var readTask = postResult.Content.ReadAsStringAsync();
            readTask.Wait();
            var response = readTask.Result;
            var json = JObject.Parse(response);
            return json["handle"].ToString();
        }

        static string GenerateContainerId(string handle)
        {
            var sha = new SHA1Managed();
            var handleBytes = Encoding.UTF8.GetBytes(handle);
            var hashBytes = sha.ComputeHash(handleBytes);
            return BitConverter.ToString(hashBytes, 0, 9).Replace("-", "");
        }

        public static string GetContainerPath(string handle)
        {
            return Path.Combine(ContainerServiceFactory.GetContainerRoot(), GenerateContainerId(handle), "user");
        }

        public class ContainerizerProcess : IDisposable
        {
            private Job job;
            public readonly string ExternalIP;
            public readonly int Port;

            public ContainerizerProcess(int port)
            {
                this.job = new Job();
                this.ExternalIP = "10.1.2." + new Random().Next(2, 253).ToString();
                this.Port = port;
            }

            public void Start()
            {
                AssertAdministratorPrivileges();

                var process = new System.Diagnostics.Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = Path.Combine(Helpers.AssemblyDirectory, "..", "..", "..", "Containerizer", "bin", "Containerizer.exe");
                process.StartInfo.Arguments = ExternalIP + " " + Port.ToString();
                Retry.Do(() => process.Start(), TimeSpan.FromSeconds(1), 5);
                job.AddProcess(process.Handle);
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
                job.Dispose();
            }
        }

        public static ContainerizerProcess CreateContainerizerProcess()
        {
            var port = 48080;
            var process = new ContainerizerProcess(port);
            process.Start();
            return process;
        }

        public static void DestroyContainer(HttpClient client, string handle)
        {
            var response = client.DeleteAsync("/api/Containers/" + handle).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        public static void AssertAdministratorPrivileges()
        {
            //http://stackoverflow.com/questions/1089046/in-net-c-test-if-process-has-administrative-privileges

            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                var user = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(user);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                isAdmin = false;
            }
            if (!isAdmin) { throw new Exception("You will need to run the tests with Administrator Privileges"); }
        }

        public static class Retry
        {
            public static void Do(
                Action action,
                TimeSpan retryInterval,
                int retryCount = 3)
            {
                Do<object>(() =>
                {
                    action();
                    return null;
                }, retryInterval, retryCount);
            }

            public static T Do<T>(
                Func<T> action,
                TimeSpan retryInterval,
                int retryCount = 3)
            {
                var exceptions = new List<Exception>();

                for (int retry = 0; retry < retryCount; retry++)
                {
                    try
                    {
                        return action();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        Thread.Sleep(retryInterval);
                    }
                }

                throw new AggregateException(exceptions);
            }
        }
    }
}