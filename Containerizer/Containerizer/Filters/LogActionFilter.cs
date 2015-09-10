using System.Globalization;
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
            logger.Info("containerizer.request.complete", new Dictionary<string, object>
            {
                {"method", filterContext.Request.Method.Method},
                {"uri", filterContext.Request.RequestUri.ToString()},
                {"start", start.ToString()},
                {"elapsed", (System.Diagnostics.Stopwatch.GetTimestamp() - start).ToString(CultureInfo.InvariantCulture)},
                {"routeTemplate", filterContext.RequestContext.RouteData.Route.RouteTemplate},
                {"arguments", filterContext.ActionArguments},
            });
        }
    }
}