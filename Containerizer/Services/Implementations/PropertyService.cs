#region

using System;
using System.Collections.Generic;
using System.Web;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        public string Get(string handle, string key)
        {
            return ((Dictionary<string, string>) HttpContext.Current.Application[handle])[key];
        }

        public void Set(string handle, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void BulkSet(string handle, Dictionary<string, string> properties)
        {
            HttpContext.Current.Application[handle] = properties;
        }

        public void Destroy(string handle, string key)
        {
            ((Dictionary<string, string>) HttpContext.Current.Application[handle]).Remove(key);
        }
    }
}