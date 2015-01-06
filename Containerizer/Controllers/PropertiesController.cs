#region

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IMetadataService metadataService;


        public PropertiesController(IMetadataService metadataService)
        {
            this.metadataService = metadataService;
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpGet]
        public async Task<IHttpActionResult> Show(string handle, string propertyKey)
        {
            return
                Json(new GetPropertyResponse
                {
                    Value = metadataService.GetMetadata(handle, propertyKey),
                });
        }

        [Route("api/containers/{id}/properties/{propertyKey}")]
        [HttpPut]
        public async Task<IHttpActionResult> Update(string id, string propertyKey)
        {
            string requestBody = await Request.Content.ReadAsStringAsync();
            HttpApplicationState application = HttpContext.Current.Application;
            if (application[id] == null)
            {
                application[id] = new Dictionary<string, string>();
            }
            ((Dictionary<string, string>) HttpContext.Current.Application[id])[propertyKey] = requestBody;
            return Json(new GetPropertyResponse {Value = "I did a thing"});
        }

        [Route("api/containers/{id}/properties")]
        [HttpGet]
        public Task<HttpResponseMessage> Index(string id)
        {
            var dictionary = (Dictionary<string, string>) HttpContext.Current.Application[id];
            if (dictionary == null)
            {
                const string ourJson = "{}";
                var emptyResponse = new HttpResponseMessage
                {
                    Content = new StringContent(
                        ourJson,
                        Encoding.UTF8,
                        "application/json"
                        )
                };
                return Task.FromResult(emptyResponse);
            }
            string jsonDictionary = JsonConvert.SerializeObject(dictionary, new KeyValuePairConverter());

            var response = new HttpResponseMessage
            {
                Content = new StringContent(
                    jsonDictionary,
                    Encoding.UTF8,
                    "application/json"
                    )
            };
            return Task.FromResult(response);
        }

        [Route("api/containers/{handle}/properties/{key}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> Destroy(string handle, string key)
        {

            metadataService.Destroy(handle, key);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}