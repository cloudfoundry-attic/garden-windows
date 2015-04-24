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
                    var properties = x.GetProperties();
                    return desiredProps.All(p =>
                    {
                        string propValue;
                        var exists = properties.TryGetValue(p.Key, out propValue);
                        return exists && propValue == p.Value;
                    });
                });
            }
            return containers.Select(x => x.Handle).ToList();
        }

        [Route("api/containers")]
        [HttpPost]
        public CreateResponse Create(ContainerSpecApiModel spec)
        {
            var containerSpec = new ContainerSpec
            {
                Handle = spec.Handle,
                Properties = spec.Properties,
            };

            try
            {
                var container = containerService.CreateContainer(containerSpec);

                return new CreateResponse
                {
                    Handle = container.Handle
                };
            }
            catch (PrincipalExistsException ex)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent(string.Format("handle already exists: {0}", spec.Handle))
                };
                throw new HttpResponseException(response);
            }
            catch (Exception ex)
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(ex.Message)
                };
                throw new HttpResponseException(response);
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