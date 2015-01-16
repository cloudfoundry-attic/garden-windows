using Containerizer.Models;
using NSpec;

namespace Containerizer.Tests.Specs.Models
{
    class ApiProcessSpecSpec : nspec
    {
        public void describe_()
        {
            describe["#Arguments"] = () =>
            {
                it["Wraps each argument in double quotes"] = () =>
                {
                    var processSpec = new ApiProcessSpec
                    {
                        Args = new [] { "/app", "", "/path with spaces/app" }
                    };
                    var result = processSpec.Arguments();
                    result.should_be("\"/app\" \"\" \"/path with spaces/app\"");
                };

                it["Escapes embedded double quotes"] = () =>
                {
                    var processSpec = new ApiProcessSpec
                    {
                        Args = new[] { "/path\"with\"quotes/app" }
                    };
                    var result = processSpec.Arguments();
                    result.should_be("\"/path\\\"with\\\"quotes/app\"");
                };

                it["Escapes embedded backslashes"] = () =>
                {
                    var processSpec = new ApiProcessSpec
                    {
                        Args = new[] { "{\"start_command\":\"start\\yo\"}" }
                    };
                    var result = processSpec.Arguments();
                    result.should_be("\"{\\\"start_command\\\":\\\"start\\\\yo\\\"}\"");
                };
            };
        }
    }
}
