using System;
using System.Collections.Generic;
using NSpec;
using System.Linq;
using System.Web.Http.Results;

namespace Containerizer.Tests
{
    class ContainersControllerSpec : nspec
    {
        Containerizer.Controllers.ContainersController containersController;

        void before_each()
        {
            containersController = new Controllers.ContainersController();
            
        }

        void describe_get()
        {
            it["returns an array of strings"] = () =>
            {
                var response = containersController.Get();
                response.should_be(new string[] { "value1", "value2" });
            };
        }

        void describe_delete()
        {
            context["in context"] = () =>
            {
                it["returns a successful status code"] = () =>
                {
                    var response = containersController.Delete(0);
                    response.should_cast_to<OkResult>();
                };
            };
        }
    }
}


