using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly Dictionary<string, string> Sources = new()
    {
        ["x86"] = "baseline-win7-x86-20261030101414-e80ee875e78744db96726538b7a076f5",
        ["x64"] = "baseline-win7-x64-20261030103039-a5261b67dab040c093284f25151f7fc8"
    };

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static Dictionary<string, string> Inventory(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .ToDictionary(p => Path.GetRelativePath(root, p), Hash);

    private static void CheckLayout(string root)
    {
        string[] allowed = ["dotnet.exe", "host", "shared", "LICENSE.txt", "ThirdPartyNotices.txt"];
        if (!Directory.GetFileSystemEntries(root).Select(Path.GetFileName).Order().SequenceEqual(allowed.Order()))
            throw new InvalidDataException("Unexpected runtime root entries: " + root);
    }

    private static void Copy(string source, string destination)
    {
        if (Directory.Exists(destination)) throw new IOException("Refusing to overwrite: " + destination);
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, false);
        }
        if (!Inventory(source).OrderBy(p => p.Key).SequenceEqual(Inventory(destination).OrderBy(p => p.Key)))
            throw new InvalidDataException("Copy hash mismatch");
    }

    private static async Task<int> RunAsync(string exe, string[] arguments, string log, string? cwd = null)
    {
        var start = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = cwd ?? Environment.CurrentDirectory };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        foreach (string key in start.Environment.Keys.Where(k => k.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase) || k.StartsWith("CORECLR_", StringComparison.OrdinalIgnoreCase) || k.StartsWith("COMPlus_", StringComparison.OrdinalIgnoreCase)).ToArray()) start.Environment.Remove(key);
        start.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Process did not start");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); await process.WaitForExitAsync(); throw; }
        string output = await stdout, error = await stderr;
        string text = $"Executable: {exe}\nArguments: {JsonSerializer.Serialize(arguments)}\nExitCode: {process.ExitCode}\nSTDOUT:\n{output}\nSTDERR:\n{error}";
        await File.WriteAllTextAsync(log, text);
        Console.WriteLine(text);
        return process.ExitCode;
    }

    private static async Task<int> Main(string[] args)
    {
        bool yy = args.Contains("--yy");
        if (yy)
        {
            Sources["x86"] = "build-win7-x86-20261030125311-7969919196624435bcc524b61ab39253";
            Sources["x64"] = "build-win7-x64-20261030125619-95c42902e51446dcb62ce1860fe89243";
        }
        if (args[0] == "sandbox")
        {
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            string evidence = Path.Combine(root, "evidence");
            Directory.CreateDirectory(evidence);
            var results = new Dictionary<string, int>();
            foreach (string arch in Sources.Keys)
            {
                string runtime = Path.Combine(root, yy ? "payload-yy" : "payload", "runtime-win-" + arch);
                CheckLayout(runtime);
                foreach (string option in new[] { "--info", "--list-runtimes", "--list-sdks" })
                {
                    string key = arch + option;
                    results[key] = await RunAsync(Path.Combine(runtime, "dotnet.exe"), [option], Path.Combine(evidence, key + ".log"));
                }
                string appLog = Path.Combine(evidence, arch + "-app.log");
                results[arch + "-app"] = await RunAsync(Path.Combine(runtime, "dotnet.exe"), [Path.Combine(root, yy ? "payload-yy" : "payload", "app", "Smoke.dll"), arch], appLog);
                string text = File.ReadAllText(appLog);
                if (!text.Contains("SMOKE_OK arch=" + arch) || !text.Contains(Path.Combine(runtime, "shared"), StringComparison.OrdinalIgnoreCase))
                    results[arch + "-app"] = -1;
                if (File.ReadAllText(Path.Combine(evidence, arch + "--list-sdks.log")).Split("STDOUT:\n")[1].Split("\nSTDERR:")[0].Trim().Length != 0)
                    results[arch + "--list-sdks"] = -1;
                await File.WriteAllTextAsync(Path.Combine(evidence, arch + "-hashes.json"), JsonSerializer.Serialize(Inventory(runtime), Json));
            }
            await File.WriteAllTextAsync(Path.Combine(evidence, "results.json"), JsonSerializer.Serialize(results, Json));
            return results.Values.All(v => v == 0) ? 0 : 1;
        }
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Builder.sln"))) directory = directory.Parent;
        string builder = directory?.FullName ?? throw new DirectoryNotFoundException("Builder.sln");
        JsonDocument? gate = null;
        if (yy && args[0] == "publish")
        {
            if (!args.Contains("--allow-unverified-third-party")) throw new InvalidOperationException("Explicit --allow-unverified-third-party required");
            gate = JsonDocument.Parse(File.ReadAllText(Path.Combine(builder, "artifacts", "yythunks-audit", "candidate-gate.json")));
            var audit = gate.RootElement;
            if (audit.GetProperty("YYThunksIntegratedScope").GetString() != "SelfBuiltNativeTargets" || audit.GetProperty("StaticAuditExitCode").GetInt32() != 1) throw new InvalidDataException("Invalid candidate audit scope");
            var architectures = audit.GetProperty("Architectures").EnumerateArray().ToArray();
            if (architectures.Length != 2 || architectures.Any(a => !a.GetProperty("SelfBuiltPassed").GetBoolean() || !a.GetProperty("SandboxHashesMatch").GetBoolean())) throw new InvalidDataException("Self-built audit incomplete");
            var uncovered = audit.GetProperty("ThirdPartyUncovered").EnumerateArray().ToArray();
            if (uncovered.Length != 4 || uncovered.Any(t => !t.GetProperty("PackageAssetMatches").GetBoolean())) throw new InvalidDataException("Third-party provenance incomplete");
            foreach (var (architecture, buildRun) in Sources)
            {
                var recorded = architectures.Single(a => a.GetProperty("Architecture").GetString() == architecture).GetProperty("FilesSha256").Deserialize<Dictionary<string, string>>()!;
                if (!recorded.OrderBy(p => p.Key).SequenceEqual(Inventory(Path.Combine(builder, "artifacts", buildRun, "runtime")).OrderBy(p => p.Key))) throw new InvalidDataException("Stale audit payload");
            }
        }
        if (yy && args[0] == "stage") Copy(Path.Combine(builder, "Verification", "payload", "app"), Path.Combine(builder, "Verification", "payload-yy", "app"));
        foreach ((string arch, string run) in Sources)
        {
            string source = Path.Combine(builder, "artifacts", run, "runtime");
            CheckLayout(source);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(builder, "artifacts", run, "manifest.json")));
            if (manifest.RootElement.GetProperty("ExitCode").GetInt32() != 0 || manifest.RootElement.GetProperty("Options").GetProperty("Command").GetString() != (yy ? "build" : "baseline")) throw new InvalidDataException("Not a successful baseline");
            var original = manifest.RootElement.GetProperty("Details").GetProperty("Layout").Deserialize<Dictionary<string, string>>()!;
            var hashes = Inventory(source);
            if (!original.OrderBy(p => p.Key).SequenceEqual(hashes.OrderBy(p => p.Key))) throw new InvalidDataException("Original build hash mismatch");
            if (args[0] == "stage") Copy(source, Path.Combine(builder, "Verification", yy ? "payload-yy" : "payload", "runtime-win-" + arch));
            else if (args[0] == "publish")
            {
                string evidence = Path.GetFullPath(args[1]);
                var results = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(Path.Combine(evidence, "results.json")))!;
                if (results.Count != 8 || results.Values.Any(v => v != 0)) throw new InvalidDataException("Sandbox acceptance failed");
                var verified = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(evidence, arch + "-hashes.json")))!;
                if (!verified.OrderBy(p => p.Key).SequenceEqual(hashes.OrderBy(p => p.Key))) throw new InvalidDataException("Sandbox hash mismatch");
                string stable = Path.Combine(builder, "artifacts", (yy ? "runtime-yythunks-win-" : "runtime-win-") + arch);
                string releaseManifest = stable + ".manifest.json";
                if (File.Exists(releaseManifest)) throw new IOException("Existing release manifest");
                Copy(source, stable);
                await File.WriteAllTextAsync(releaseManifest, JsonSerializer.Serialize(new { SchemaVersion = 1, BuildKind = yy ? "candidate" : "baseline", YYThunksIntegrated = false, YYThunksIntegratedScope = yy ? "SelfBuiltNativeTargets" : "None", ThirdPartyUncovered = gate?.RootElement.GetProperty("ThirdPartyUncovered"), StaticAuditExitCode = yy ? 1 : (int?)null, IsSdk = false, Architecture = arch, Version = Path.GetFileName(Directory.GetDirectories(Path.Combine(source, "shared", "Microsoft.NETCore.App")).Single()), Source = Path.GetRelativePath(builder, source), OriginalManifest = Path.Combine("artifacts", run, "manifest.json"), Evidence = Path.GetRelativePath(builder, evidence), Compatibility = "Win7Unverified", SandboxAcceptance = results, FilesSha256 = hashes }, Json));
                Console.WriteLine("Published verified baseline without YY: " + stable);
            }
            else throw new ArgumentException("stage, sandbox or publish required");
        }
        return 0;
    }
}
