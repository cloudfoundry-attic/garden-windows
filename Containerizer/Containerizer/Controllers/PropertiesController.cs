#region

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IronFrame;
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
        private readonly IContainerService containerService;

        public PropertiesController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/properties")]
        [HttpGet]
        public IHttpActionResult Index(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            var properties = container.GetProperties();
            return Json(properties);
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpGet]
        public IHttpActionResult Show(string handle, string propertyKey)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
                return ResponseMessage(Request.CreateResponse(System.Net.HttpStatusCode.NotFound, string.Format("unknown handle: {0}", handle)));


            var value = container.GetProperty(propertyKey);
            if (value == null)
            {
                return ResponseMessage(Request.CreateResponse(System.Net.HttpStatusCode.NotFound, string.Format("property does not exist: {0}", propertyKey)));
            }

            return Json(value);
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpPut]
        public async Task<IHttpActionResult> Update(string handle, string propertyKey)
        {
            string requestBody = await Request.Content.ReadAsStringAsync();
            var container = containerService.GetContainerByHandle(handle);
            container.SetProperty(propertyKey, requestBody);
            return Json(new GetPropertyResponse {Value = "I did a thing"});
        }

        [Route("api/containers/{handle}/properties/{key}")]
        [HttpDelete]
        public Task<IHttpActionResult> Destroy(string handle, string key)
        {
            var container = containerService.GetContainerByHandle(handle);
            container.RemoveProperty(key);
            return Task.FromResult((IHttpActionResult)Ok());
        }
    }
}