#region

using System.Collections.Generic;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface IPropertyService
    {
        string Get(string handle, string key);
        Dictionary<string, string> GetAll(string handle);
        void Set(string handle, string key, string value);
        void BulkSet(string handle, Dictionary<string, string> properties);
        void BulkSetWithContainerPath(string containerPath, Dictionary<string, string> properties);
        void Destroy(string handle, string key);
    }
}