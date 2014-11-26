containerizer
=============

A containerization solution for Windows

setup
=====
1) Install Visual Studio 2013 (tested under professional).

2) Install http://msysgit.github.io/ for git on windows.

3) Install IIS: http://www.howtogeek.com/112455/how-to-install-iis-8-on-windows-8/ . Be sure to select ASP.NET and friends or the tests will fail with an unhelpful error message.

![git bash](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/iis_options.png)

4) Open Git Bash

![git bash](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/git_bash.png)

5) Clone this repository

![cloning](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/cloning.png)
tests

6) Install https://visualstudiogallery.msdn.microsoft.com/7a52473f-9e1a-40f3-8bd8-6c00ab163609

7) Open Visual Studio in Administrator mode

![opening as admin](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_as_admin.png)

![visual studio running as admin](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/showing_vs_running_as_admin.png)

8) Open containerizer solution

![open project](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_project.png)

![open details](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_details.png)

![open solution](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_solution.png)


advanced debugging
==================

The acceptance tests spin up containerizer in IIS out of process and communicate with it over HTTP. Unfortunately, this means it's more difficult to debug the server. To debug:

1) Have the server running (i.e. stop execution after starting an acceptance test, but before letting it be killed at the end of the test). To be sure that the server is correctly running, you should allow at least one request to hit the server, or IIS might not spin it up right away.

![open tests with breakpoint](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/open_tests_with_breakpoint.png)

2) Open a new instance of Visual Studio as Administrator and open the Containerizer solution. Then, go in the debug menu and select "Attach to Process"

![attach process menu](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/attach_to_process_menu.png)

3) Attach to the w3wp.exe process in the available processes. Make sure that you click "Show processes from all users".

![attach process](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/attach_process.png)

4) You can now hit debug points in the server!

![debugging in process](https://github.com/pivotal-cf-experimental/containerizer/blob/master/README_images/debugging_in_process.png)


quick tips
==========

If all of the acceptance tests start breaking mysteriously and you recently created a new service, you probably forgot to add the service to the DI container. Assuming you added a service named *Service*, you can do so by adding

     containerBuilder.RegisterType<Service>().As<IService>();

to DependencyResolver.cs.

tests
=====

