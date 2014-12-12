using System;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface ICreateContainerService
    {
        /// <exception cref="Containerizer.Services.Interfaces.CouldNotCreateContainerException">
        ///     Thrown when container could not be created.
        /// </exception>
        Task<string> CreateContainer(String containerId);
    }

    public class CouldNotCreateContainerException : Exception
    {
        public CouldNotCreateContainerException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}