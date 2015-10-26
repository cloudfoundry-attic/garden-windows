using Containerizer.Controllers;
using Containerizer.Services.Implementations;
using NSpec;

namespace Containerizer.Tests.Specs.Controllers
{
    internal class CapacityControllerSpec : nspec
    {
        private void describe_()
        {
            describe[Controller.Index] = () =>
            {
                CapacityController controller = null;

                before = () => {
                    var options = new Options() { };
                    controller = new CapacityController(options); 
                };

                it["returns positive capacity for MemoryInBytes"] = () =>
                {
                    Capacity capacity = controller.Index();
                    capacity.MemoryInBytes.should_be_greater_than(0);
                };

                it["returns positive capacity for DiskInBytes"] = () =>
                {
                    Capacity capacity = controller.Index();
                    capacity.DiskInBytes.should_be_greater_than(0);
                };

                it["returns positive capacity for MaxContainers"] = () =>
                {
                    Capacity capacity = controller.Index();
                    capacity.MaxContainers.should_be(100);
                };
            };
        }
    }
}