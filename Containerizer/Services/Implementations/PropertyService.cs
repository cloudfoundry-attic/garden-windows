#region

using System.Collections.Generic;
using System.Web;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;

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
            Dictionary<string, string> properties = GetProperties(handle);
            if (properties[key] == null)
            {
                throw new KeyNotFoundException();
            }
            return properties[key];
        }

        public void Set(string handle, string key, string value)
        {
            Dictionary<string, string> properties = GetProperties(handle);
            properties[key] = value;

            WritePropertiesToDisk(handle, properties);
        }

        private Dictionary<string, string> GetProperties(string handle)
        {
            Dictionary<string, string> properties;
            var fileName = GetFileName(handle);

            if (System.IO.File.Exists(fileName))
            {
                var fileJson = System.IO.File.ReadAllText(fileName);
                properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileJson);
            }
            else
            {
                properties = new Dictionary<string, string>();
            }
            return properties;
        }

        public void BulkSet(string handle, Dictionary<string, string> properties)
        {
            WritePropertiesToDisk(handle, properties);
        }

        public void Destroy(string handle, string key)
        {
            Dictionary<string, string> properties = GetProperties(handle);
            if (properties[key] == null)
            {
                throw new KeyNotFoundException();
            }

            properties.Remove(key);
            WritePropertiesToDisk(handle, properties);
        }

        public Dictionary<string, string> GetAll(string handle)
        {
            var fileJson = System.IO.File.ReadAllText(GetFileName(handle));
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(fileJson);
        }

        private void WritePropertiesToDisk(string handle, Dictionary<string, string> properties)
        { 
            System.IO.File.WriteAllText(GetFileName(handle), JsonConvert.SerializeObject(properties));
        }

        private string GetFileName(string handle)
        {
            return pathService.GetContainerRoot(handle) + ".json";
        }

        class KeyNotFoundException : System.Exception { }
    }
}