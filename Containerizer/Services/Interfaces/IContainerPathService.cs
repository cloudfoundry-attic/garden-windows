using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IContainerPathService
    {
        string GetContainerRoot(string id);
        void CreateContainerDirectory(string id);
    }
}
