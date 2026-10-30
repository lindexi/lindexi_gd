using System.ComponentModel;
using System.Text.Json;

namespace Builder;

public static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>执行原生构建编排，返回真实进程退出码或明确的诊断退出码。</summary>
    public static async Task<int> Main(string[] args)
    {
        if (args is ["--help"] or ["-h"]) { Console.WriteLine(Text.Get("Help")); return 0; }
        using var cancel = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancel.Cancel(); };
        Console.CancelKeyPress += handler;
        try { return await ExecuteAsync(args, cancel.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) { Console.Error.WriteLine(Text.Get("Cancelled")); return 130; }
        catch (TimeoutException ex) { Console.Error.WriteLine(ex.Message); return 124; }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException or
            UnauthorizedAccessException or InvalidOperationException or BadImageFormatException or Win32Exception)
        {
            Console.Error.WriteLine(string.Format(Text.Get("Failed"), ex.Message));
            return 2;
        }
        finally { Console.CancelKeyPress -= handler; }
    }

    private static string Root()
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (System.IO.File.Exists(Path.Combine(d.FullName, "Builder.sln"))) return d.FullName;
        throw new InvalidOperationException(Text.Missing("Builder.sln"));
    }

    private static async Task<int> ExecuteAsync(string[] args, CancellationToken ct)
    {
        Options o = Options.Parse(args, Root());
        string directory = Path.Combine(o.Root, "artifacts", $"{o.Command}-{o.TargetOs}-{o.Arch}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string status = "Started";
        int exit = 2;
        object? details = null;
        var hashes = new Dictionary<string, string>();
        var processResults = new List<object>();
        string? failure = null;
        try
        {
            Console.WriteLine(Text.Get("Unverified"));
            if (o.Command == "inspect")
            {
                if (o.File is null) throw new ArgumentException(Text.Get("Arguments"));
                var report = BinaryInspection.Inspect(o.File, o.Arch);
                details = report;
                Console.WriteLine(JsonSerializer.Serialize(report, Json));
                exit = report.HeaderMatchesTarget ? 0 : 3;
                status = report.HeaderMatchesTarget ? "HeadersOnlyPassed" : "HeadersFailed";
                return exit;
            }
            NativePlan plan = NativePlanning.Create(o, directory);
            details = plan;
            foreach (string blocker in plan.Blockers) Console.WriteLine(blocker);
            foreach (string path in new[] { Path.Combine(o.Runtime, "global.json"), Path.Combine(o.Runtime, "src/coreclr/CMakeLists.txt"),
                         Path.Combine(o.Runtime, "eng/native/configurecompiler.cmake"),
                         Path.Combine(o.Root, "Builder", "NativePlan.cs") }.Concat(o.Command == "baseline" ? [] : new[] { o.Obj }))
                if (System.IO.File.Exists(path)) hashes[path] = BinaryInspection.Hash(path);
            await SaveAsync(Path.Combine(directory, "plan.json"), plan).ConfigureAwait(false);
            if (o.Command == "doctor")
            {
                status = plan.Blockers.Length == 0 ? "PrerequisitesPassed" : "Blocked";
                exit = plan.Blockers.Length == 0 ? 0 : 2;
                return exit;
            }
            if (o.Command == "plan" || o.DryRun)
            {
                Console.WriteLine(JsonSerializer.Serialize(plan, Json));
                status = "PlannedNotExecuted";
                exit = plan.Blockers.Length == 0 ? 0 : 2;
                return exit;
            }
            if (plan.Blockers.Length != 0) { status = "Blocked"; return exit; }
            // Official Windows scripts own their output layout. Discard only generated CMake caches
            // so a baseline cannot inherit a previous injected toolchain (or vice versa).
            string objRoot = Path.Combine(o.Runtime, "artifacts", "obj");
            if (Directory.Exists(objRoot))
                foreach (string cache in Directory.EnumerateFiles(objRoot, "CMakeCache.txt", SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(objRoot, cache);
                    string libsConfig = o.Configuration == "Checked" ? "Debug" : o.Configuration;
                    if (relative.StartsWith(Path.Combine("coreclr", $"windows.{o.Arch}.{o.Configuration}") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        relative.StartsWith($"win-{o.Arch}.{o.Configuration}" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        (relative.StartsWith("native" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                         relative.Contains($"-windows-{libsConfig}-{o.Arch}" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                    {
                        ct.ThrowIfCancellationRequested();
                        System.IO.File.Delete(cache);
                        System.IO.File.Delete(Path.Combine(Path.GetDirectoryName(cache)!, "cmake_cmd_line.txt"));
                    }
                }
            await System.IO.File.WriteAllTextAsync(plan.Injection, NativePlanning.Injection(o), ct).ConfigureAwait(false);
            await System.IO.File.WriteAllTextAsync(Path.Combine(directory, "Toolchain.cmake"), NativePlanning.Toolchain(plan.Injection), ct).ConfigureAwait(false);
            hashes[plan.Injection] = BinaryInspection.Hash(plan.Injection);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(o.Timeout));
            for (int i = 0; i < plan.Commands.Length; i++)
            {
                exit = await ProcessRunner.RunAsync(plan.Commands[i], Path.Combine(directory, $"{i}.log"),
                    TimeSpan.FromSeconds(o.Timeout), deadline.Token).ConfigureAwait(false);
                processResults.Add(new { Step = i, ExitCode = exit });
                if (exit != 0)
                {
                    failure = Path.Combine(directory, $"{i}.log");
                    status = "ProcessFailed";
                    return exit;
                }
            }
            if (o.ConfigureOnly) { status = "OfficialConfigureCompletedLibrariesBuilt"; return exit; }
            if (o.FullRuntime)
            {
                details = new { Plan = plan, Layout = RuntimeLayout.Validate(Path.Combine(directory, "runtime"), o.Arch) };
                status = "RuntimeAssembledExecutionUnverified";
                return exit;
            }
            var reports = new List<PeReport>();
            string bin = Path.Combine(o.Runtime, "artifacts", "bin");
            if (!Directory.Exists(bin)) throw new InvalidOperationException(Text.Missing(bin));
            foreach (string path in Directory.EnumerateFiles(bin, "*", SearchOption.AllDirectories)
                         .Where(p => Path.GetExtension(p).Equals(".dll", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(p).Equals(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                ct.ThrowIfCancellationRequested();
                string relative = Path.GetRelativePath(bin, path);
                string libraryConfig = o.Configuration == "Checked" ? "Debug" : o.Configuration;
                string clrPrefix = Path.Combine("coreclr", $"windows.{o.Arch}.{o.Configuration}") + Path.DirectorySeparatorChar;
                string[] segments = relative.Split(Path.DirectorySeparatorChar);
                bool isLibrariesOutput = segments.Length > 2 && segments[0] == "native" &&
                    segments[1].EndsWith($"-windows-{libraryConfig}-{o.Arch}", StringComparison.OrdinalIgnoreCase);
                string hostPrefix = $"win-{o.Arch}.{o.Configuration}" + Path.DirectorySeparatorChar;
                if (!relative.StartsWith(clrPrefix, StringComparison.OrdinalIgnoreCase) &&
                    !isLibrariesOutput &&
                    !relative.StartsWith(hostPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                string copy = Path.Combine(directory, "bin", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                System.IO.File.Copy(path, copy);
                var report = BinaryInspection.Inspect(copy, o.Arch);
                reports.Add(report);
                hashes[path] = report.Sha256;
            }
            details = new { Plan = plan, Reports = reports };
            foreach (string required in new[] { "coreclr.dll", "System.Globalization.Native.dll", "System.IO.Compression.Native.dll", "hostfxr.dll", "hostpolicy.dll", "dotnet.exe" })
                if (!reports.Any(r => Path.GetFileName(r.Path).Equals(required, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException(Text.Missing(required));
            exit = reports.All(r => r.HeaderMatchesTarget) ? 0 : 3;
            status = exit == 0 ? "NativeBuildSucceededCompatibilityUnverified" : "HeadersFailed";
            return exit;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { status = "TimedOut"; exit = 124; throw new TimeoutException(Text.Get("Timeout")); }
        catch (OperationCanceledException) { status = "Cancelled"; exit = 130; throw; }
        catch (TimeoutException ex) { status = "TimedOut"; failure = ex.Message; exit = 124; throw; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or
            Win32Exception or BadImageFormatException or ArgumentException)
        { status = "Failed"; failure = ex.Message; exit = 2; throw; }
        finally
        {
            using var package = o.Command != "baseline" && o.ObjectSource == "NuGetPackage"
                ? JsonDocument.Parse(await System.IO.File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "yy-thunks", "package.json")).ConfigureAwait(false))
                : null;
            await SaveAsync(Path.Combine(directory, "manifest.json"), new
            {
                SchemaVersion = 1, Status = status, ExitCode = exit, Failure = failure, Options = o,
                CompletedUtc = DateTimeOffset.UtcNow, Compatibility = "Unverified", ImportValidation = "NotImplemented",
                ThunkObject = o.Command == "baseline" ? null : new
                {
                    Source = o.ObjectSource,
                    PackageId = o.ObjectSource == "NuGetPackage" ? "YY-Thunks" : null,
                    PackageVersion = package?.RootElement.GetProperty("PackageVersion").GetString(),
                    PackagePath = o.ObjectSource == "NuGetPackage" ? $"build/native/objs/{o.Arch}/YY_Thunks_for_Win7.obj" : null,
                    FixObj = "NotIndependentlyVerified",
                    Sha256 = hashes.GetValueOrDefault(o.Obj)
                },
                InputsAndOutputsSha256 = hashes, Processes = processResults, Details = details
            }).ConfigureAwait(false);
            Console.WriteLine(directory);
        }
    }

    private static Task SaveAsync(string path, object value) => System.IO.File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, Json));
}
