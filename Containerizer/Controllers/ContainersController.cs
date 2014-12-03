using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Containerizer.Facades;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;

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

    public class ContainersController : ApiController
    {
        private readonly IContainerPathService containerPathService;
        private readonly ICreateContainerService createContainerService;
        private readonly IStreamInService streamInService;
        private readonly IStreamOutService streamOutService;
        private readonly INetInService netInService;

        public ContainersController(IContainerPathService containerPathService, ICreateContainerService createContainerService, IStreamInService streamInService, IStreamOutService streamOutService, INetInService netInService)
        {
            this.containerPathService = containerPathService;
            this.createContainerService = createContainerService;
            this.streamOutService = streamOutService;
            this.streamInService = streamInService;
            this.netInService = netInService;
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
                string id = await createContainerService.CreateContainer();
                return Json(new CreateResponse {Id = id});
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
            HttpContext.Current.AcceptWebSocketRequest(new ContainerProcessHandler(id, new ContainerPathService(), new ProcessFacade()));
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
            var formData = await Request.Content.ReadAsFormDataAsync();

            var hostPort = int.Parse(formData.Get("hostPort"));
            hostPort = netInService.AddPort(hostPort, id);
            return Json(new NetInResponse {HostPort = hostPort});
        }

    }
}