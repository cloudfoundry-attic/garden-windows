:: Visual Studio must be in path

where devenv
if errorLevel 1 ( echo "devenv was not found on PATH" && exit /b 1 )
 
:: enable some features
SET dism=%WINDIR%\SysNative\dism.exe
%dism% /online /Enable-Feature /FeatureName:IIS-WebServer /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:IIS-WebSockets /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:Application-Server-WebServer-Support /FeatureName:AS-NET-Framework /All /NoRestart
%dism% /online /Enable-Feature /FeatureName:IIS-HostableWebCore /All /NoRestart

rmdir /S /Q packages
bin\nuget restore || exit /b 1
devenv Containerizer\Containerizer.csproj /build "Release" || exit /b 1
devenv Containerizer.Tests\Containerizer.Tests.csproj /build "Release" || exit /b 1
packages\nspec.0.9.68\tools\NSpecRunner.exe Containerizer.Tests\bin\Release\Containerizer.Tests.dll || exit /b 1

