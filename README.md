containerizer
=============

Containerizer is a restful API to the
[if_warden](https://github.com/cloudfoundry-incubator/if_warden) windows
containerization technology. When it runs with [garden
windows](https://github.com/cloudfoundry-incubator/garden-windows), it provides
a [garden](https://github.com/cloudfoundry-incubator/garden) implementation.

## dependencies
- 64 bit version of Windows (tested with Windows Server 2012 R2 Standard)
- msbuild in PATH
- Administrator access


git on windows
==============

we suggest: http://msysgit.github.io/


building on the command line
============================

1. Run make.bat in cmd.

running
============================

1. [in solution root] ```Containerizer\bin\Containerizer.exe EXTERNAL_IP PORT```,
where ```EXTERNAL_IP``` is generally the IPv4 addresss ipconfig reports (e.g.
10.10.5.4 in a VPC) and ```PORT``` is some arbitrary port that is passed
to garden-windows as well (e.g. 1788).

building in Visual Studio
========================

1. Install https://visualstudiogallery.msdn.microsoft.com/7a52473f-9e1a-40f3-8bd8-6c00ab163609 (nspec test runner)

1. Open Visual Studio as Administrator.
![opening as admin](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_as_admin.png)
![visual studio running as admin](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/showing_vs_running_as_admin.png)

1. Build solution.

advanced debugging
==================

The acceptance tests spin up containerizer out of process and communicate with
it over HTTP. Unfortunately, this means it's more difficult to debug the
server. To debug:

1. Have the server running (i.e. stop execution after starting an acceptance
test, but before letting it be killed at the end of the test). To be sure
that the server is correctly running, you should allow at least one request to
hit the server, or IIS might not spin it up right away.
![open tests with breakpoint](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_tests_with_breakpoint.png)

1. Open a new instance of Visual Studio as Administrator and open the
Containerizer solution. Then, go in the debug menu and select "Attach to
Process"
![attach process menu](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/attach_to_process_menu.png)

1. Attach to the w3wp.exe process in the available processes. Make sure that
you click "Show processes from all users".
![attach process](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/attach_process.png)

1. You can now hit debug points in the server!
![debugging in process](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/debugging_in_process.png)

1. Also, try out [entrian](http://entrian.com/attach/), which automatically attaches the Visual Studio debugger to any process as it starts.


quick tips
==========

If all of the acceptance tests start breaking mysteriously and you recently
created a new service, you probably forgot to add the service to the DI
container. Assuming you added a service named *Service*, you can do so by
adding

     containerBuilder.RegisterType<Service>().As<IService>();

to DependencyResolver.cs.
