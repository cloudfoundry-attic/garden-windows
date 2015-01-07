#region

using System;
using System.Threading.Tasks;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface ICreateContainerService
    {
        string CreateContainer(String containerId);
    }
}