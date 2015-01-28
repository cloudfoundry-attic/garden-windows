#region

using System.Net.Http;
using System.Web.Http;
using Containerizer.Controllers;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class RunControllerSpec : nspec
    {
        private RunController runController;

        private void before_each()
        {
            /*
            runController = new RunController
            {
                Configuration = new HttpConfiguration(),
                Request = new HttpRequestMessage()
            };
            */
        }
    }
}