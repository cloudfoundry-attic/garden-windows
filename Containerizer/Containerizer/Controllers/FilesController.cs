#region

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
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

        [Route("api/containers/{handle}/files")]
        [HttpGet]
        public Task<HttpResponseMessage> Show(string handle, string source)
        {
            Stream outStream = streamOutService.StreamOutFile(handle, source);
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = new StreamContent(outStream);
            return Task.FromResult(response);
        }

        [Route("api/containers/{handle}/files")]
        [HttpPut]
        public async Task<IHttpActionResult> Update(string handle, string destination)
        {
            try
            {
                Stream stream = await Request.Content.ReadAsStreamAsync();
                streamInService.StreamInFile(stream, handle, destination);
                return Ok();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("EntryStream has not been fully consumed"))
                {
                    return new StatusCodeResult(HttpStatusCode.RequestEntityTooLarge, this);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}