using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public ContainersController(ICreateContainerService createContainerService)
        {
            this.createContainerService = createContainerService;
        }

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
    }
}
