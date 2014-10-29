using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Containerizer.Controllers
{
    public class ContainersController : ApiController
    {
        // GET: api/Containers
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Containers/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Containers
        public void Post([FromBody]string value)
        {
           // return "Container started";
        }

        // PUT: api/Containers/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Containers/5
        public IHttpActionResult Delete(int id)
        {
            return Ok();
        }
    }
}
