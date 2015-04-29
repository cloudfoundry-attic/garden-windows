#region

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IronFrame;
using Containerizer.Models;
using System;
using System.DirectoryServices.AccountManagement;
using Logger;

#endregion

namespace Containerizer.Controllers
{
    public class ContainerProcessController : ApiController
    {
        private readonly IContainerService containerService;

        public ContainerProcessController(IContainerService containerService)
        {
            this.containerService = containerService;
        }

        [Route("api/containers/{handle}/processes/{pid}")]
        [HttpDelete]
        public IHttpActionResult Stop(string handle, int pid)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container == null)
                return NotFound();
            var process = container.FindProcessById(pid);
            if (process == null)
                return NotFound();
            process.Kill();
            return Ok();
        }
    }
}