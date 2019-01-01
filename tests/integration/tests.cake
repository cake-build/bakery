// Tools
#tool "nuget:?package=Cake.Bakery&prerelease"
#tool "nuget:https://api.nuget.org/v3/index.json?package=Cake&version=0.31.0"

// Addins
#addin "nuget:https://api.nuget.org/v3/index.json?package=xunit.assert&version=2.2.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.Logging.Console&version=2.1.1&loaddependencies=true"
#addin "nuget:?package=Cake.Scripting.Abstractions&prerelease"
#addin "nuget:?package=Cake.Scripting.Transport&prerelease"

// Usings
using System.Diagnostics;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tcp.Client;
using Microsoft.Extensions.Logging;
using Xunit;

AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
    if (args.Name.StartsWith("System.Runtime.InteropServices.RuntimeInformation"))
    {
        return System.Reflection.Assembly.Load(new System.Reflection.AssemblyName("System.Runtime.InteropServices.RuntimeInformation, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
    }
    return null;
};

class MonoScriptGenerationProcess : IScriptGenerationProcess
{
    private readonly ILogger _logger;
    private Process _process;

    public MonoScriptGenerationProcess(string serverExecutablePath, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(MonoScriptGenerationProcess));
        ServerExecutablePath = serverExecutablePath;
    }

    public void Dispose()
    {
        _process?.Kill();
        _process?.WaitForExit();
        _process?.Dispose();
    }

    public void Start(int port, string workingDirectory)
    {
        var (fileName, arguments) = GetMonoRuntime();

        if (fileName == null)
        {
            // Something went wrong figurint out mono runtime,
            // try executing exe and let mono handle it.
            fileName = ServerExecutablePath;
        }
        else
        {
            // Else set exe as argument
            arguments += $"\"{ServerExecutablePath}\"";
        }

        arguments += $" --port={port}";
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            arguments += " --verbose";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        _logger.LogDebug("Starting \"{fileName}\" with arguments \"{arguments}\"", startInfo.FileName, startInfo.Arguments);
        _process = Process.Start(startInfo);
        _process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                _logger.LogError(e.Data);
            }
        };
        _process.BeginErrorReadLine();
        _process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                _logger.LogDebug(e.Data);
            }
        };
        _process.BeginOutputReadLine();
    }

    private (string, string) GetMonoRuntime()
    {
        // Check using ps how process was started.
        var startInfo = new ProcessStartInfo
        {
            FileName = "sh",
            Arguments = $"-c \"ps -fp {Process.GetCurrentProcess().Id} | tail -n1 | awk '{{print $8}}'\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        var process = Process.Start(startInfo);
        var runtime = process.StandardOutput.ReadToEnd().TrimEnd('\n');
        process.WaitForExit();

        // If OmniSharp bundled Mono runtime, use bootstrap script.
        var script = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(runtime), "../run");
        if (System.IO.File.Exists(script))
        {
            return (script, "--no-omnisharp ");
        }

        // Else use mono directly.
        return (runtime, string.Empty);
    }

    public string ServerExecutablePath { get; set; }
}

// Globals
IScriptGenerationService service;
const string CakeHelloWorldFile = "helloworld.cake";
const string CakeHelloWorldSrc =
@"var target = Argument(""target"", ""Default"");

Task(""Default"")
  .Does(() =>
{
  Information(""Hello World!"");
});

RunTarget(target);";
const string CakeHelloWorldModified =
@"var target = Argument(""target"", ""Foobar"");

Task(""Foobar"")
  .Does(() =>
{
  Verbose(""Hello World!"");
});

RunTarget(target);";
const string CakeAddinDirectiveFile = "addin.cake";

// Setup
Setup((context) => {
    var loggerFactory = new LoggerFactory()
        .AddConsole(Microsoft.Extensions.Logging.LogLevel.Debug);

    service = Context.IsRunningOnUnix() ? 
        new ScriptGenerationClient(
            new MonoScriptGenerationProcess(MakeAbsolute(context.Tools.Resolve("Cake.Bakery.exe")).FullPath, loggerFactory),
            MakeAbsolute(context.Environment.WorkingDirectory).FullPath,
            loggerFactory) :
        new ScriptGenerationClient(
            MakeAbsolute(context.Tools.Resolve("Cake.Bakery.exe")).FullPath,
            MakeAbsolute(context.Environment.WorkingDirectory).FullPath,
            loggerFactory);
});

