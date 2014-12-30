using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;

namespace Containerizer.Services.Implementations
{
    public class MetadataService : IMetadataService
    {
        public string GetMetadata(string handle, string key)
        {
            return ((Dictionary<string, string>) HttpContext.Current.Application[handle])[key];
        }

        public void SetMetadata(string handle, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void BulkSetMetadata(string handle, Dictionary<string, string> properties)
        {
            HttpContext.Current.Application[handle] = properties;
        }
    }
}