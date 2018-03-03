#load nuget:https://www.myget.org/F/cake-contrib/api/v3/index.json?package=Cake.Recipe&version=0.3.0-unstable0342&prerelease
#tool nuget:https://api.nuget.org/v3/index.json?package=SignClient&version=0.9.0

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

var binArtifactPath = BuildParameters.Paths.Directories.PublishedApplications.Combine("Cake.Bakery/net461");
var zipArtifactsPath = BuildParameters.Paths.Directories.Build.Combine("Packages/Zip");

Task("Copy-License")
    .IsDependentOn("DotNetCore-Build")
    .WithCriteria(() => BuildParameters.ShouldRunDotNetCorePack)
    .Does(() =>
{
    // Copy license
    CopyFileToDirectory("./LICENSE", binArtifactPath);
});

Task("Zip-Files")
    .IsDependentOn("Copy-License")
    .IsDependeeOf("Package")
    .Does(() =>
{
    CleanDirectory(zipArtifactsPath);
    Zip(binArtifactPath, zipArtifactsPath.CombineWithFilePath($"Cake.Bakery.{BuildParameters.Version.SemVersion}.zip"));
});

Task("Upload-AppVeyor-Artifacts-Zip")
    .IsDependentOn("Package")
    .IsDependeeOf("Upload-AppVeyor-Artifacts")
    .WithCriteria(() => BuildParameters.IsRunningOnAppVeyor)
    .Does(() =>
{
    foreach(var package in GetFiles(zipArtifactsPath + "/*"))
    {
        AppVeyor.UploadArtifact(package);
    }
});

Task("Publish-GitHub-Release-Zip")
    .IsDependentOn("Package")
    .IsDependentOn("Zip-Files")
    .IsDependeeOf("Publish-GitHub-Release")
    .WithCriteria(() => BuildParameters.ShouldPublishGitHub)
    .Does(() => RequireTool(GitReleaseManagerTool, () => {
        if(BuildParameters.CanUseGitReleaseManager)
        {
            foreach(var package in GetFiles(zipArtifactsPath + "/*"))
            {
                GitReleaseManagerAddAssets(BuildParameters.GitHub.UserName, BuildParameters.GitHub.Password, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, BuildParameters.Version.Milestone, package.ToString());
            }
        }
        else
        {
            Warning("Unable to use GitReleaseManager, as necessary credentials are not available");
        }
    })
)
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
    publishingError = true;
});

