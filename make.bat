:: enable some features
SET dism=%WINDIR%\SysNative\dism.exe
%dism% /online /Enable-Feature /FeatureName:IIS-WebServer /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:IIS-WebSockets /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:Application-Server-WebServer-Support /FeatureName:AS-NET-Framework /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:IIS-HostableWebCore /All /NoRestart

rmdir /S /Q packages
bin\nuget restore || exit /b 1
MSBuild Containerizer\Containerizer.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
MSBuild Containerizer.Tests\Containerizer.Tests.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
packages\nspec.0.9.68\tools\NSpecRunner.exe Containerizer.Tests\bin\Release\Containerizer.Tests.dll || exit /b 1

