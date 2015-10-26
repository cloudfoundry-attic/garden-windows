#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        public static string GetContainerPath(string containerRootDirectory, string handle)
        {
            return Path.Combine(containerRootDirectory, GenerateContainerId(handle), "user");
        }
        public static string CreateTarFile()
        {
            var parentDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            var contentDirectory = parentDir.CreateSubdirectory("content");
            File.WriteAllText(Path.Combine(contentDirectory.FullName, "file.txt"), "stuff!!!!");
            var tarFile = Path.Combine(parentDir.FullName, Guid.NewGuid().ToString() + ".tgz");
            new TarStreamService().CreateTarFromDirectory(contentDirectory.FullName, tarFile);
            return tarFile;
        }
        public static HttpResponseMessage StreamIn(string tgzPath, string handle, HttpClient client)
        {
            HttpResponseMessage responseMessage;
            var content = new MultipartFormDataContent();
            var fileStream = new FileStream(tgzPath, FileMode.Open);
            using (var streamContent = new StreamContent(fileStream))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent);
                string path = "/api/containers/" + handle + "/files?destination=%2F";
                responseMessage = client.PutAsync(path, streamContent).GetAwaiter().GetResult();
            }
            return responseMessage;
        }

        public class ContainerizerProcess : IDisposable
        {
            private Job job;
            public readonly string ExternalIP;
            public readonly int Port;
            public readonly string ContainerDirectory;
            
            public ContainerizerProcess(int port, string containerDirectory)
            {
                this.job = new Job();
                this.ExternalIP = "10.1.2." + new Random().Next(2, 253).ToString();
                this.Port = port;
                this.ContainerDirectory = containerDirectory;
            }

            public void Start()
            {
                AssertAdministratorPrivileges();

                var process = new System.Diagnostics.Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = Path.Combine(Helpers.AssemblyDirectory, "..", "..", "..", "Containerizer", "bin", "Containerizer.exe");
                process.StartInfo.Arguments = " --externalIP " + ExternalIP + " --port " + Port.ToString() + " --containerDirectory " + ContainerDirectory;
                Retry.Do(() => process.Start(), TimeSpan.FromSeconds(1), 5);
                job.AddProcess(process.Handle);
                process.StandardOutput.ReadLine().should_contain("containerizer.started");
                process.StandardOutput.ReadToEndAsync();
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
            return CreateContainerizerProcess(Factories.ContainerServiceFactory.GetContainerDefaultRoot());
        }

        public static ContainerizerProcess CreateContainerizerProcess(string containerDirectory)
        {
            while (true)
            {
                var containzerizerProcesses = Process.GetProcessesByName("Containerizer");
                if (containzerizerProcesses.Length == 0)
                {
                    break;
                }

                foreach (Process p in containzerizerProcesses)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(); // possibly with a timeout
                    }
                    catch (Win32Exception winException)
                    {
                        // process was terminating or can't be terminated - deal with it
                    }
                    catch (InvalidOperationException invalidException)
                    {
                        // process has already exited - might be able to let this one go
                    }
                }
            }
            var port = 48080;
            var process = new ContainerizerProcess(port, containerDirectory);
            process.Start();
            return process;
        }

        public static void DestroyContainer(HttpClient client, string handle)
        {
            var response = client.DeleteAsync("/api/Containers/" + handle);
            if (response.Wait(5000))
            {
                response.Result.EnsureSuccessStatusCode();
            }
            else
            {
                throw new TimeoutException("timed out destroying container");
            }
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