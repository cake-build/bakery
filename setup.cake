#load nuget:https://www.myget.org/F/cake-contrib/api/v3/index.json?package=Cake.Recipe&version=0.3.0-unstable0280&prerelease
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

Task("Copy-License")
    .IsDependentOn("DotNetCore-Build")
    .WithCriteria(() => BuildParameters.ShouldRunDotNetCorePack)
    .Does(() =>
{
    // Copy license
    CopyFileToDirectory("./LICENSE", binArtifactPath);
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
    .WithCriteria(() => BuildParameters.ShouldPublishNuGet)
    .Does(() =>
{
    // Get the secret.
    var secret = EnvironmentVariable("SIGNING_SECRET");
    if(string.IsNullOrWhiteSpace(secret)) {
        throw new InvalidOperationException("Could not resolve signing secret.");
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
    var files = GetFiles(string.Concat(BuildParameters.Paths.Directories.NuGetPackages, "/", "*.nupkg"));

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
            .AppendSwitchQuoted("-h", "dual")
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

// Hook up integration tests to default and appveyor tasks
BuildParameters.Tasks.DefaultTask.IsDependentOn("Run-Bakery-Integration-Tests");
BuildParameters.Tasks.AppVeyorTask.IsDependentOn("Run-Bakery-Integration-Tests");

// Hook up signing task to publish tasks
BuildParameters.Tasks.PublishNuGetPackagesTask.IsDependentOn("Sign-Binaries");
BuildParameters.Tasks.UploadAppVeyorArtifactsTask.IsDependentOn("Run-Bakery-Integration-Tests")
                                                 .IsDependentOn("Sign-Binaries");

Build.RunDotNetCore();
