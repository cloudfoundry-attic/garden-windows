#region

using System.Collections.Generic;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface IContainerPathService
    {
        string GetContainerRoot(string id);
    }
}