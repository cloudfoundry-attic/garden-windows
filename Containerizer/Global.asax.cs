#region

using System.Web;
using System.Web.Http;

#endregion

namespace Containerizer
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}