using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cake.Scripting.Abstractions;
using Cake.Scripting.Abstractions.Models;
using Cake.Scripting.Transport.Tcp.Client;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Integration
{
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
            var tuple = GetMonoRuntime();
            var fileName = tuple.Item1;
            var arguments = tuple.Item2;

            if (fileName == null)
            {
                // Something went wrong figuring out mono runtime,
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

        private Tuple<string, string> GetMonoRuntime()
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
            var runtime = process.StandardOutput.ReadToEnd().TrimEnd(new[] { '\n' });
            process.WaitForExit();

            // If OmniSharp bundled Mono runtime, use bootstrap script.
            var script = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(runtime), "../run");
            if (System.IO.File.Exists(script))
            {
                return Tuple.Create(script, "--no-omnisharp ");
            }

            // Else use mono directly.
            return Tuple.Create(runtime, string.Empty);
        }

        public string ServerExecutablePath { get; set; }
    }

    class Program
    {
        static void Should_Generate_From_File()
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
            var cakeCorePath = $"{workingDirectory}/tools/Cake.Bakery/tools/Cake.Core.dll";
            var lineDirective = $"#line 1 \"{workingDirectory}/{CakeHelloWorldFile}\"";
            var sourceLines = response.Source.Split('\n').ToList();
            var expectedLines = CakeHelloWorldSrc.Split('\n').ToList();

            Assert.Equal(response.Host.AssemblyPath, cakeCorePath);
            Assert.Contains("Cake.Core", response.Usings);
            Assert.Contains(cakeCorePath, response.References, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

            var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
            for(var i = 0; i < expectedLines.Count; i++)
            {
                Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd(new [] { '\r', '\n' }));
            }
        }

        static void Should_Generate_From_Buffer()
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
            var cakeCorePath = $"{workingDirectory}/tools/Cake.Bakery/tools/Cake.Core.dll";
            var lineDirective = $"#line 1 \"{workingDirectory}/{fileName}\"";
            var sourceLines = response.Source.Split('\n').ToList();
            var expectedLines = CakeHelloWorldSrc.Split('\n').ToList();

            Assert.Equal(response.Host.AssemblyPath, cakeCorePath);
            Assert.Contains("Cake.Core", response.Usings);
            Assert.Contains(cakeCorePath, response.References, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

            var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
            for(var i = 0; i < expectedLines.Count; i++)
            {
                Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd(new [] { '\r', '\n' }));
            }
        }

        static void Should_Generate_With_Line_Changes()
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
            var cakeCorePath = $"{workingDirectory}/tools/Cake.Bakery/tools/Cake.Core.dll";
            var lineDirective = $"#line 1 \"{workingDirectory}/{fileName}\"";
            var sourceLines = response.Source.Split('\n').ToList();
            var expectedLines = CakeHelloWorldModified.Split('\n').ToList();

            Assert.Equal(response.Host.AssemblyPath, cakeCorePath);
            Assert.Contains("Cake.Core", response.Usings);
            Assert.Contains(cakeCorePath, response.References, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(lineDirective, sourceLines, StringComparer.OrdinalIgnoreCase);

            var startIndex = sourceLines.FindIndex(x => x.Equals(lineDirective, StringComparison.OrdinalIgnoreCase)) + 1;
            for(var i = 0; i < expectedLines.Count; i++)
            {
                Assert.Equal(sourceLines[startIndex + i], expectedLines[i].TrimEnd(new [] { '\r', '\n' }));
            }
        }

        static void Should_Install_Addins()
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
            var addin = Path.GetFullPath("./tools/Addins/Cake.Wyam.0.18.6/lib/net45/Cake.Wyam.dll")
                .Replace('\\', '/');

            Assert.Contains(addin, response.References);
        }

        static IScriptGenerationService service;
        static string workingDirectory;
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

        static int Main(string[] args)
        {
            var isRunningOnMono = Type.GetType("Mono.Runtime") != null;
            var loggerFactory = new LoggerFactory()
                .AddConsole(Microsoft.Extensions.Logging.LogLevel.Debug);

            var bakeryPath = args[0];
            workingDirectory = Environment.CurrentDirectory.Replace('\\', '/');;

            service = isRunningOnMono ?
                new ScriptGenerationClient(new MonoScriptGenerationProcess(bakeryPath, loggerFactory), workingDirectory, loggerFactory) :
                new ScriptGenerationClient(bakeryPath, workingDirectory, loggerFactory);

            Should_Generate_From_File();
            Should_Generate_From_Buffer();
            Should_Generate_With_Line_Changes();
            Should_Install_Addins();

            return 0;
        }
    }
}