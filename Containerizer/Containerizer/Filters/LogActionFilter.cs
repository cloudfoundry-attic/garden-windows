using Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Containerizer.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LogActionFilter : ActionFilterAttribute
    {
        private readonly ILogger logger;

        public LogActionFilter(ILogger logger)
        {
            this.logger = logger;
        }

        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            base.OnActionExecuting(filterContext);
            var requestData = new RequestData
            {
                method = filterContext.Request.Method.Method,
                uri = filterContext.Request.RequestUri,
                start = start,
                elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - start,
                routeTemplate = filterContext.RequestContext.RouteData.Route.RouteTemplate,
                arguments = filterContext.ActionArguments,
            };
            logger.Info(Newtonsoft.Json.JsonConvert.SerializeObject(requestData));
        }

        private class RequestData
        {
            public string method;
            public Uri uri;
            public long start;
            public long elapsed;
            public string routeTemplate;
            public IDictionary<string, object> arguments;
        }
    }
}