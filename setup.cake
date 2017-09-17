#load nuget:https://www.myget.org/F/cake-contrib/api/v2?package=Cake.Recipe&prerelease

Environment.SetVariableNames();

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./src",
                            title: "Cake.Bakery",
                            repositoryOwner: "cake-build",
                            repositoryName: "bakery",
                            appVeyorAccountName: "cakebuild",
                            shouldRunDotNetCorePack: true,
                            shouldRunDupFinder: false,
                            shouldRunCodecov: false,
                            nugetConfig: "./src/NuGet.Config");

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(context: Context,
                            dupFinderExcludePattern: new string[] {
                                BuildParameters.RootDirectoryPath + "/src/*Tests/*.cs" },
                            testCoverageFilter: "+[*]* -[xunit.*]* -[Cake.Core]* -[Cake.Testing]* -[*.Tests]*",
                            testCoverageExcludeByAttribute: "*.ExcludeFromCodeCoverage*",
                            testCoverageExcludeByFile: "*/*Designer.cs;*/*.g.cs;*/*.g.i.cs");

var net46ArtifactPath = BuildParameters.Paths.Directories.PublishedApplications.Combine("Cake.Bakery/net46");

Task("Copy-License")
    .IsDependentOn("DotNetCore-Build")
    .WithCriteria(() => BuildParameters.ShouldRunDotNetCorePack)
    .Does(() =>
{
    // Copy license
    CopyFileToDirectory("./LICENSE", net46ArtifactPath);
});

// Override default Pack task
BuildParameters.Tasks.DotNetCorePackTask.Task.Actions.Clear();
BuildParameters.Tasks.DotNetCorePackTask
    .IsDependentOn("Copy-License")
    .Does(() =>
{
    var projects = GetFiles(BuildParameters.SourceDirectoryPath + "/**/*.csproj");
    foreach(var project in projects)
    {
        var name = project.GetDirectory().FullPath;
        if(name.EndsWith("Cake.Bakery") || name.EndsWith("Tests") || name.EndsWith("Cake.Scripting"))
        {
            continue;
        }

        DotNetCorePack(project.FullPath, new DotNetCorePackSettings {
            NoBuild = true,
            Configuration = BuildParameters.Configuration,
            OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
            ArgumentCustomization = args => args
                .Append("/p:Version={0}", BuildParameters.Version.SemVersion)
                .Append("/p:AssemblyVersion={0}", BuildParameters.Version.Version)
                .Append("/p:FileVersion={0}", BuildParameters.Version.Version)
                .Append("/p:AssemblyInformationalVersion={0}", BuildParameters.Version.InformationalVersion)
        });
    }

    var net46FullArtifactPath = MakeAbsolute(net46ArtifactPath).FullPath;
    var net46FullArtifactPathLength = net46FullArtifactPath.Length+1;

    // Cake.Bakery - .NET 4.6
    NuGetPack("./nuspec/Cake.Bakery.nuspec", new NuGetPackSettings {
        Version = BuildParameters.Version.SemVersion,
        //ReleaseNotes = TODO,
        BasePath = net46ArtifactPath,
        OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
        Symbols = false,
        NoPackageAnalysis = true,
        Files = GetFiles(net46FullArtifactPath + "/**/*")
                                .Select(file=>file.FullPath.Substring(net46FullArtifactPathLength))
                                .Select(file=> !file.Equals("LICENSE") ? 
                                    new NuSpecContent { Source = file, Target = "tools/" + file } :
                                    new NuSpecContent { Source = file, Target = file })
                                .ToArray()
    });
});

Task("Init-Integration-Tests")
    .IsDependentOn("Package")
    .Does(() =>
{
    CleanDirectories(new [] {
        "./tests/integration/packages",
        "./tests/integration/tools"
    });

    CopyFiles(MakeAbsolute(BuildParameters.Paths.Directories.NuGetPackages).FullPath + "/*.nupkg",
        "./tests/integration/packages");
});

Task("Run-Integration-Tests")
    .IsDependentOn("Init-Integration-Tests")
    .Does(() =>
{
    CakeExecuteScript("./tests/integration/tests.cake", new CakeSettings {
        Verbosity = Context.Log.Verbosity,
        WorkingDirectory = "./tests/integration/",
        Arguments = new Dictionary<string, string> {
            { "NuGet_Source", MakeAbsolute(new DirectoryPath("./tests/integration/packages")).FullPath }
        }
    });
});

// Hook up integration tests to default and appveyor tasks
BuildParameters.Tasks.DefaultTask.IsDependentOn("Run-Integration-Tests");
BuildParameters.Tasks.AppVeyorTask.IsDependentOn("Run-Integration-Tests");

Build.RunDotNetCore();
