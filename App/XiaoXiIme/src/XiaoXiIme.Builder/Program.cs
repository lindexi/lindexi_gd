using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using XiaoXiIme.Builder;
using XiaoXiIme.Dictionary;

return await BuildAsync(AppContext.BaseDirectory);

static async Task<int> BuildAsync(string executableDirectory)
{
    var operation = "LocateRepository";
    var path = executableDirectory;
    try
    {
        var root = new DirectoryInfo(executableDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "XiaoXiIme.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            Console.Error.WriteLine($"[Builder][{operation}] Path={path}; Error=XiaoXiIme.slnx not found");
            return 1;
        }

        operation = "SelectSdk";
        path = root.FullName;
        var sdk = DotNetSdk.Resolve(root);
        sdk.LogSelection();
        if (sdk.IsLocal)
        {
            path = sdk.Command;
            await sdk.ValidateLocalAsync();
        }

        var output = Path.Combine(root.FullName, "artifacts", "distribution");
        var staging = output + ".building";
        operation = "PrepareOutput";
        path = staging;
        if (Directory.Exists(staging))
        {
            Directory.Delete(staging, recursive: true);
        }
        Directory.CreateDirectory(staging);

        operation = "RestoreRuntime";
        path = Path.Combine(staging, "dotnet");
        await RestoreRuntimeAsync(sdk, Path.GetDirectoryName(staging)!, path);

        operation = "Publish";
        foreach (var (project, rid, destination, frameworkDependent) in PublishTargets())
        {
            path = Path.Combine(root.FullName, project);
            var destinationPath = Path.Combine(staging, destination);
            if (await PublishAsync(sdk, path, rid, destinationPath, frameworkDependent) is var exitCode && exitCode != 0)
            {
                Console.Error.WriteLine($"[Builder][{operation}] Project={path}; RuntimeIdentifier={rid}; Output={destinationPath}; ExitCode={exitCode}");
                return exitCode;
            }
        }

        operation = "CompileDictionaries";
        path = Path.Combine(root.FullName, "data", "dictionaries");
        var phonetic = FindSources(path, "*.phonetic.tsv");
        var shape = FindSources(path, "*.shape.tsv");
        var symbols = FindSources(path, "*.symbols.tsv");
        foreach (var scheme in PhoneticSchemeCatalog.All.Where(item => item.ProductScheme.HasValue))
        {
            path = DictionaryPackageLocations.GetDistributionPackageDirectory(staging, scheme.SchemeId);
            LocalDictionaryPackageManager.Update(new LocalDictionaryPackageUpdate
            {
                PhoneticSourcePaths = phonetic,
                ShapeSourcePaths = shape,
                SymbolSourcePaths = symbols,
                Parameters = new DictionaryPackageParameters { InputScheme = scheme.SchemeId },
            }, path);
            DictionaryPackageLoader.Load(path);
        }

        operation = "CommitOutput";
        path = output;
        if (Directory.Exists(output))
        {
            Directory.Delete(output, recursive: true);
        }
        Directory.Move(staging, output);
        Console.WriteLine($"[Builder][Complete] Output={output}");
        return 0;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException
                                  or HttpRequestException or OperationCanceledException or JsonException
                                  or System.ComponentModel.Win32Exception or DictionarySourceException or DictionaryPackageException)
    {
        Console.Error.WriteLine($"[Builder][{operation}] Path={path}; Error={error}");
        return 1;
    }
}

static IEnumerable<(string Project, string Rid, string Destination, bool FrameworkDependent)> PublishTargets()
{
    yield return (Path.Combine("src", "XiaoXiIme", "XiaoXiIme.csproj"), "win-x64", "", false);
    yield return (Path.Combine("src", "XiaoXiIme.Cli", "XiaoXiIme.Cli.csproj"), "win-x64", Path.Combine("Lib", "Cli"), true);
    yield return (Path.Combine("src", "XiaoXiIme.ImeUi.Avalonia", "XiaoXiIme.ImeUi.Avalonia.csproj"), "win-x64", Path.Combine("Lib", "ImeUI"), true);
    yield return (Path.Combine("tests", "XiaoXiIme.IntegrationTestHost", "XiaoXiIme.IntegrationTestHost.csproj"), "win-x64", "Tests", false);
    foreach (var rid in new[] { "win-x86", "win-x64" })
    {
        yield return (Path.Combine("src", "XiaoXiIme.ImeModule", "XiaoXiIme.ImeModule.csproj"), rid, Path.Combine("native", "ime", rid), false);
        yield return (Path.Combine("src", "XiaoXiIme.TsfModule", "XiaoXiIme.TsfModule.csproj"), rid, Path.Combine("native", "tsf", rid), false);
        yield return (Path.Combine("tests", "XiaoXiIme.TsfAbiHost", "XiaoXiIme.TsfAbiHost.csproj"), rid, Path.Combine("Tests", "tsf", rid), false);
    }
}

