#region

using System.Collections.Generic;

#endregion

namespace Containerizer.Services.Interfaces
{
    public interface IMetadataService
    {
        string GetMetadata(string handle, string key);
        void SetMetadata(string handle, string key, string value);
        void BulkSetMetadata(string handle, Dictionary<string, string> properties);
        void Destroy(string handle, string key);
    }
}