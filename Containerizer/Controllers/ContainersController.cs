using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Web.Http;
using Newtonsoft.Json;
using Containerizer.Services.Interfaces;
using System.Threading.Tasks;

namespace Containerizer.Controllers
{
    public class CreateResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class ContainersController : ApiController
    {
        private ICreateContainerService createContainerService;
        private IStreamOutService streamOutService;

        public ContainersController(ICreateContainerService createContainerService, IStreamOutService streamOutService)
        {
            this.createContainerService = createContainerService;
            this.streamOutService = streamOutService;
        }

        [Route("api/containers")]
        public async Task<IHttpActionResult> Post()
        {
            try
            {
               var id = await createContainerService.CreateContainer();
               return Json(new CreateResponse { Id = id });
            }
            catch (CouldNotCreateContainerException ex)
            {
                return this.InternalServerError(ex);
            }
        }

        [Route("api/containers/{id}/files/{fileName}")]
        [HttpGet]
        public  Task<HttpResponseMessage> StreamOut(string id, string fileName)
        {
            var outStream = streamOutService.StreamFile(id, fileName);
            var response = Request.CreateResponse();
            response.Content = new StreamContent(outStream);
            return Task.FromResult(response);
        }
    }
}
