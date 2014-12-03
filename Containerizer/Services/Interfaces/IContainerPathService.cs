using System.Collections.Generic;

namespace Containerizer.Services.Interfaces
{
    public interface IContainerPathService
    {
        string GetContainerRoot(string id);

        void CreateContainerDirectory(string id);

        string GetSubdirectory(string id, string destination);

        IEnumerable<string> ContainerIds();
    }
}