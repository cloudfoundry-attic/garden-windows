#region

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        private readonly IPropertyService propertyService;


        public PropertiesController(IPropertyService propertyService)
        {
            this.propertyService = propertyService;
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpGet]
        public Task<IHttpActionResult> Show(string handle, string propertyKey)
        {
            return Task.FromResult((IHttpActionResult)Json(new GetPropertyResponse {Value = propertyService.Get(handle, propertyKey)}));
        }

        [Route("api/containers/{id}/properties/{propertyKey}")]
        [HttpPut]
        public async Task<IHttpActionResult> Update(string id, string propertyKey)
        {
            string requestBody = await Request.Content.ReadAsStringAsync();
            propertyService.Set(id, propertyKey, requestBody);
            return Json(new GetPropertyResponse {Value = "I did a thing"});
        }

        [Route("api/containers/{id}/properties")]
        [HttpGet]
        public Task<HttpResponseMessage> Index(string id)
        {
            string json;
            try
            {
                var dictionary = propertyService.GetAll(id);
                json = JsonConvert.SerializeObject(dictionary, new KeyValuePairConverter());
            }
            catch (System.IO.FileNotFoundException e)
            {
                json = "{}";
            }

            var response = new HttpResponseMessage
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                    )
            };
            return Task.FromResult(response);
        }

        [Route("api/containers/{handle}/properties/{key}")]
        [HttpDelete]
        public Task<IHttpActionResult> Destroy(string handle, string key)
        {
            propertyService.Destroy(handle, key);
            return Task.FromResult((IHttpActionResult)Ok());
        }
    }
}