#region

using System;
using System.Net.Http;
using NSpec;

#endregion

namespace Containerizer.Tests.Specs.Features
{
    internal class PingSpec : nspec
    {
        private void describe_ping()
        {
            Helpers.ContainerizerProcess process = null;

            before = () => process = Helpers.CreateContainerizerProcess();
            after = () => process.Dispose();

            context["given that containerizer is running"] = () =>
            {
                describe["ping"] = () =>
                {
                    it["succeeds"] = () =>
                    {
                        HttpResponseMessage getTask = process.GetClient().GetAsync("/api/Ping").GetAwaiter().GetResult();
                        getTask.IsSuccessStatusCode.should_be_true();
                    };
                };
            };
        }
    }
}