:: msbuild must be in path
SET DEVENV_PATH=%programfiles(x86)%\Microsoft Visual Studio 12.0\Common7\IDE
SET PATH=%PATH%;%DEVENV_PATH%;%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
where msbuild
if errorLevel 1 ( echo "msbuild was not found on PATH" && exit /b 1 )

pushd IronFrame || exit /b 1
  ..\bin\nuget restore || exit /b 1
  devenv IronFrame.sln /build "Release" || exit /b 1
  :: call build.bat build || exit /b 1
popd

rmdir /S /Q packages
bin\nuget restore || exit /b 1
MSBuild IronFrame\Guard\Guard.vcxproj /t:Rebuild /p:Platform=x64 /p:Configuration=Release
MSBuild Containerizer\Containerizer.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
MSBuild Containerizer.Tests\Containerizer.Tests.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
packages\nspec.0.9.68\tools\NSpecRunner.exe Containerizer.Tests\bin\Release\Containerizer.Tests.dll || exit /b 1

robocopy Containerizer\bin\ ..\output\ ^& IF %ERRORLEVEL% LEQ 1 exit 0
