using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Containerizer;
using Containerizer.Controllers;
using Microsoft.Owin.Logging;
using Owin;
using Owin.WebSocket.Extensions;
using Containerizer.Services.Implementations;


namespace Containerizer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            // Web API routes
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional });

            // Remove XML formatter
            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects;
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Create and assign a dependency resolver for Web API to use.
            var dependencyResolver = new DependencyResolver();
            config.DependencyResolver = dependencyResolver;

            // Make sure the Autofac lifetime scope is passed to Web API.
            app.UseAutofacWebApi(config);

            app.MapWebSocketPattern<ContainerProcessHandler>("/api/containers/(?<handle>.*)/run", dependencyResolver);

            app.UseWebApi(config);
        }
    }
}