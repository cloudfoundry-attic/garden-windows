using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Dependencies;
using Autofac;
using Containerizer.Controllers;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;

namespace Containerizer
{
    public class DependencyResolver : IDependencyResolver
    {
        private IContainer container;

        public DependencyResolver()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<CreateContainerService>().As<ICreateContainerService>();
            containerBuilder.RegisterType<ContainerPathService>().As<IContainerPathService>();
            containerBuilder.RegisterType<StreamInService>().As<IStreamInService>();
            containerBuilder.RegisterType<StreamOutService>().As<IStreamOutService>();
            containerBuilder.RegisterType<TarStreamService>().As<ITarStreamService>();
            containerBuilder.RegisterType<ContainersController>();
            this.container = containerBuilder.Build();
        }

        IDependencyScope IDependencyResolver.BeginScope()
        {
            return new DependencyResolver();
        }

        object IDependencyScope.GetService(Type serviceType)
        {
            return container.ResolveOptional(serviceType);
        }

        IEnumerable<object> IDependencyScope.GetServices(Type serviceType)
        {
            IEnumerable<object> collection = (IEnumerable<object>)container.ResolveOptional(serviceType);
            if (collection == null)
            {
                return new List<object>();
            }
            else
            {
                return collection;
            }
        }

        void IDisposable.Dispose()
        {
            container.Dispose();
        }
    }
}