#region

using System.Collections.Generic;
using System.Web;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using System.IO;

#endregion 

namespace Containerizer.Services.Implementations
{
    public class PropertyService : IPropertyService
    {
        private IContainerPathService pathService;

        public PropertyService(IContainerPathService pathService) {
            this.pathService = pathService;
        }

        public string Get(string handle, string key)
        {
            Dictionary<string, string> properties = GetAll(handle);
            if (properties[key] == null)
            {
                throw new KeyNotFoundException();
            }
            return properties[key];
        }

        public void Set(string handle, string key, string value)
        {
            Dictionary<string, string> properties;
            var propertiesFileName = GetFileNameFromHandle(handle);
            if (File.Exists(propertiesFileName)) {
                properties = GetAll(handle);
            } else {
                properties = new Dictionary<string, string>();
            }
            properties[key] = value;

            WritePropertiesToDisk(propertiesFileName, properties);
        }

        public void BulkSet(string handle, Dictionary<string, string> properties)
        {
            WritePropertiesToDisk(GetFileNameFromHandle(handle), properties);
        }

        public void BulkSetWithContainerPath(string containerPath, Dictionary<string, string> properties)
        {
            WritePropertiesToDisk(GetFileName(containerPath), properties);
        }

        public void Destroy(string handle, string key)
        {
            Dictionary<string, string> properties = GetAll(handle);
            if (properties[key] == null)
            {
                throw new KeyNotFoundException();
            }

            properties.Remove(key);
            WritePropertiesToDisk(GetFileNameFromHandle(handle), properties);
        }

        public Dictionary<string, string> GetAll(string handle)
        {
            var fileJson = File.ReadAllText(GetFileNameFromHandle(handle));
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(fileJson);
        }

        private void WritePropertiesToDisk(string propertiesFileName, Dictionary<string, string> properties)
        { 
            File.WriteAllText(propertiesFileName, JsonConvert.SerializeObject(properties));
        }

        private string GetFileNameFromHandle(string handle)
        {
            return GetFileName(pathService.GetContainerRoot(handle));
        }

        private string GetFileName(string containerPath)
        {
            return Path.Combine(containerPath, "properties.json");
        }

        class KeyNotFoundException : System.Exception { }
    }
}