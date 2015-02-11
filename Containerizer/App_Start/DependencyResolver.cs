#region

using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Autofac;
using Containerizer.Controllers;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;
using System.IO;
using Containerizer.Factories;

#endregion

namespace Containerizer
{
    public class DependencyResolver : IDependencyResolver
    {
        private readonly Autofac.IContainer container;
        private static IContainerService containerService;

        static DependencyResolver()
        {
            containerService = new ContainerServiceFactory().New();
        }

        public DependencyResolver()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register(context => containerService).As<IContainerService>();
            containerBuilder.RegisterType<NetInService>().As<INetInService>();
            containerBuilder.RegisterType<StreamInService>().As<IStreamInService>();
            containerBuilder.RegisterType<StreamOutService>().As<IStreamOutService>();
            containerBuilder.RegisterType<TarStreamService>().As<ITarStreamService>();
            containerBuilder.RegisterType<PropertyService>().As<IPropertyService>();
            containerBuilder.RegisterType<ContainersController>();
            containerBuilder.RegisterType<FilesController>();
            containerBuilder.RegisterType<NetController>();
            containerBuilder.RegisterType<PropertiesController>();
            containerBuilder.RegisterType<RunController>();
            containerBuilder.RegisterType<InfoController>();
            container = containerBuilder.Build();
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
            var collection = (IEnumerable<object>) container.ResolveOptional(serviceType);
            if (collection == null)
            {
                return new List<object>();
            }
            return collection;
        }

        void IDisposable.Dispose()
        {
            container.Dispose();
        }
    }
}