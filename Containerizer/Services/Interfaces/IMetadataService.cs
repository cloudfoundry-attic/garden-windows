using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IMetadataService
    {
        string GetMetadata(string handle, string key);
        void SetMetadata(string handle, string key, string value);
        void BulkSetMetadata(string handle, Dictionary<string,string> properties);
    }
}
