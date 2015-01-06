#region

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;

#endregion

namespace Containerizer.Controllers
{
    public class FilesController : ApiController
    {
        private readonly IStreamInService streamInService;
        private readonly IStreamOutService streamOutService;


        public FilesController(IStreamInService streamInService, IStreamOutService streamOutService)
        {
            this.streamOutService = streamOutService;
            this.streamInService = streamInService;
        }

        [Route("api/containers/{id}/files")]
        [HttpGet]
        public Task<HttpResponseMessage> Show(string id, string source)
        {
            Stream outStream = streamOutService.StreamOutFile(id, source);
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = new StreamContent(outStream);
            return Task.FromResult(response);
        }

        [Route("api/containers/{id}/files")]
        [HttpPut]
        public async Task<HttpResponseMessage> Create(string id, string destination)
        {
            Stream stream = await Request.Content.ReadAsStreamAsync();
            streamInService.StreamInFile(stream, id, destination);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}