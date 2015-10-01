using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSpec;

namespace Containerizer.Tests.Specs.Features
{
    internal class ConsumerCanDestroyContainerSpec : nspec
    {
        private void describe_consumer_can_destroy_a_container()
        {
            Helpers.ContainerizerProcess process = null;
            HttpClient client = null;
            string handle = null;

            before = () => process = Helpers.CreateContainerizerProcess();
            after = () => process.Dispose();

            context["given that I am a consumer of the api and a container exists"] = () =>
            {
                before = () =>
                {
                    client = process.GetClient();

                    handle = Helpers.CreateContainer(client);
                };

                context["when I send two destroy request"] = () =>
                {
                    it["responds with 200 and then 404"] = () =>
                    {
                        var response = client.DeleteAsync("/api/containers/" + handle).GetAwaiter().GetResult();
                        response.StatusCode.should_be(HttpStatusCode.OK);

                        response = client.DeleteAsync("/api/containers/" + handle).GetAwaiter().GetResult();
                        response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };

                it["deletes the container's resources"] = () =>
                {
                    var response = client.DeleteAsync("/api/containers/" + handle).GetAwaiter().GetResult();
                    response.StatusCode.should_be(HttpStatusCode.OK);

                    Directory.Exists(Helpers.GetContainerPath(handle)).should_be_false();
                };
            };
        }
    }
}