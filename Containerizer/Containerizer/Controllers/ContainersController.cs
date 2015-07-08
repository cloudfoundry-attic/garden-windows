#region

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
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
    public class CreateResponse
    {
        [JsonProperty("handle")]
        public string Handle { get; set; }
    }

    public class ContainersController : ApiController
    {
        private readonly IContainerService containerService;
        private readonly ILogger logger;
        private const uint CONTAINER_ACTIVE_PROCESS_LIMIT = 10;

        public ContainersController(IContainerService containerService, ILogger logger)
        {
            this.containerService = containerService;
            this.logger = logger;
        }

        [Route("api/containers")]
        [HttpGet]
        public IReadOnlyList<string> Index(string q = null)
        {
            IEnumerable<IContainer> containers = containerService.GetContainers();

            if (q != null)
            {
                var desiredProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(q);

                containers = containers.Where((x) =>
                {
                    try
                    {
                        var properties = x.GetProperties();
                        return desiredProps.All(p =>
                        {
                            string propValue;
                            var exists = properties.TryGetValue(p.Key, out propValue);
                            return exists && propValue == p.Value;
                        });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return false;
                    }
                });
            }
            return containers.Select(x => x.Handle).ToList();
        }

        [Route("api/containers")]
        [HttpPost]
        public CreateResponse Create(ContainerSpecApiModel spec)
        {
            if (spec.Env == null)
            {
                spec.Env = new List<string>();
            }

            var containerSpec = new ContainerSpec
            {
                Handle = spec.Handle,
                Properties = spec.Properties,
                Environment = ContainerService.EnvsFromList(spec.Env)
            };

            try
            {
                var container = containerService.CreateContainer(containerSpec);
                container.SetActiveProcessLimit(CONTAINER_ACTIVE_PROCESS_LIMIT);
                container.SetPriorityClass(ProcessPriorityClass.BelowNormal);

                return new CreateResponse
                {
                    Handle = container.Handle
                };
            }
            catch (PrincipalExistsException ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.Conflict,
                    string.Format("handle already exists: {0}", spec.Handle)));
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message));
            }
        }

        [Route("api/containers/{handle}/stop")]
        [HttpPost]
        public IHttpActionResult Stop(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container != null)
            {
                container.Stop(true);
                return Ok();
            }

            return NotFound();
        }

        [Route("api/containers/{handle}")]
        [HttpDelete]
        public IHttpActionResult Destroy(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container != null)
            {
                containerService.DestroyContainer(handle);
                return Ok();
            }

            return NotFound();
        }
    }
}
