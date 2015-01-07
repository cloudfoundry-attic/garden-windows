#region

using System;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface ICreateContainerService
    {
        string CreateContainer(String containerId);
    }
}