using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Services.Interfaces;
using Microsoft.Web.Administration;
using System.IO;

namespace Containerizer.Services.Implementations
{
    public class CreateContainerService : ICreateContainerService
    {
        public Task<string> CreateContainer()
        {
            try
            {
                var id = Guid.NewGuid().ToString();
                var serverManager = new ServerManager();
                var rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                var path = Path.Combine(rootDir, "containerizer", id);
                var site = serverManager.Sites.Add(id, path, 0);

                serverManager.ApplicationPools.Add(id);
                site.Applications[0].ApplicationPoolName = id;
                var appPool = serverManager.ApplicationPools[id];
                appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;

                return Task.Factory.StartNew(() =>
                {
                    serverManager.CommitChanges();
                    Directory.CreateDirectory(path);
                    return id;
                });
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                if (ex.Message.Contains("2B72133B-3F5B-4602-8952-803546CE3344"))
                {
                    throw new CouldNotCreateContainerException("Please install IIS.", ex);
                }
                else
                {
                    throw new CouldNotCreateContainerException("", ex);
                }
            }
            catch (Exception ex)
            {
                throw new CouldNotCreateContainerException(String.Empty, ex);
            }
        }
    }
}
