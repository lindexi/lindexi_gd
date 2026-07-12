using System.Diagnostics;
using System.Security.Cryptography;

namespace XiaoXiIme.Cli;

internal static class IntegrationPayloadBuilder
{
    public static async Task<int> BuildAsync(PayloadBuildOptions options, TextWriter output, TextWriter error)
    {
        var log = new StructuredConsole(output, error);
        var solutionDirectory = FindSolutionDirectory(AppContext.BaseDirectory);
        var outputDirectory = Path.GetFullPath(options.Output ?? Path.Combine(solutionDirectory, "artifacts", "integration-payload", options.RuntimeIdentifier));
        var stagingDirectory = Path.Combine(solutionDirectory, "artifacts", "integration-publish", options.RuntimeIdentifier);

        if (!options.NoBuild)
        {
            Directory.CreateDirectory(stagingDirectory);
            var commands = CreateBuildCommands(solutionDirectory, stagingDirectory, options.RuntimeIdentifier);
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

        var imeDll = Path.Combine(outputDirectory, "ime", "XiaoXiIme.ImeModule.dll");
        var imeFile = Path.Combine(outputDirectory, "ime", "XiaoXiIme.ime");
        File.Move(imeDll, imeFile, true);

        var files = Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Select(path => CreatePayloadFile(outputDirectory, path))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var manifest = new IntegrationPayloadManifest(
            1,
            DateTimeOffset.UtcNow,
            options.RuntimeIdentifier,
            "ime/XiaoXiIme.ime",
            "tsf/XiaoXiIme.TsfModule.dll",
            "cli/XiaoXiIme.Cli.exe",
            "host/XiaoXiIme.ImeHost.exe",
            "tools/XiaoXiIme.TsfAbiHost.exe",
            ["tests/XiaoXiIme.IntegrationTests.dll"],
            files);
        var manifestPath = Path.Combine(outputDirectory, IntegrationPayloadManifest.FileName);
        manifest.Save(manifestPath);
        log.Information("payload", "Integration payload created.", new { outputDirectory, manifestPath, fileCount = files.Length });
        return 0;
    }

    private static IReadOnlyList<BuildCommand> CreateBuildCommands(string solutionDirectory, string stagingDirectory, string runtimeIdentifier) =>
    [
        new("build", ["build", Path.Combine(solutionDirectory, "XiaoXiIme.slnx"), "-c", "Release"]),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj", "ime", true),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.TsfModule/XiaoXiIme.TsfModule.csproj", "tsf", true),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj", "cli", false),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "src/XiaoXiIme.ImeHost/XiaoXiIme.ImeHost.csproj", "host", false),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "tests/XiaoXiIme.TsfAbiHost/XiaoXiIme.TsfAbiHost.csproj", "tools", false),
        Publish(solutionDirectory, stagingDirectory, runtimeIdentifier, "tests/XiaoXiIme.IntegrationTests/XiaoXiIme.IntegrationTests.csproj", "tests", false),
    ];

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
        new(Path.Combine(stagingDirectory, "ime", "XiaoXiIme.ImeModule.dll"), "ime"),
        new(Path.Combine(stagingDirectory, "tsf", "XiaoXiIme.TsfModule.dll"), "tsf"),
        new(Path.Combine(stagingDirectory, "cli", "XiaoXiIme.Cli.exe"), "cli"),
        new(Path.Combine(stagingDirectory, "host", "XiaoXiIme.ImeHost.exe"), "host"),
        new(Path.Combine(stagingDirectory, "tools", "XiaoXiIme.TsfAbiHost.exe"), "tools"),
        new(Path.Combine(stagingDirectory, "tests", "XiaoXiIme.IntegrationTests.dll"), "tests"),
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
