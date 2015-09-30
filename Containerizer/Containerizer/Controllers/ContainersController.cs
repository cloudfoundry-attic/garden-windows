#region

using System.IO;
using System.Threading.Tasks;
using Containerizer.Models;
using IronFrame;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

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
        private const int MaxRetrys = 5;
        private const int DelayInMilliseconds = 1000;
        private readonly IContainerService containerService;
        private readonly ILogger logger;
        private const uint CONTAINER_ACTIVE_PROCESS_LIMIT = 10;
        internal const int CONTAINER_DEFAULT_CPU_WEIGHT = 5;
        private static readonly object containerDeletionLock = new Object();

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

                lock (containerDeletionLock)
                {
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
                        catch (DirectoryNotFoundException)
                        {
                            return false;
                        }
                    });
                }
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
                container.LimitCpu(CONTAINER_DEFAULT_CPU_WEIGHT);
                if (spec.Limits.MemoryLimits.LimitInBytes != null)
                    container.LimitMemory(spec.Limits.MemoryLimits.LimitInBytes.Value);
                if (spec.Limits.DiskLimits.ByteHard != null)
                    container.LimitDisk(spec.Limits.DiskLimits.ByteHard.Value);

                return new CreateResponse
                {
                    Handle = container.Handle
                };
            }
            catch (PrincipalExistsException)
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
        public async Task<IHttpActionResult> Destroy(string handle)
        {
            var container = containerService.GetContainerByHandle(handle);
            if (container != null)
            {
                Exception ex = await Retry(() => containerService.DestroyContainer(handle), MaxRetrys, DelayInMilliseconds);
                if(ex == null) return Ok();
                return InternalServerError(ex);
            }

            return NotFound();
        }


        private Task<Exception> Retry(Action destroy, int maxRetrys, int delayInMilliseconds)
        {
            return Task.Run<Exception>(async () =>
            {
                Exception lastException = null;
                while (maxRetrys > 0)
                {
                    try
                    {
                        lock (containerDeletionLock)
                        {
                            destroy();
                        }
                        lastException = null;
                        break;
                    }
                    catch (Exception exception)
                    {
                        lastException = exception;
                        maxRetrys--;
                        logger.Info("Retrying container deletion", new Dictionary<string,object> {
                            {"reason", lastException},
                        });
                    }
                    await Task.Delay(delayInMilliseconds);
                }
                return lastException;
            });
        }
    }
}
