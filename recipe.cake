#load nuget:?package=Cake.Recipe&version=2.2.0
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
var omnisharpBaseDownloadURL = "https://omnisharpdownload.blob.core.windows.net/ext";
var omnisharpMonoRuntimeMacOS = $"{omnisharpBaseDownloadURL}/mono.macOS-5.12.0.301.zip";
var omnisharpMonoRuntimeLinux32= $"{omnisharpBaseDownloadURL}/mono.linux-x86-5.12.0.301.zip";
var omnisharpMonoRuntimeLinux64= $"{omnisharpBaseDownloadURL}/mono.linux-x86_64-5.12.0.301.zip";

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
    .Does<BuildVersion>((context, buildVersion) =>
{
    CleanDirectory(zipArtifactsPath);
    Zip(binArtifactPath, zipArtifactsPath.CombineWithFilePath($"Cake.Bakery.{buildVersion.SemVersion}.zip"));
});

Task("Upload-AppVeyor-Artifacts-Zip")
    .IsDependentOn("Package")
    .IsDependeeOf("Upload-Artifacts")
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
    .Does<BuildVersion>((context, buildVersion) => RequireTool(BuildParameters.IsDotNetCoreBuild ? ToolSettings.GitReleaseManagerGlobalTool : ToolSettings.GitReleaseManagerTool, () => {
        if(BuildParameters.CanUseGitReleaseManager)
        {
            foreach(var package in GetFiles(zipArtifactsPath + "/*"))
            {
                GitReleaseManagerAddAssets(BuildParameters.GitHub.Token, BuildParameters.RepositoryOwner, BuildParameters.RepositoryName, buildVersion.Milestone, package.ToString());
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

// Override default Build and Restore tasks
(BuildParameters.Tasks.DotNetCoreRestoreTask.Task as CakeTask).Actions.Clear();
(BuildParameters.Tasks.DotNetCoreBuildTask.Task as CakeTask).Actions.Clear();
BuildParameters.Tasks.DotNetCoreBuildTask
    .Does<BuildVersion>((context, buildVersion) =>
{
    Information("Building {0}", BuildParameters.SolutionFilePath);

    MSBuild(BuildParameters.SolutionFilePath.FullPath, new MSBuildSettings {
        Configuration = BuildParameters.Configuration,
        Restore = true,
        Properties = {
            ["Version"] = new[] { buildVersion.SemVersion },
            ["AssemblyVersion"] = new[] { buildVersion.Version },
            ["FileVersion"] = new[] { buildVersion.Version },
            ["AssemblyInformationalVersion"] = new[] { buildVersion.InformationalVersion },
        }
    });

    CleanDirectory(binArtifactPath);
    CopyFiles(GetFiles($"./src/Cake.Bakery/bin/{BuildParameters.Configuration}/net461/**/*"), binArtifactPath, true);
});

// Override default Pack task
(BuildParameters.Tasks.DotNetCorePackTask.Task as CakeTask).Actions.Clear();
BuildParameters.Tasks.DotNetCorePackTask
    .IsDependentOn("Copy-License")
    .Does<BuildVersion>((context, buildVersion) =>
{
    var msBuildSettings = new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", buildVersion.SemVersion)
                                .WithProperty("AssemblyVersion", buildVersion.Version)
                                .WithProperty("FileVersion",  buildVersion.Version)
                                .WithProperty("AssemblyInformationalVersion", buildVersion.InformationalVersion);

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
        Version = buildVersion.SemVersion,
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
        "./tests/integration/tools",
        "./tests/integration/bin"
    });

    NuGetInstall(new[] { "Cake.Bakery", "Cake" }, new NuGetInstallSettings {
        ExcludeVersion = true,
        OutputDirectory = new DirectoryPath("./tests/integration/tools"),
        DisableParallelProcessing = true,
        Prerelease = true,
        Source = new[] { MakeAbsolute(BuildParameters.Paths.Directories.NuGetPackages).FullPath },
        FallbackSource = new[] { "https://api.nuget.org/v3/index.json" }
    });

    MSBuild("./tests/integration/integration.csproj", new MSBuildSettings {
        Configuration = BuildParameters.Configuration,
        Restore = true,
        Properties = {
            ["OutputPath"] = new[] { "./bin/" }
        }
    });
});

Task("Download-Mono-Assets")
    .WithCriteria(() => !IsRunningOnWindows())
    .Does(() =>
{
    CleanDirectory("./tests/integration/mono");

    string downloadUrl = null;
    switch(Context.Environment.Platform.Family)
    {
        case PlatformFamily.OSX:
            downloadUrl = omnisharpMonoRuntimeMacOS;
            break;
        case PlatformFamily.Linux:
            downloadUrl = Context.Environment.Platform.Is64Bit ?
                omnisharpMonoRuntimeLinux64 :
                omnisharpMonoRuntimeLinux32;
            break;
        default:
            break;
    }

    if (string.IsNullOrEmpty(downloadUrl))
    {
        return;
    }

    var zipFile = DownloadFile(downloadUrl);
    Unzip(zipFile, "./tests/integration/mono");

    StartProcess("chmod", "u+x ./tests/integration/mono/run");
    StartProcess("chmod", "u+x ./tests/integration/mono/bin/mono");
});

Task("Run-Bakery-Integration-Tests")
    .IsDependentOn("Init-Integration-Tests")
    .IsDependentOn("Download-Mono-Assets")
    .IsDependeeOf("CI")
    .IsDependeeOf("Default")
    .Does(() =>
{
    // If not running on Windows, run with OmniSharp Mono and Mono.
    if (!IsRunningOnWindows())
    {
        var exitCode = StartProcess("mono", new ProcessSettings {
            Arguments = MakeAbsolute(new FilePath("./tests/integration/bin/integration.exe")).FullPath + " " +
                MakeAbsolute(new FilePath("./tests/integration/tools/Cake.Bakery/tools/Cake.Bakery.exe")).FullPath,
            WorkingDirectory = new DirectoryPath("./tests/integration")
        });

        if (exitCode != 0)
        {
            throw new Exception("Mono integration tests failed.");
        }

        exitCode = StartProcess("./tests/integration/mono/run", new ProcessSettings {
            Arguments = "--no-omnisharp " +
                MakeAbsolute(new FilePath("./tests/integration/bin/integration.exe")).FullPath + " " +
                MakeAbsolute(new FilePath("./tests/integration/tools/Cake.Bakery/tools/Cake.Bakery.exe")).FullPath,
            WorkingDirectory = new DirectoryPath("./tests/integration")
        });

        if (exitCode != 0)
        {
            throw new Exception("OmniSharp Mono integration tests failed.");
        }
    }
    else
    {
        var exitCode = StartProcess("./tests/integration/bin/integration.exe", new ProcessSettings {
            Arguments = MakeAbsolute(new FilePath("./tests/integration/tools/Cake.Bakery/tools/Cake.Bakery.exe")).FullPath,
            WorkingDirectory = new DirectoryPath("./tests/integration")
        });

        if (exitCode != 0)
        {
            throw new Exception(".NET Framework integration tests failed.");
        }
    }
});

var shouldDeployBakery = (!BuildParameters.IsLocalBuild || BuildParameters.ForceContinuousIntegration) &&
                        BuildParameters.IsTagged &&
                        BuildParameters.PreferredBuildAgentOperatingSystem == BuildParameters.BuildAgentOperatingSystem &&
                        BuildParameters.PreferredBuildProviderType == BuildParameters.BuildProvider.Type;
Task("Sign-Binaries")
    .IsDependentOn("Package")
    .IsDependeeOf("Upload-AppVeyor-Artifacts-Zip")
    .IsDependeeOf("Upload-Artifacts")
    .IsDependeeOf("Publish-PreRelease-Packages")
    .IsDependeeOf("Publish-Release-Packages")
    .IsDependeeOf("Publish-GitHub-Release-Zip")
    .IsDependeeOf("Publish-GitHub-Release")
    .WithCriteria(() => shouldDeployBakery || string.Equals(EnvironmentVariable("SIGNING_TEST"), "true", StringComparison.OrdinalIgnoreCase))
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

    var client = File($"./tools/SignClient.0.9.0/tools/netcoreapp2.0/SignClient.dll");
    var settings = File("./signclient.json");
    var filter = File("./signclient.filter");

    // Get the files to sign.
    var files = GetFiles(string.Concat(BuildParameters.Paths.Directories.NuGetPackages, "/", "*.nupkg")) +
                GetFiles(string.Concat(BuildParameters.Paths.Directories.ChocolateyPackages , "/", "*.nupkg")) +
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