static async Task<int> PublishAsync(DotNetSdk sdk, string project, string rid, string destination, bool frameworkDependent)
{
    var info = sdk.CreateStartInfo();
    if (sdk.IsLocal)
    {
        // Keep NativeAOT intermediates separate from ordinary system SDK builds.
        info.Environment["ArtifactsPath"] = Path.Combine(info.WorkingDirectory, "artifacts", "local-sdk");
    }
    foreach (var arg in new[] { "publish", project, "-c", "Release", "-r", rid, "-o", destination, "--self-contained", frameworkDependent ? "false" : "true" })
    {
        info.ArgumentList.Add(arg);
    }
    if (frameworkDependent)
    {
        info.ArgumentList.Add("-p:UseAppHost=false");
    }
    Console.WriteLine($"[Builder][Publish] Project={project}; RuntimeIdentifier={rid}; Output={destination}");
    using var process = Process.Start(info) ?? throw new InvalidOperationException($"Failed to start dotnet publish for {project}.");
    await process.WaitForExitAsync();
    return process.ExitCode;
}

static async Task RestoreRuntimeAsync(DotNetSdk sdk, string artifacts, string destination)
{
    const string packageId = "Microsoft.NETCore.App.Runtime.win-x64";
    const string source = "https://api.nuget.org/v3/index.json";
    var packageKey = packageId.ToLowerInvariant();
    var index = $"https://api.nuget.org/v3-flatcontainer/{packageKey}/index.json";
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var request = new HttpRequestMessage(HttpMethod.Get, index);
    request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
    using var response = await http.SendAsync(request, deadline.Token);
    response.EnsureSuccessStatusCode();
    await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
    using var versions = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
    var latest = versions.RootElement.GetProperty("versions").EnumerateArray()
        .Select(item => Version.TryParse(item.GetString(), out var version) ? version : null)
        .Where(version => version is { Major: 10, Minor: 0, Build: >= 0, Revision: -1 })
        .Max() ?? throw new InvalidOperationException($"No stable .NET 10.0 runtime was found at {index}.");
    var packageVersion = latest.ToString(3);
    Console.WriteLine($"[Builder][RestoreRuntime] Package={packageId}; Version={packageVersion}; Source={source}");

    var restoreDirectory = Path.Combine(artifacts, $"runtime-restore-{Guid.NewGuid():N}");
    Directory.CreateDirectory(restoreDirectory);
    try
    {
        var project = Path.Combine(restoreDirectory, "RuntimeRestore.csproj");
        await File.WriteAllTextAsync(project, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <RestoreSources>{source}</RestoreSources>
              </PropertyGroup>
              <ItemGroup>
                <PackageDownload Include="{packageId}" Version="[{packageVersion}]" />
              </ItemGroup>
            </Project>
            """);
        var config = Path.Combine(restoreDirectory, "NuGet.Config");
        await File.WriteAllTextAsync(config, $"""
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="{source}" />
              </packageSources>
            </configuration>
            """);
        var info = sdk.CreateStartInfo();
        foreach (var arg in new[] { "restore", project, "--configfile", config,
            "--packages", Path.Combine(restoreDirectory, "packages"), "--no-http-cache",
            "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false",
            "-p:ImportDirectoryPackagesProps=false" })
        {
            info.ArgumentList.Add(arg);
        }
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Failed to start restore for {packageId}.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Restore for {packageId} {packageVersion} exited with code {process.ExitCode}.");
        }

        await using var assetsStream = File.OpenRead(Path.Combine(restoreDirectory, "obj", "project.assets.json"));
        using var assets = await JsonDocument.ParseAsync(assetsStream);
        var package = assets.RootElement.GetProperty("packageFolders").EnumerateObject()
            .Select(folder => Path.Combine(folder.Name, packageKey, packageVersion))
            .FirstOrDefault(directory => Directory.Exists(directory))
            ?? throw new DirectoryNotFoundException($"NuGet restore did not locate {packageId} {packageVersion}.");
        var runtime = Path.Combine(package, "runtimes", "win-x64");
        var native = Path.Combine(runtime, "native");
        var shared = Path.Combine(destination, "shared", "Microsoft.NETCore.App", packageVersion);
        var fxr = Path.Combine(destination, "host", "fxr", packageVersion);
        const string hostFxr = "hostfxr.dll";
        Directory.CreateDirectory(fxr);
        File.Copy(Path.Combine(native, hostFxr), Path.Combine(fxr, hostFxr));
        CopyDirectory(native, shared, hostFxr);
        CopyDirectory(Path.Combine(runtime, "lib", "net10.0"), shared);
        foreach (var file in new[] { "coreclr.dll", "hostpolicy.dll", "System.Private.CoreLib.dll",
            "Microsoft.NETCore.App.deps.json", "Microsoft.NETCore.App.runtimeconfig.json" })
        {
            if (!File.Exists(Path.Combine(shared, file)))
            {
                throw new FileNotFoundException($"Required runtime asset is missing from {packageId} {packageVersion}.", file);
            }
        }
        foreach (var notice in new[] { "LICENSE.TXT", "THIRD-PARTY-NOTICES.TXT" })
        {
            File.Copy(Path.Combine(package, notice), Path.Combine(destination, notice));
        }
    }
    finally
    {
        Directory.Delete(restoreDirectory, recursive: true);
    }
}

static string[] FindSources(string root, string pattern) =>
    Directory.GetFiles(root, pattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();

static void CopyDirectory(string source, string destination, string? excludedFile = null)
{
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(source, file);
        if (string.Equals(relative, excludedFile, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
        var target = Path.Combine(destination, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(file, target);
    }
}
