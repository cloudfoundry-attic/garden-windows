using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Containerizer.Services.Interfaces;

namespace Containerizer.Services.Implementations
{
    public class ContainerPathService : IContainerPathService
    {
        public string GetContainerRoot(string id)
        {
            var rootDir = Directory.GetDirectoryRoot(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            return Path.Combine(rootDir, "containerizer", id);
        }


        public void CreateContainerDirectory(string id)
        {
            Directory.CreateDirectory(this.GetContainerRoot(id));
        }
    }
}