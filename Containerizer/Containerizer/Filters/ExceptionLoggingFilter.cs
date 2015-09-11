using System.Collections.Generic;
using System.Web.Http.Filters;
using Logger;

namespace Containerizer.Filters
{
    public class ExceptionLoggingFilter : ExceptionFilterAttribute
    {
        private ILogger logger;

        public ExceptionLoggingFilter(ILogger logger)
        {
            this.logger = logger;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            var actionContext = context.ActionContext;
            logger.Error("unhandled exception", new Dictionary<string,object>
            {
                {"exception", context.Exception},
                {"method", actionContext.Request.Method.Method},
                {"uri", actionContext.Request.RequestUri.ToString()},
                {"routeTemplate", actionContext.RequestContext.RouteData.Route.RouteTemplate},
                {"arguments", actionContext.ActionArguments},
            });
            base.OnException(context);
        }
    }
}