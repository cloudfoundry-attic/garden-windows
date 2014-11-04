using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface ICreateContainerService
    {
        /// <exception cref="Containerizer.Services.Interfaces.CouldNotCreateContainerException">
        /// Thrown when container could not be created.
        /// </exception>
        Task<string> CreateContainer();
    }

    public class CouldNotCreateContainerException : Exception
    {
        public CouldNotCreateContainerException(Exception inner) : base(String.Empty, inner) { }
    } 
}
