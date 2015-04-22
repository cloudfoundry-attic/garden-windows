using Containerizer.Models;
using NSpec;

namespace Containerizer.Tests.Specs.Models
{
    class ArgumentEscaperSpec : nspec
    {
        public void describe_()
        {
            describe["#Escape"] = () =>
            {
                it["Wraps each argument in double quotes"] = () =>
                {
                    var args = new[] {"/app", "", "/path with spaces/app"};
                    var result = ArgumentEscaper.Escape(args);
                    result.should_be("\"/app\" \"\" \"/path with spaces/app\"");
                };

                it["Escapes embedded double quotes"] = () =>
                {
                    var args = new[] {"/path\"with\"quotes/app"};
                    var result = ArgumentEscaper.Escape(args);
                    result.should_be("\"/path\\\"with\\\"quotes/app\"");
                };

                it["Escapes embedded backslashes"] = () =>
                {
                    var args = new[] { "{\"start_command\":\"start\\yo\"}" };
                    var result = ArgumentEscaper.Escape(args);
                    result.should_be("\"{\\\"start_command\\\":\\\"start\\\\yo\\\"}\"");
                };
            };
        }
    }
}
