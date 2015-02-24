using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Containerizer;
using Containerizer.Controllers;
using Microsoft.Owin.Logging;
using Owin;
using Owin.WebSocket.Extensions;


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

            //var builder = new ContainerBuilder();

            // Register Web API controller in executing assembly.
            //builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // Register a logger service to be used by the controller and middleware.
            // builder.Register(c => new Logger()).As<ILogger>().InstancePerRequest();

            // Autofac will add middleware to IAppBuilder in the order registered.
            // The middleware will execute in the order added to IAppBuilder.
            //builder.RegisterType<FirstMiddleware>().InstancePerRequest();
            //builder.RegisterType<SecondMiddleware>().InstancePerRequest();

            //var container = builder.Build();

            // Create and assign a dependency resolver for Web API to use.
            var dependencyResolver = new DependencyResolver();
            config.DependencyResolver = dependencyResolver;

            // This should be the first middleware added to the IAppBuilder.
            //app.UseAutofacMiddleware(container);

            // Make sure the Autofac lifetime scope is passed to Web API.
            app.UseAutofacWebApi(config);

            app.MapWebSocketPattern<ContainerProcessHandler>("/api/containers/(?<handle>.*)/run", dependencyResolver);

            app.UseWebApi(config);
        }
    }
}