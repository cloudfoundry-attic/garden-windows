#region

using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Autofac;
using Containerizer.Controllers;
using Containerizer.Services.Implementations;
using Containerizer.Services.Interfaces;
using IronFoundry.Container;
using IronFoundry.Container.Internal;
using IronFoundry.Container.Utilities;
using System.IO;
using System.Runtime.InteropServices;
using Containerizer.Factories;
using Microsoft.Owin.Hosting.Services;
using Microsoft.Practices.ServiceLocation;
using Logger;

#endregion

namespace Containerizer
{
    public class DependencyResolver : IDependencyResolver, IServiceLocator
    {
        private readonly Autofac.IContainer container;
        private static IContainerService containerService;
        private static IContainerPropertyService containerPropertyService;
        public static IExternalIP ExternalIP;
        public static ILogger logger;

        static DependencyResolver()
        {
            containerService = new ContainerServiceFactory().New();
            var fileSystemManager = new FileSystemManager();
            containerPropertyService = new LocalFilePropertyService(fileSystemManager, "properties.json");
            logger = new Logger.Logger("containerizer");
        }

        public DependencyResolver()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.Register(context => containerService).As<IContainerService>();
            containerBuilder.Register(context => ExternalIP).As<IExternalIP>();
            containerBuilder.RegisterType<RunService>().As<IRunService>();
            containerBuilder.RegisterType<ContainerInfoService>().As<IContainerInfoService>();
            containerBuilder.RegisterType<StreamInService>().As<IStreamInService>();
            containerBuilder.RegisterType<StreamOutService>().As<IStreamOutService>();
            containerBuilder.RegisterType<TarStreamService>().As<ITarStreamService>();
            containerBuilder.Register(context => containerPropertyService).As<IContainerPropertyService>();
            containerBuilder.Register(context => logger).As<ILogger>();
            containerBuilder.RegisterType<ContainersController>();
            containerBuilder.RegisterType<FilesController>();
            containerBuilder.RegisterType<NetController>();
            containerBuilder.RegisterType<PropertiesController>();
            containerBuilder.RegisterType<InfoController>();
            containerBuilder.RegisterType<BulkInfoController>();
            containerBuilder.RegisterType<ContainerProcessHandler>();
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

        IEnumerable<TService> IServiceLocator.GetAllInstances<TService>()
        {
            Console.WriteLine("NotImplementedException: IEnumerable<TService> IServiceLocator.GetAllInstances<TService>()");
            throw new NotImplementedException();
        }

        IEnumerable<object> IServiceLocator.GetAllInstances(Type serviceType)
        {
            Console.WriteLine("NotImplementedException: IEnumerable<object> IServiceLocator.GetAllInstances(Type serviceType)");
            throw new NotImplementedException();
        }

        TService IServiceLocator.GetInstance<TService>(string key)
        {
            Console.WriteLine("NotImplementedException: TService IServiceLocator.GetInstance<TService>(string key)");
            throw new NotImplementedException();
        }

        TService IServiceLocator.GetInstance<TService>()
        {
            Type type = typeof (TService);
            return (TService)container.ResolveOptional(type);
        }

        object IServiceLocator.GetInstance(Type serviceType, string key)
        {
            Console.WriteLine("NotImplementedException: object IServiceLocator.GetInstance(Type serviceType, string key)");
            throw new NotImplementedException();
        }

        object IServiceLocator.GetInstance(Type serviceType)
        {
            Console.WriteLine("NotImplementedException: object IServiceLocator.GetInstance(Type serviceType)");
            throw new NotImplementedException();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            Console.WriteLine("NotImplementedException: object IServiceProvider.GetService(Type serviceType)");
            throw new NotImplementedException();
        }
    }
}