#region

using System;
using System.Collections.Generic;
using System.Web;
using Containerizer.Services.Interfaces;

#endregion

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