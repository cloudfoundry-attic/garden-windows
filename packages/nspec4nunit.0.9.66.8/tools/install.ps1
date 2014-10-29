param($installPath, $toolsPath, $package, $project)

# See http://nuget.codeplex.com/discussions/254095 and
# http://msdn.microsoft.com/en-us/library/microsoft.build.construction.projectelement.containingproject.aspx
function AddReferenceToProject ($msbuild, $assemblyName, $dllRef, $itemGroup)
{
    $item = $itemGroup.AddItem("Reference", $assemblyName)
    $hintPath = $msbuild.Xml.CreateMetadataElement("HintPath", $dllRef)
    #$msbuild.Xml.RemoveChild($hintPath)
    $item.AppendChild($hintPath)	
}

# Set the location to the current project's folder, so our relative paths are correct
Set-Location ([System.IO.Path]::GetDirectoryName($project.FullName))

# Get the relative path to the "packages" folder
$packagesPath = Get-Item (Get-Item $installPath).Parent.FullName | Resolve-Path -Relative

#
# Manually add the .dll reference for NUnit to the project,  
# since these dlls aren't explicitly referenced when adding 
# the NUnit.Runners package.
#
if (Test-Path $packagesPath)
{
    # Get the path to the NUnit.Runners lib folder
    $nunitRunnersPath = Join-Path (Join-Path (Join-Path $packagesPath (Get-ChildItem -Path $packagesPath -Filter "nunit.runners*" | select -expand Name)) "tools") "lib"

    # Get a path to each dll we'd like to add a reference to
    $nunitCoreDll = Join-Path $nunitRunnersPath "nunit.core.dll"
    $nunitInterfacesDll = Join-Path $nunitRunnersPath "nunit.core.interfaces.dll"
    $nunitGuiRunnerDll = Join-Path $nunitRunnersPath "nunit-gui-runner.dll"

    # Load the XML data via the Microsoft.Build.Evaluation workspace
    [System.Reflection.Assembly]::LoadWithPartialName("Microsoft.Build") | out-null
    $msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

    # Add an item group to the project file
    $itemGroup = $msbuild.Xml.AddItemGroup()

    # Add each NUnit reference to this item group
    AddReferenceToProject -msbuild $msbuild -assemblyName "nunit.core" -dllRef $nunitCoreDll -itemGroup $itemGroup
    AddReferenceToProject -msbuild $msbuild -assemblyName "nunit.core.interfaces" -dllRef $nunitInterfacesDll -itemGroup $itemGroup
    AddReferenceToProject -msbuild $msbuild -assemblyName "nunit-gui-runner" -dllRef $nunitGuiRunnerDll -itemGroup $itemGroup

    # Set the NUnit project file to "Copy If Newer"
    # See http://stackoverflow.com/questions/8474253/nuget-how-can-i-change-property-of-files-with-install-ps1-file
    $file1 = $project.ProjectItems.Item("Test.nunit")
    $copyToOutput1 = $file1.Properties.Item("CopyToOutputDirectory")
    $copyToOutput1.Value = 2

    # Save the MSBuild project
    $msbuild.save();

    # Save the VS project when finished
    $project.Save()
}