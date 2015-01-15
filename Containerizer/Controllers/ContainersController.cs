#region

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace Containerizer.Controllers
{
    public class CreateResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class ContainersController : ApiController
    {
        private readonly IContainerPathService containerPathService;
        private readonly IContainerService _containerService;
        private readonly IPropertyService propertyService;


        public ContainersController(IContainerPathService containerPathService,
            IContainerService _containerService, IPropertyService propertyService)
        {
            this.containerPathService = containerPathService;
            this._containerService = _containerService;
            this.propertyService = propertyService;
        }

        [Route("api/containers")]
        [HttpGet]
        public Task<IHttpActionResult> Index()
        {
            return Task.FromResult((IHttpActionResult) Json(containerPathService.ContainerIds()));
        }

        [Route("api/containers")]
        [HttpPost]
        public async Task<IHttpActionResult> Create()
        {
            string content = await Request.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(content);
            string id = _containerService.CreateContainer(json["Handle"].ToString());

            if (json["Properties"] != null)
            {
                var properties =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(json["Properties"].ToString());
                propertyService.BulkSet(id, properties);
            }
            return Json(new CreateResponse
            {
                Id = id
            });
        }

        [Route("api/containers/{id}")]
        [HttpDelete]
        public Task<HttpResponseMessage> Destroy(string id)
        {
            //ServerManager serverManager = ServerManager.OpenRemote("localhost");
            //Site site = serverManager.Sites[id];
            //site.Stop();

            if (containerPathService.ContainerIds().Contains(id))
            {
                containerPathService.DeleteContainerDirectory(id);
                return Task.FromResult(Request.CreateResponse(HttpStatusCode.OK));
            }

            return Task.FromResult(Request.CreateResponse(HttpStatusCode.NotFound));
        }
    }
}