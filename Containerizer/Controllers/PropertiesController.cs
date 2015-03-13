#region

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IronFoundry.Container;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Containerizer.Controllers
{
    public class GetPropertyResponse
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class PropertiesController : ApiController
    {
        private readonly IContainerPropertyService propertyService;
        private readonly IContainerService containerService;

        public PropertiesController(IContainerPropertyService propertyService, IContainerService containerService)
        {
            this.propertyService = propertyService;
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/properties")]
        [HttpGet]
        public IHttpActionResult Index(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            var properties = propertyService.GetProperties(container);
            return Json(properties);
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpGet]
        public Task<IHttpActionResult> Show(string handle, string propertyKey)
        {
            var container = containerService.GetContainerByHandle(handle);
            return Task.FromResult((IHttpActionResult)Json(new GetPropertyResponse {Value = propertyService.GetProperty(container, propertyKey)}));
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpPut]
        public async Task<IHttpActionResult> Update(string handle, string propertyKey)
        {
            string requestBody = await Request.Content.ReadAsStringAsync();
            var container = containerService.GetContainerByHandle(handle);
            propertyService.SetProperty(container, propertyKey, requestBody);
            return Json(new GetPropertyResponse {Value = "I did a thing"});
        }

        [Route("api/containers/{handle}/properties/{key}")]
        [HttpDelete]
        public Task<IHttpActionResult> Destroy(string handle, string key)
        {
            var container = containerService.GetContainerByHandle(handle);
            propertyService.RemoveProperty(container, key);
            return Task.FromResult((IHttpActionResult)Ok());
        }
    }
}