Task("Should-Generate-From-File")
    .Does(() =>
{
    // Given
    var fileChange = new FileChange
    {
        FileName = CakeHelloWorldFile,
        FromDisk = true
    };

    // When
    var response = service.Generate(fileChange);

    // Then
    var cakeCorePath = Context.Tools.Resolve("cake.exe").GetDirectory().CombineWithFilePath("Cake.Core.dll");
    var lineDirective = $"#line 1 \"{MakeAbsolute(new FilePath(CakeHelloWorldFile)).FullPath}\"";
    var sourceLines = response.Source.Split('\n').ToList();
    var expectedLines = CakeHelloWorldSrc.Split('\n').ToList();

    Assert.True(PathComparer.Default.Equals(new FilePath(response.Host.AssemblyPath), cakeCorePath));
    Assert.Contains("Cake.Core", response.Usings);
    Assert.Contains(MakeAbsolute(cakeCorePath).FullPath, response.References, StringComparer.OrdinalIgnoreCase);
    Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

    var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
    for(var i = 0; i < expectedLines.Count; i++)
    {
        Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd('\r', '\n'));
    }
});

Task("Should-Generate-From-Buffer")
    .Does(() =>
{
    // Given
    var buffer = CakeHelloWorldSrc;
    var fileName = $"{Guid.NewGuid()}.cake";
    var fileChange = new FileChange
    {
        Buffer = buffer,
        FileName = fileName,
        FromDisk = false
    };

    // When
    var response = service.Generate(fileChange);

    // Then
    var cakeCorePath = Context.Tools.Resolve("cake.exe").GetDirectory().CombineWithFilePath("Cake.Core.dll");
    var lineDirective = $"#line 1 \"{MakeAbsolute(new FilePath(fileName)).FullPath}\"";
    var sourceLines = response.Source.Split('\n').ToList();
    var expectedLines = CakeHelloWorldSrc.Split('\n').ToList();

    Assert.True(PathComparer.Default.Equals(new FilePath(response.Host.AssemblyPath), cakeCorePath));
    Assert.Contains("Cake.Core", response.Usings);
    Assert.Contains(MakeAbsolute(cakeCorePath).FullPath, response.References, StringComparer.OrdinalIgnoreCase);
    Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

    var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
    for(var i = 0; i < expectedLines.Count; i++)
    {
        Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd('\r', '\n'));
    }
});

Task("Should-Generate-With-Line-Changes")
    .Does(() =>
{
    // Given
    var buffer = CakeHelloWorldSrc;
    var fileName = $"{Guid.NewGuid()}.cake";
    var fileChange = new FileChange
    {
        Buffer = buffer,
        FileName = fileName,
        FromDisk = false
    };
    service.Generate(fileChange);
    fileChange = new FileChange
    {
        LineChanges =
        {
            new LineChange
            {
                StartLine = 0,
                StartColumn = 33,
                EndLine = 0,
                EndColumn = 40,
                NewText = "Foobar"
            },
            new LineChange
            {
                StartLine = 2,
                StartColumn = 6,
                EndLine = 2,
                EndColumn = 13,
                NewText = "Foobar"
            },
            new LineChange
            {
                StartLine = 5,
                StartColumn = 2,
                EndLine = 5,
                EndColumn = 13,
                NewText = "Verbose"
            }
        },
        FileName = fileName,
        FromDisk = false
    };

    // When
    var response = service.Generate(fileChange);

    // Then
    var cakeCorePath = Context.Tools.Resolve("cake.exe").GetDirectory().CombineWithFilePath("Cake.Core.dll");
    var lineDirective = $"#line 1 \"{MakeAbsolute(new FilePath(fileName)).FullPath}\"";
    var sourceLines = response.Source.Split('\n').ToList();
    var expectedLines = CakeHelloWorldModified.Split('\n').ToList();

    Assert.True(PathComparer.Default.Equals(new FilePath(response.Host.AssemblyPath), cakeCorePath));
    Assert.Contains("Cake.Core", response.Usings);
    Assert.Contains(MakeAbsolute(cakeCorePath).FullPath, response.References, StringComparer.OrdinalIgnoreCase);
    Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

    var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
    for(var i = 0; i < expectedLines.Count; i++)
    {
        Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd('\r', '\n'));
    }
});

Task("Should-Install-Addins")
    .Does(() =>
{
    // Given
    var fileChange = new FileChange
    {
        FileName = CakeAddinDirectiveFile,
        FromDisk = true
    };

    // When
    var response = service.Generate(fileChange);

    // Then
    var addin = new FilePath("./tools/Addins/Cake.Wyam.0.18.6/lib/net45/Cake.Wyam.dll");

    Assert.Contains(MakeAbsolute(addin).FullPath, response.References);
});

Task("Default")
    .IsDependentOn("Should-Generate-From-File")
    .IsDependentOn("Should-Generate-From-Buffer")
    .IsDependentOn("Should-Generate-With-Line-Changes")
    .IsDependentOn("Should-Install-Addins");

RunTarget("Default");