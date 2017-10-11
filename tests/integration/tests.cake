// Tools
#tool "nuget:?package=Cake.Bakery&prerelease"
#tool "nuget:https://api.nuget.org/v3/index.json?package=Cake&version=0.23.0"

// Addins
#addin "nuget:https://api.nuget.org/v3/index.json?package=xunit.assert&version=2.2.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.Logging&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.Logging.Console&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.Logging.Abstractions&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Microsoft.Extensions.Configuration.Abstractions&version=1.1.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=System.Runtime.InteropServices.RuntimeInformation&version=4.0.0"
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
        .AddConsole(Microsoft.Extensions.Logging.LogLevel.Information);

    service = new ScriptGenerationClient(
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