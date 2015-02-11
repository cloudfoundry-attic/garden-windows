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
using Containerizer.Factories;

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

        public static void SetupSiteInIIS(string applicationFolderName, string siteName, string applicationPoolName,
            int port, bool privleged)
        {
            try
            {
                var serverManager = ServerManager.OpenRemote("localhost");
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationFolderName);

                RemoveExistingSite(siteName, applicationPoolName);

                Site mySite = serverManager.Sites.Add(siteName, path, port);
                mySite.ServerAutoStart = true;

                serverManager.ApplicationPools.Add(applicationPoolName);
                mySite.Applications[0].ApplicationPoolName = applicationPoolName;
                ApplicationPool apppool = serverManager.ApplicationPools[applicationPoolName];
                if (privleged)
                {
                    apppool.ProcessModel.IdentityType = ProcessModelIdentityType.LocalSystem;
                }
                apppool.ManagedRuntimeVersion = "v4.0";
                apppool.ManagedPipelineMode = ManagedPipelineMode.Integrated;

                serverManager.CommitChanges();
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                {
                    throw new Exception("Please install IIS.", ex);
                }
                throw;
            }
        }

        public static void DestroyContainer(HttpClient client, string handle)
        {
            var response = client.DeleteAsync("/api/Containers/" + handle).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        public static void RemoveExistingSite(string siteName, string applicationPoolName)
        {
            try
            {
                var serverManager = ServerManager.OpenRemote("localhost");
                var existingSite = serverManager.Sites.FirstOrDefault(x => x.Name == siteName);
                if (existingSite != null)
                {
                    serverManager.Sites.Remove(existingSite);
                    serverManager.CommitChanges();
                }

                var existingAppPool = serverManager.ApplicationPools.FirstOrDefault(x => x.Name == applicationPoolName);
                if (existingAppPool != null)
                {
                    serverManager.ApplicationPools.Remove(existingAppPool);
                    serverManager.CommitChanges();
                }
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                {
                    throw new Exception("Please install IIS.", ex);
                }
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("Try running Visual Studio/test runner as Administrator instead.", ex);
            }
        }


        public static DataReceivedEventArgs CreateMockDataReceivedEventArgs(string TestData)
        {
            var MockEventArgs =
                (DataReceivedEventArgs) FormatterServices
                    .GetUninitializedObject(typeof (DataReceivedEventArgs));

            FieldInfo[] EventFields = typeof (DataReceivedEventArgs)
                .GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

            if (EventFields.Any())
            {
                EventFields[0].SetValue(MockEventArgs, TestData);
            }
            else
            {
                throw new ApplicationException(
                    "Failed to find _data field!");
            }

            return MockEventArgs;
        }

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
    }
}