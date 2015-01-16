#region

using System;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface IContainerService
    {
        string CreateContainer(String containerId);
        void DeleteContainer(String containerId);
    }
}