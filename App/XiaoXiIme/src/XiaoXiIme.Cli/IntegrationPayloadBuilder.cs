using System.Diagnostics;
using System.Security.Cryptography;

namespace XiaoXiIme.Cli;

internal static class IntegrationPayloadBuilder
{
    private const string SharedRuntimeIdentifier = "win-x64";
    private static readonly string[] NativeRuntimeIdentifiers = ["win-x86", "win-x64"];

    public static async Task<int> BuildAsync(PayloadBuildOptions options, TextWriter output, TextWriter error)
    {
        var log = new StructuredConsole(output, error);
        var stagingDirectory = options.NoBuild
            ? FindPublishDirectory(AppContext.BaseDirectory, Environment.CurrentDirectory)
            : Path.Combine(FindSolutionDirectory(AppContext.BaseDirectory), "artifacts", "integration-publish");
        var solutionDirectory = options.NoBuild
            ? stagingDirectory
            : FindSolutionDirectory(AppContext.BaseDirectory);
        var outputDirectory = Path.GetFullPath(options.Output ?? Path.Combine(solutionDirectory, "artifacts", "integration-payload"));

        if (!options.NoBuild)
        {
            Directory.CreateDirectory(stagingDirectory);
            var commands = CreateBuildCommands(solutionDirectory, stagingDirectory);
            foreach (var command in commands)
            {
                var exitCode = await RunProcessAsync(command, solutionDirectory, log);
                if (exitCode != 0)
                {
                    return 10;
                }
            }
        }

        var sources = CreateSources(stagingDirectory);
        var missing = sources.Where(source => !File.Exists(source.SourcePath)).Select(source => source.SourcePath).ToArray();
        if (missing.Length > 0)
        {
            log.Error("payload", "Required publish outputs are missing.", missing);
            return 11;
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }
        Directory.CreateDirectory(outputDirectory);

        foreach (var source in sources)
        {
            CopyDirectory(Path.GetDirectoryName(source.SourcePath)!, Path.Combine(outputDirectory, source.TargetDirectory));
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (var runtimeIdentifier in NativeRuntimeIdentifiers)
            {
                var imePath = Path.Combine(outputDirectory, "native", runtimeIdentifier, "ime", "XiaoXiIme.ime");
                var validationError = ImeBinaryValidator.Validate(imePath);
                if (validationError is not null)
                {
                    log.Error("payload", $"Invalid {runtimeIdentifier} IME binary: {validationError}");
                    return 12;
                }
            }
        }

        var files = Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => CreatePayloadFile(outputDirectory, path))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var manifest = new IntegrationPayloadManifest(
            3,
            DateTimeOffset.UtcNow,
            new Dictionary<string, NativePayloadComponents>
            {
                ["x86"] = new("win-x86", "native/win-x86/ime/XiaoXiIme.ime", "native/win-x86/tsf/XiaoXiIme.TsfModule.dll", "native/win-x86/tools/XiaoXiIme.TsfAbiHost.exe"),
                ["x64"] = new("win-x64", "native/win-x64/ime/XiaoXiIme.ime", "native/win-x64/tsf/XiaoXiIme.TsfModule.dll", "native/win-x64/tools/XiaoXiIme.TsfAbiHost.exe"),
            },
            "app/cli/XiaoXiIme.Cli.exe",
            "app/host/XiaoXiIme.ImeHost.exe",
            ["app/tests/XiaoXiIme.IntegrationTestHost.exe"],
            files);
        var manifestPath = Path.Combine(outputDirectory, IntegrationPayloadManifest.FileName);
        manifest.Save(manifestPath);
        log.Information("payload", "Integration payload created.", new { outputDirectory, manifestPath, fileCount = files.Length });
        return 0;
    }

    private static IReadOnlyList<BuildCommand> CreateBuildCommands(string solutionDirectory, string stagingDirectory)
    {
        var commands = new List<BuildCommand>
        {
            new("build", ["build", Path.Combine(solutionDirectory, "XiaoXiIme.slnx"), "-c", "Release"]),
        };
        foreach (var runtimeIdentifier in NativeRuntimeIdentifiers)
        {
            var nativeRoot = Path.Combine("native", runtimeIdentifier);
            commands.Add(Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj", Path.Combine(nativeRoot, "ime"), true));
            commands.Add(Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.TsfModule/XiaoXiIme.TsfModule.csproj", Path.Combine(nativeRoot, "tsf"), true));
            commands.Add(Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "tests/XiaoXiIme.TsfAbiHost/XiaoXiIme.TsfAbiHost.csproj", Path.Combine(nativeRoot, "tools"), false));
        }
        commands.Add(Publish(solutionDirectory, stagingDirectory, SharedRuntimeIdentifier, "src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj", Path.Combine("app", "cli"), false));
        commands.Add(Publish(solutionDirectory, stagingDirectory, SharedRuntimeIdentifier, "src/XiaoXiIme.ImeHost/XiaoXiIme.ImeHost.csproj", Path.Combine("app", "host"), false));
        commands.Add(Publish(solutionDirectory, stagingDirectory, SharedRuntimeIdentifier, "tests/XiaoXiIme.IntegrationTestHost/XiaoXiIme.IntegrationTestHost.csproj", Path.Combine("app", "tests"), false));
        return commands;
    }

    private static BuildCommand Publish(string solutionDirectory, string stagingDirectory, string runtimeIdentifier, string project, string folder, bool nativeAot)
    {
        var arguments = new List<string>
        {
            "publish", Path.Combine(solutionDirectory, project), "-c", "Release", "-r", runtimeIdentifier,
            "--self-contained", "true", "-o", Path.Combine(stagingDirectory, folder),
        };
        if (nativeAot)
        {
            arguments.Add("-p:PublishAot=true");
        }
        return new BuildCommand($"publish-{folder}", arguments);
    }

    private static PayloadSource[] CreateSources(string stagingDirectory) =>
    [
        new(Path.Combine(stagingDirectory, "native", "win-x86", "ime", "XiaoXiIme.ime"), Path.Combine("native", "win-x86", "ime")),
        new(Path.Combine(stagingDirectory, "native", "win-x86", "tsf", "XiaoXiIme.TsfModule.dll"), Path.Combine("native", "win-x86", "tsf")),
        new(Path.Combine(stagingDirectory, "native", "win-x86", "tools", "XiaoXiIme.TsfAbiHost.exe"), Path.Combine("native", "win-x86", "tools")),
        new(Path.Combine(stagingDirectory, "native", "win-x64", "ime", "XiaoXiIme.ime"), Path.Combine("native", "win-x64", "ime")),
        new(Path.Combine(stagingDirectory, "native", "win-x64", "tsf", "XiaoXiIme.TsfModule.dll"), Path.Combine("native", "win-x64", "tsf")),
        new(Path.Combine(stagingDirectory, "native", "win-x64", "tools", "XiaoXiIme.TsfAbiHost.exe"), Path.Combine("native", "win-x64", "tools")),
        new(Path.Combine(stagingDirectory, "app", "cli", "XiaoXiIme.Cli.exe"), Path.Combine("app", "cli")),
        new(Path.Combine(stagingDirectory, "app", "host", "XiaoXiIme.ImeHost.exe"), Path.Combine("app", "host")),
        new(Path.Combine(stagingDirectory, "app", "tests", "XiaoXiIme.IntegrationTestHost.exe"), Path.Combine("app", "tests")),
    ];

    private static async Task<int> RunProcessAsync(BuildCommand command, string workingDirectory, StructuredConsole log)
    {
        log.Information(command.Id, "Starting dotnet command.", new { command.Arguments });
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start dotnet.");
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            log.Information(command.Id, "dotnet output", standardOutput);
        }
        if (!string.IsNullOrWhiteSpace(standardError))
        {
            log.Error(command.Id, "dotnet error output", standardError);
        }
        return process.ExitCode;
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var file in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), true);
        }
    }

    private static PayloadFile CreatePayloadFile(string root, string path)
    {
        using var stream = File.OpenRead(path);
        return new PayloadFile(Path.GetRelativePath(root, path).Replace('\\', '/'), stream.Length, Convert.ToHexString(SHA256.HashData(stream)));
    }

    private static string FindPublishDirectory(params string[] startDirectories)
    {
        foreach (var startDirectory in startDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "app"))
                    && Directory.Exists(Path.Combine(directory.FullName, "native")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Unable to locate the integration publish directory from the CLI or current directory.");
    }

    private static string FindSolutionDirectory(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "XiaoXiIme.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Unable to locate XiaoXiIme.slnx from the CLI directory.");
    }

    private sealed record BuildCommand(string Id, IReadOnlyList<string> Arguments);
    private sealed record PayloadSource(string SourcePath, string TargetDirectory);
}
