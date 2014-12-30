#region

using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Containerizer.Facades;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

#endregion

namespace Containerizer.Controllers
{
    public class CreateResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class NetInResponse
    {
        [JsonProperty("hostPort")]
        public int HostPort { get; set; }
    }

    public class GetPropertyResponse
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ContainersController : ApiController
    {
        private readonly IContainerPathService containerPathService;
        private readonly ICreateContainerService createContainerService;
        private readonly INetInService netInService;
        private readonly IStreamInService streamInService;
        private readonly IStreamOutService streamOutService;
        private IMetadataService metadataService;


        public ContainersController(IContainerPathService containerPathService,
            ICreateContainerService createContainerService, IStreamInService streamInService,
            IStreamOutService streamOutService, INetInService netInService, IMetadataService metadataService)
        {
            this.containerPathService = containerPathService;
            this.createContainerService = createContainerService;
            this.streamOutService = streamOutService;
            this.streamInService = streamInService;
            this.netInService = netInService;
            this.metadataService = metadataService;
        }

        [Route("api/containers")]
        [HttpGet]
        public async Task<IHttpActionResult> List()
        {
            return Json(containerPathService.ContainerIds());
        }

        [Route("api/containers")]
        [HttpPost]
        public async Task<IHttpActionResult> Post()
        {
            try
            {
                string content = await Request.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(content);
                string id = await createContainerService.CreateContainer(json["Handle"].ToString());

                if (json["Properties"] != null)
                {
                    var properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(json["Properties"].ToString());
                    metadataService.BulkSetMetadata(id, properties);
                }
                return Json(new CreateResponse { Id = id });
            }
            catch (CouldNotCreateContainerException ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("api/containers/{id}/files")]
        [HttpGet]
        public Task<HttpResponseMessage> StreamOut(string id, string source)
        {
            Stream outStream = streamOutService.StreamOutFile(id, source);
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = new StreamContent(outStream);
            return Task.FromResult(response);
        }

        [Route("api/containers/{id}/run")]
        [HttpGet]
        public Task<HttpResponseMessage> Run(string id)
        {
            HttpContext.Current.AcceptWebSocketRequest(new ContainerProcessHandler(id, new ContainerPathService(),
                new ProcessFacade()));
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
            return Task.FromResult(response);
        }

        [Route("api/containers/{id}/files")]
        [HttpPut]
        public async Task<HttpResponseMessage> StreamIn(string id, string destination)
        {
            Stream stream = await Request.Content.ReadAsStreamAsync();
            streamInService.StreamInFile(stream, id, destination);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Route("api/containers/{id}/net/in")]
        [HttpPost]
        public async Task<IHttpActionResult> NetIn(string id)
        {
            NameValueCollection formData = await Request.Content.ReadAsFormDataAsync();

            int hostPort = int.Parse(formData.Get("hostPort"));
            hostPort = netInService.AddPort(hostPort, id);
            return Json(new NetInResponse { HostPort = hostPort });
        }

        [Route("api/containers/{handle}/properties/{propertyKey}")]
        [HttpGet]
        public async Task<IHttpActionResult> GetProperty(string handle, string propertyKey)
        {
            return
                Json(new GetPropertyResponse
                {
                    Value = metadataService.GetMetadata(handle, propertyKey),
                });
        }

        [Route("api/containers/{id}/properties/{propertyKey}")]
        [HttpPut]
        public async Task<IHttpActionResult> SetProperty(string id, string propertyKey)
        {
            propertyKey = propertyKey.Replace("♥", ":");
            string requestBody = await Request.Content.ReadAsStringAsync();
            HttpApplicationState application = HttpContext.Current.Application;
            if (application[id] == null)
            {
                application[id] = new Dictionary<string, string>();
                ((Dictionary<string, string>)application[id])["tag:foo"] = "bar";
            }
            ((Dictionary<string, string>)HttpContext.Current.Application[id])[propertyKey] = requestBody;
            return Json(new GetPropertyResponse { Value = "I did a thing" });
        }

        [Route("api/containers/{id}/properties")]
        [HttpGet]
        public Task<HttpResponseMessage> GetProperties(string id)
        {
            var dictionary = (Dictionary<string, string>)HttpContext.Current.Application[id];
            if (dictionary == null)
            {
                string ourJson = "{}";
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

        [Route("api/containers/{id}/properties/{propertyKey}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> RemoveProperty(string id, string propertyKey)
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Route("api/containers/{id}")]
        [HttpDelete]
        public async Task<HttpResponseMessage> DeleteContainer(string id)
        {
            //ServerManager serverManager = ServerManager.OpenRemote("localhost");
            //Site site = serverManager.Sites[id];
            //site.Stop();
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}