// Override default Build task
BuildParameters.Tasks.DotNetCoreBuildTask.Task.Actions.Clear();
BuildParameters.Tasks.DotNetCoreBuildTask
    .Does(() =>
{
    Information("Building {0}", BuildParameters.SolutionFilePath);

    var msBuildSettings = new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", BuildParameters.Version.SemVersion)
                                .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                                .WithProperty("FileVersion",  BuildParameters.Version.Version)
                                .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

    if(!IsRunningOnWindows())
    {
        var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

        // Use FrameworkPathOverride when not running on Windows.
        Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
        msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
    }

    DotNetCoreBuild(BuildParameters.SolutionFilePath.FullPath, new DotNetCoreBuildSettings
    {
        Configuration = BuildParameters.Configuration,
        MSBuildSettings = msBuildSettings
    });

    if(BuildParameters.ShouldExecuteGitLink)
    {
        ExecuteGitLink();
    }

    Information("Copying build output...");

    foreach(var project in ParseSolution(BuildParameters.SolutionFilePath).GetProjects())
    {
        // There is quite a bit of duplication in this function, that really needs to be tidied Upload
        Information("Input BuildPlatformTarget: {0}", ToolSettings.BuildPlatformTarget.ToString());
        var platformTarget = ToolSettings.BuildPlatformTarget == PlatformTarget.MSIL ? "AnyCPU" : ToolSettings.BuildPlatformTarget.ToString();
        Information("Using BuildPlatformTarget: {0}", platformTarget);
        var parsedProject = ParseProject(project.Path, BuildParameters.Configuration, platformTarget);

        if(project.Path.FullPath.ToLower().Contains("wixproj"))
        {
            Warning("Skipping wix project");
            continue;
        }

        if(project.Path.FullPath.ToLower().Contains("shproj"))
        {
            Warning("Skipping shared project");
            continue;
        }

        if(parsedProject.OutputPath == null || parsedProject.RootNameSpace == null || parsedProject.OutputType == null)
        {
            Information("OutputPath: {0}", parsedProject.OutputPath);
            Information("RootNameSpace: {0}", parsedProject.RootNameSpace);
            Information("OutputType: {0}", parsedProject.OutputType);
            throw new Exception(string.Format("Unable to parse project file correctly: {0}", project.Path));
        }

        // If the project is an exe, then simply copy all of the contents to the correct output folder
        if(!parsedProject.IsLibrary())
        {
            Information("Project has an output type of exe: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedApplications.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);

            // If .NET Core project, copy using dotnet publish for each target framework
            // Otherwise just copy
            if(parsedProject.IsNetCore)
            {
                foreach(var targetFramework in parsedProject.NetCore.TargetFrameworks)
                {
                    DotNetCorePublish(project.Path.FullPath, new DotNetCorePublishSettings {
                        OutputDirectory = outputFolder.Combine(targetFramework),
                        Framework = targetFramework,
                        Configuration = BuildParameters.Configuration,
                        MSBuildSettings = msBuildSettings
                    });
                }
            }
            else
            {
                CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            }

            continue;
        }

        var isxUnitTestProject = false;
        var ismsTestProject = false;
        var isFixieProject = false;
        var isNUnitProject = false;

        // Now we need to test for whether this is a unit test project.  Currently, this is only testing for XUnit Projects.
        // It needs to be extended to include others, i.e. NUnit, MSTest, and VSTest
        // If this is found, move the output to the unit test folder, otherwise, simply copy to normal output folder

        ICollection<ProjectAssemblyReference> references = null;
        if(!BuildParameters.IsDotNetCoreBuild)
        {
            Information("Not a .Net Core Build");
            references = parsedProject.References;
        }
        else
        {
            Information("Is a .Net Core Build");
            references = new List<ProjectAssemblyReference>();
        }

        foreach(var reference in references)
        {
            Verbose("Reference Include: {0}", reference.Include);
            if(reference.Include.ToLower().Contains("xunit.core"))
            {
                isxUnitTestProject = true;
                break;
            }
            else if(reference.Include.ToLower().Contains("unittestframework"))
            {
                ismsTestProject = true;
                break;
            }
            else if(reference.Include.ToLower().Contains("fixie"))
            {
                isFixieProject = true;
                break;
            }
            else if(reference.Include.ToLower().Contains("nunit.framework"))
            {
                isNUnitProject = true;;
                break;
            }
        }

        if(parsedProject.IsLibrary() && isxUnitTestProject)
        {
            Information("Project has an output type of library and is an xUnit Test Project: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedxUnitTests.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else if(parsedProject.IsLibrary() && ismsTestProject)
        {
            // We will use vstest.console.exe by default for MSTest Projects
            Information("Project has an output type of library and is an MSTest Project: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedVSTestTests.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else if(parsedProject.IsLibrary() && isFixieProject)
        {
            Information("Project has an output type of library and is a Fixie Project: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedFixieTests.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else if(parsedProject.IsLibrary() && isNUnitProject)
        {
            Information("Project has an output type of library and is a NUnit Test Project: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedNUnitTests.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
        else
        {
            Information("Project has an output type of library: {0}", parsedProject.RootNameSpace);
            var outputFolder = BuildParameters.Paths.Directories.PublishedLibraries.Combine(parsedProject.RootNameSpace);
            EnsureDirectoryExists(outputFolder);
            CopyFiles(GetFiles(parsedProject.OutputPath.FullPath + "/**/*"), outputFolder, true);
            continue;
        }
    }
});

// Override default Pack task
BuildParameters.Tasks.DotNetCorePackTask.Task.Actions.Clear();
BuildParameters.Tasks.DotNetCorePackTask
    .IsDependentOn("Copy-License")
    .Does(() =>
{
    var msBuildSettings = new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", BuildParameters.Version.SemVersion)
                                .WithProperty("AssemblyVersion", BuildParameters.Version.Version)
                                .WithProperty("FileVersion",  BuildParameters.Version.Version)
                                .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion);

    if(!IsRunningOnWindows())
    {
        var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

        // Use FrameworkPathOverride when not running on Windows.
        Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
        msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
    }

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
            MSBuildSettings = msBuildSettings
        });
    }

    var binFullArtifactPath = MakeAbsolute(binArtifactPath).FullPath;
    var binFullArtifactPathLength = binFullArtifactPath.Length+1;

    // Cake.Bakery - .NET 4.6
    NuGetPack("./nuspec/Cake.Bakery.nuspec", new NuGetPackSettings {
        Version = BuildParameters.Version.SemVersion,
        //ReleaseNotes = TODO,
        BasePath = binArtifactPath,
        OutputDirectory = BuildParameters.Paths.Directories.NuGetPackages,
        Symbols = false,
        NoPackageAnalysis = true,
        Files = GetFiles(binFullArtifactPath + "/**/*")
                                .Select(file=>file.FullPath.Substring(binFullArtifactPathLength))
                                .Select(file=> !file.Equals("LICENSE") ? 
                                    new NuSpecContent { Source = file, Target = "tools/" + file } :
                                    new NuSpecContent { Source = file, Target = file })
                                .ToArray()
    });
});

Task("Init-Integration-Tests")
    .IsDependentOn("Sign-Binaries")
    .Does(() =>
{
    CleanDirectories(new [] {
        "./tests/integration/packages",
        "./tests/integration/tools"
    });

    CopyFiles(MakeAbsolute(BuildParameters.Paths.Directories.NuGetPackages).FullPath + "/*.nupkg",
        "./tests/integration/packages");
});

Task("Run-Bakery-Integration-Tests")
    .IsDependentOn("Init-Integration-Tests")
    .IsDependeeOf("AppVeyor")
    .IsDependeeOf("Default")
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

Task("Sign-Binaries")
    .IsDependentOn("Package")
    .IsDependeeOf("Upload-AppVeyor-Artifacts-Zip")
    .IsDependeeOf("Upload-AppVeyor-Artifacts")
    .IsDependeeOf("Publish-MyGet-Packages")
    .IsDependeeOf("Publish-Nuget-Packages")
    .IsDependeeOf("Publish-GitHub-Release-Zip")
    .IsDependeeOf("Publish-GitHub-Release")
    .WithCriteria(() => BuildParameters.ShouldPublishNuGet ||
        string.Equals(EnvironmentVariable("SIGNING_TEST"), "true", StringComparison.OrdinalIgnoreCase))
    .Does(() =>
{
    // Get the secret.
    var secret = EnvironmentVariable("SIGNING_SECRET");
    if(string.IsNullOrWhiteSpace(secret)) {
        throw new InvalidOperationException("Could not resolve signing secret.");
    }

    // Get the user.
    var user = EnvironmentVariable("SIGNING_USER");
    if(string.IsNullOrWhiteSpace(user)) {
        throw new InvalidOperationException("Could not resolve signing user.");
    }

    // Resolve dotnet and version
    var dotnetPath = Context.Tools.Resolve("dotnet.exe");
    if(dotnetPath == null) {
        throw new InvalidOperationException("Could not resolve dotnet.");
    }

    // Resolve dotnet version
    StartProcess(dotnetPath, new ProcessSettings {
        Arguments = "--version",
        RedirectStandardOutput = true
    }, out var versionOutput);
    var dotnetVersion = versionOutput.First().Substring(0,3);

    var client = File($"./tools/SignClient.0.9.0/tools/netcoreapp{dotnetVersion}/SignClient.dll");
    var settings = File("./signclient.json");
    var filter = File("./signclient.filter");

    // Get the files to sign.
    var files = GetFiles(string.Concat(BuildParameters.Paths.Directories.NuGetPackages, "/", "*.nupkg")) +
                GetFiles(string.Concat(zipArtifactsPath, "/", "*.zip"));

    foreach(var file in files)
    {
        Information("Signing {0}...", file.FullPath);

        // Build the argument list.
        var arguments = new ProcessArgumentBuilder()
            .AppendQuoted(MakeAbsolute(client.Path).FullPath)
            .Append("sign")
            .AppendSwitchQuoted("-c", MakeAbsolute(settings.Path).FullPath)
            .AppendSwitchQuoted("-i", MakeAbsolute(file).FullPath)
            .AppendSwitchQuoted("-f", MakeAbsolute(filter).FullPath)
            .AppendSwitchQuotedSecret("-s", secret)
            .AppendSwitchQuotedSecret("-r", user)
            .AppendSwitchQuoted("-n", "Cake")
            .AppendSwitchQuoted("-d", "Cake (C# Make) is a cross platform build automation system.")
            .AppendSwitchQuoted("-u", "https://cakebuild.net");

        // Sign the binary.
        var result = StartProcess(dotnetPath, new ProcessSettings {  Arguments = arguments });
        if(result != 0)
        {
            // We should not recover from this.
            throw new InvalidOperationException("Signing failed!");
        }
    }
});

Build.RunDotNetCore();
