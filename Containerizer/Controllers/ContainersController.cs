#region

using System.Collections.Generic;
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
        private readonly ICreateContainerService createContainerService;
        private readonly IMetadataService metadataService;


        public ContainersController(IContainerPathService containerPathService,
            ICreateContainerService createContainerService, IMetadataService metadataService)
        {
            this.containerPathService = containerPathService;
            this.createContainerService = createContainerService;
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
                    var properties =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(json["Properties"].ToString());
                    metadataService.BulkSetMetadata(id, properties);
                }
                return Json(new CreateResponse {Id = id});
            }
            catch (CouldNotCreateContainerException ex)
            {
                return InternalServerError(ex);
            }
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