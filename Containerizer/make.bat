:: msbuild must be in path
SET PATH=%PATH%;%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
where msbuild
if errorLevel 1 ( echo "msbuild was not found on PATH" && exit /b 1 )

git submodule update --init --recursive
cd IronFrame
call build.bat build || exit /b 1
cd ..

rmdir /S /Q packages
bin\nuget restore || exit /b 1
MSBuild Containerizer\Containerizer.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
MSBuild Containerizer.Tests\Containerizer.Tests.csproj /t:Rebuild /p:Configuration=Release || exit /b 1
packages\nspec.0.9.68\tools\NSpecRunner.exe Containerizer.Tests\bin\Release\Containerizer.Tests.dll || exit /b 1
