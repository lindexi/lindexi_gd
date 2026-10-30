using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

string builder = Path.GetFullPath(args.Length == 0 ? "Builder" : args[0]);
string workspace = Directory.GetParent(builder)!.FullName;
string output = Path.Combine(builder, "artifacts", "integrity-audit");
Directory.CreateDirectory(output);
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
void Save(string name, object value) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, jsonOptions));
string Relative(string path) => Path.GetRelativePath(workspace, path).Replace('\\', '/');
string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
string Machine(string path)
{
    if (!new[] { ".dll", ".exe" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return "not-pe";
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    if (pe.PEHeaders.PEHeader is null) return "not-pe";
    return pe.PEHeaders.CoffHeader.Machine.ToString();
}
Entry[] Inventory(string root) => Directory.Exists(root) ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
    .Order(StringComparer.OrdinalIgnoreCase).Select(p => new Entry(Path.GetRelativePath(root, p).Replace('\\', '/'), Relative(p), new FileInfo(p).Length, Hash(p), Machine(p))).ToArray() : [];
var evidence = new List<object>();
void Capture(string file, string kind)
{
    if (!File.Exists(file)) { evidence.Add(new { Path = Relative(file), Kind = kind, Missing = true }); return; }
    string destination = Path.Combine(output, "evidence", Relative(file).Replace("../", "_parent_/", StringComparison.Ordinal).Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    File.Copy(file, destination, true);
    evidence.Add(new { Path = Relative(file), Kind = kind, Sha256 = Hash(file), Length = new FileInfo(file).Length });
}
var textHits = new List<object>();
void Scan(string file, string kind, string pattern)
{
    Capture(file, kind);
    if (!File.Exists(file)) return;
    int line = 0;
    foreach (string text in File.ReadLines(file))
    {
        line++;
        if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase)) textHits.Add(new { Path = Relative(file), Line = line, Kind = kind, Text = text });
    }
}
const string interesting = @"python|crossgen|ReadyToRun|skip|up.to.date|cached|generat|\.py\b|resource|subset|ninja|cmake|Build succeeded|error |warning |Time Elapsed|verify|validat|manifest";
var plans = new List<object>();
foreach (string dir in Directory.GetDirectories(Path.Combine(builder, "artifacts"), "baseline-*"))
{
    foreach (string file in Directory.GetFiles(dir, "*.log")) Scan(file, "historical-build-log", interesting);
    foreach (string file in Directory.GetFiles(dir, "*.cmake")) Capture(file, "historical-injection");
    string plan = Path.Combine(dir, "plan.json");
    Capture(plan, "historical-plan");
    if (File.Exists(plan))
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(plan));
        var commands = new List<string>();
        foreach (var command in doc.RootElement.GetProperty("Commands").EnumerateArray())
        {
            var arguments = command.GetProperty("Arguments").EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
            int index = Array.IndexOf(arguments, "-EncodedCommand");
            commands.Add(index >= 0 ? Encoding.Unicode.GetString(Convert.FromBase64String(arguments[index + 1])) : string.Join(" ", arguments));
        }
        plans.Add(new { Path = Relative(plan), Commands = commands });
    }
    Capture(Path.Combine(dir, "manifest.json"), "historical-manifest");
}
string runtime = Path.Combine(workspace, "runtime");
string obj = Path.Combine(runtime, "artifacts", "obj");
var caches = Directory.Exists(obj) ? Directory.EnumerateFiles(obj, "CMakeCache.txt", SearchOption.AllDirectories).ToArray() : [];
foreach (string cache in caches)
{
    Scan(cache, "current-cache-not-historical", @"python|crossgen|interpreter|feature|CMAKE_HOME_DIRECTORY|CMAKE_BUILD_TYPE|toolchain");
    string dir = Path.GetDirectoryName(cache)!;
    foreach (string name in new[] { "cmake_cmd_line.txt", "build.ninja", "CMakeFiles/rules.ninja", "CMakeFiles/CMakeConfigureLog.yaml" })
        Scan(Path.Combine(dir, name), "current-generated-build-not-execution-proof", @"python|\.py\b|crossgen|genRuntime|genEvent|genEtw|genLttng|genDummy|genheaders|genrc|skip|cached");
}
foreach (string path in new[] {
    "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.CoreCLR.sfxproj",
    "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.CoreCLR.props",
    "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.props",
    "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.props",
    "src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.CoreCLR.platformmanifest.txt",
    "src/installer/pkg/sfx/bundle/Microsoft.NETCore.App.Bundle.bundleproj",
    "src/installer/pkg/sfx/Directory.Build.targets", "src/installer/pkg/sfx/Directory.Build.props",
    "eng/liveBuilds.targets", "eng/Versions.props" })
    Scan(Path.Combine(runtime, path), "official-source", interesting + "|Include=|Exclude=|Remove=");
string installer = Path.Combine(runtime, "src", "installer");
foreach (string file in Directory.EnumerateFiles(installer, "*", SearchOption.AllDirectories)
    .Where(p => new[] { ".targets", ".props", ".sfxproj", ".txt" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase)))
{
    if (Regex.IsMatch(File.ReadAllText(file), @"ValidatePlatformManifest|Verify.*(Version|Manifest)|PlatformManifestFileEntry|Missing.*[Mm]anifest"))
        Scan(file, "official-validation-source", interesting + "|Condition=|Include=|Exclude=|Remove=");
}
foreach (string file in Directory.EnumerateFiles(Path.Combine(builder, "Builder"), "*", SearchOption.TopDirectoryOnly)
    .Where(p => new[] { ".cs", ".targets", ".rc" }.Contains(Path.GetExtension(p)))) Capture(file, "builder-source");
string reference = Path.Combine(workspace, "dotnet");
var installations = new List<Installation>();
foreach (string root in new[] { reference, Path.Combine(builder, "artifacts", "runtime-win-x86"), Path.Combine(builder, "artifacts", "runtime-win-x64") })
{
    var versions = new List<VersionInventory>();
    foreach (string component in new[] { "host/fxr", "shared/Microsoft.NETCore.App" })
    {
        string basePath = Path.Combine(root, component);
        if (!Directory.Exists(basePath)) continue;
        foreach (string version in Directory.GetDirectories(basePath).Order()) versions.Add(new(component, Path.GetFileName(version), Inventory(version)));
    }
    var rootFiles = Directory.GetFiles(root).Where(p => new[] { "dotnet.exe", "LICENSE.txt", "ThirdPartyNotices.txt" }.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
        .Select(p => new Entry(Path.GetFileName(p), Relative(p), new FileInfo(p).Length, Hash(p), Machine(p))).ToArray();
    installations.Add(new(Relative(root), Directory.GetDirectories(root).Select(Path.GetFileName).ToArray(), rootFiles, versions.ToArray()));
}
Save("inventories.json", installations);
var comparisons = new List<Comparison>();
var fullDiff = new StringBuilder("# 完整逐文件差异（版本目录归一为 <version>；参考实际为 x86；x64 对参考仅布局可比）\n");
foreach (Installation target in installations.Skip(1))
foreach (VersionInventory referenceVersion in installations[0].Versions.Where(x => x.Component == "shared/Microsoft.NETCore.App"))
{
    VersionInventory targetVersion = target.Versions.Single(x => x.Component == referenceVersion.Component);
    var referenceFiles = installations[0].RootFiles.Concat(referenceVersion.Files.Select(e => e with { Path = "shared/Microsoft.NETCore.App/<version>/" + e.Path }))
        .Concat(installations[0].Versions.Where(v => v.Component == "host/fxr" && v.Version == referenceVersion.Version).SelectMany(v => v.Files.Select(e => e with { Path = "host/fxr/<version>/" + e.Path }))).ToArray();
    var targetFiles = target.RootFiles.Concat(targetVersion.Files.Select(e => e with { Path = "shared/Microsoft.NETCore.App/<version>/" + e.Path }))
        .Concat(target.Versions.Where(v => v.Component == "host/fxr" && v.Version == targetVersion.Version).SelectMany(v => v.Files.Select(e => e with { Path = "host/fxr/<version>/" + e.Path }))).ToArray();
    var left = referenceFiles.ToDictionary(e => e.Path, StringComparer.OrdinalIgnoreCase);
    var right = targetFiles.ToDictionary(e => e.Path, StringComparer.OrdinalIgnoreCase);
    var differences = left.Keys.Union(right.Keys, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Select(path =>
    {
        left.TryGetValue(path, out Entry? l); right.TryGetValue(path, out Entry? r);
        return new Difference(path, l is null ? "extra" : r is null ? "missing" : l.Sha256 == r.Sha256 ? "common-equal" : "common-differing", l, r);
    }).ToArray();
    var counts = differences.GroupBy(x => x.Status).ToDictionary(g => g.Key, g => g.Count());
    comparisons.Add(new(target.Root, referenceVersion.Version, targetVersion.Version, counts, differences));
    fullDiff.AppendLine($"\n## {target.Root} vs reference {referenceVersion.Version}\n");
    foreach (var d in differences) fullDiff.AppendLine($"{d.Status}\t{d.Path}\trefLength={d.Reference?.Length}\ttargetLength={d.Target?.Length}\trefSHA256={d.Reference?.Sha256}\ttargetSHA256={d.Target?.Sha256}");
}
Save("comparisons.json", comparisons);
File.WriteAllText(Path.Combine(output, "full-diff.tsv"), fullDiff.ToString());
var mapping = new List<object>();
foreach (Installation target in installations.Skip(1))
{
    string root = Path.Combine(workspace, target.Root);
    string arch = target.Root.EndsWith("x86", StringComparison.Ordinal) ? "x86" : "x64";
    var files = Inventory(root);
    string manifestPath = root + ".manifest.json";
    Capture(manifestPath, "published-manifest");
    using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    var expected = manifest.RootElement.GetProperty("FilesSha256").EnumerateObject().ToDictionary(p => p.Name.Replace('\\', '/'), p => p.Value.GetString(), StringComparer.OrdinalIgnoreCase);
    var mismatches = files.Where(f => !expected.TryGetValue(f.Path, out string? hash) || hash != f.Sha256).Select(f => f.Path).ToArray();
    var missingManifest = expected.Keys.Except(files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase).ToArray();
    string originalManifest = manifest.RootElement.GetProperty("OriginalManifest").GetString()!;
    using var original = JsonDocument.Parse(File.ReadAllText(originalManifest));
    var originalLayout = original.RootElement.GetProperty("Details").GetProperty("Layout");
    var originalExpected = originalLayout.EnumerateObject().ToDictionary(p => p.Name.Replace('\\', '/'), p => p.Value.GetString(), StringComparer.OrdinalIgnoreCase);
    var originalMismatches = files.Where(f => !originalExpected.TryGetValue(f.Path, out string? h) || h != f.Sha256).Select(f => f.Path).Concat(originalExpected.Keys.Except(files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase)).ToArray();
    string framework = Path.Combine(root, "shared", "Microsoft.NETCore.App", target.Versions.Single(v => v.Component == "shared/Microsoft.NETCore.App").Version);
    string deps = Path.Combine(framework, "Microsoft.NETCore.App.deps.json");
    Capture(deps, "product-deps"); Capture(Path.Combine(framework, "Microsoft.NETCore.App.runtimeconfig.json"), "product-runtimeconfig");
    using var depsDoc = JsonDocument.Parse(File.ReadAllText(deps));
    var assetMapping = new List<object>();
    foreach (var tfm in depsDoc.RootElement.GetProperty("targets").EnumerateObject())
    foreach (var library in tfm.Value.EnumerateObject())
    foreach (var group in library.Value.EnumerateObject().Where(p => p.Name is "runtime" or "native" or "resources" or "runtimeTargets"))
    foreach (var asset in group.Value.EnumerateObject())
    {
        string name = Path.GetFileName(asset.Name);
        if (name == "_._") continue;
        string destination = group.Name == "resources" ? Path.Combine(framework, Path.GetFileName(Path.GetDirectoryName(asset.Name)) ?? "", name) : Path.Combine(framework, name);
        assetMapping.Add(new { Target = tfm.Name, Library = library.Name, Group = group.Name, Asset = asset.Name, Destination = Relative(destination), Exists = File.Exists(destination) });
    }
    var buildManifests = Directory.Exists(obj) ? Directory.EnumerateFiles(obj, "*", SearchOption.AllDirectories)
        .Where(p => p.Contains("win-" + arch, StringComparison.OrdinalIgnoreCase) && (p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && p.Contains("manifest", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && p.Contains("FilesToPublish", StringComparison.OrdinalIgnoreCase))).ToArray() : [];
    foreach (string file in buildManifests) Capture(file, "current-official-generated-manifest");
    mapping.Add(new { Target = target.Root, FileCount = files.Length, ManifestFileCount = expected.Count, Mismatches = mismatches, Missing = missingManifest, OriginalManifestMismatches = originalMismatches, DepsAssets = assetMapping, OfficialGeneratedManifests = buildManifests.Select(Relative).ToArray() });
}
var additionalRoots = new[] { Path.Combine(runtime, "eng"), Path.Combine(runtime, "src", "coreclr", "cmake"), Path.Combine(runtime, "src", "coreclr", "interpreter") };
foreach (string sourceRoot in additionalRoots.Where(Directory.Exists))
foreach (string file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).Where(p => new[] { ".props", ".targets", ".cmake", ".txt" }.Contains(Path.GetExtension(p))))
    if (Regex.IsMatch(File.ReadAllText(file), "EnableSourceControlManagerQueries|FEATURE_INTERPRETER|CLR_INTERPRETER"))
        Scan(file, "feature-and-version-conditions", "EnableSourceControlManagerQueries|FEATURE_INTERPRETER|CLR_INTERPRETER|Condition=");
string assetsRoot = Path.Combine(runtime, "artifacts", "obj");
var sdkPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (string assetsFile in Directory.EnumerateFiles(assetsRoot, "project.assets.json", SearchOption.AllDirectories).Where(p => p.Contains("Microsoft.NETCore.App.Runtime.CoreCLR", StringComparison.OrdinalIgnoreCase)))
{
    using var assets = JsonDocument.Parse(File.ReadAllText(assetsFile));
    foreach (var library in assets.RootElement.GetProperty("libraries").EnumerateObject().Where(p => p.Name.StartsWith("Microsoft.DotNet.SharedFramework.Sdk/", StringComparison.OrdinalIgnoreCase)))
    foreach (var folder in assets.RootElement.GetProperty("packageFolders").EnumerateObject())
    {
        if (!library.Value.TryGetProperty("path", out var packagePath)) continue;
        string package = Path.Combine(folder.Name, packagePath.GetString()!);
        if (Directory.Exists(package)) sdkPackages.Add(package);
    }
}
foreach (string packageBase in new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"), Path.Combine(runtime, ".packages") }.Where(Directory.Exists))
{
    string sdkRoot = Path.Combine(packageBase, "microsoft.dotnet.sharedframework.sdk");
    if (Directory.Exists(sdkRoot)) foreach (string version in Directory.GetDirectories(sdkRoot)) sdkPackages.Add(version);
}
foreach (string package in sdkPackages)
foreach (string file in Directory.EnumerateFiles(package, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".props", StringComparison.OrdinalIgnoreCase)))
    Scan(file, "official-sharedframework-sdk-validation", "Validat|Verif|PlatformManifest|FilesToPublish|PublishToDisk|Version|Error|Condition=");
var brief = new StringBuilder();
brief.AppendLine("INSTALLATIONS");
foreach (var i in installations)
{
    brief.AppendLine(i.Root + " root=" + string.Join(",", i.RootFiles.Select(f => f.Path + ":" + f.Machine)));
    foreach (var v in i.Versions) brief.AppendLine($"  {v.Component}/{v.Version} files={v.Files.Length} nativeMachines={string.Join(',', v.Files.Where(f => f.Path is "hostfxr.dll" or "coreclr.dll").Select(f => f.Machine))}");
}
brief.AppendLine("ALL COMPARISON COUNTS AND ALL MISSING/EXTRA");
foreach (var c in comparisons)
{
    brief.AppendLine($"{c.Target} vs {c.ReferenceVersion}: {string.Join(", ", c.Counts.Select(k => k.Key + "=" + k.Value))}");
    foreach (var d in c.Files.Where(d => d.Status is "missing" or "extra")) brief.AppendLine($"  {d.Status}: {d.Path}");
}
brief.AppendLine("PYTHON IN SUCCESSFUL BUILD LOGS");
foreach (string id in new[] { "baseline-win7-x64-20261030103039-a5261b67dab040c093284f25151f7fc8", "baseline-win7-x86-20261030101414-e80ee875e78744db96726538b7a076f5" })
{
    string log = Path.Combine(builder, "artifacts", id, "0.log");
    var lines = File.ReadAllLines(log);
    for (int n = 0; n < lines.Length; n++) if (Regex.IsMatch(lines[n], @"python|\.py\b|no work to do|\[\d+/\d+\].*(Generat|resource)", RegexOptions.IgnoreCase)) brief.AppendLine($"{id}/0.log:{n + 1}: {lines[n]}");
}
brief.AppendLine("CACHE PYTHON AND FEATURES (CURRENT, NOT HISTORICAL)");
foreach (string cache in caches.Where(p => p.Contains("windows.x64.Release") || p.Contains("windows.x86.Release")))
foreach (string line in File.ReadLines(cache).Where(l => Regex.IsMatch(l, "python|interpreter|FEATURE_EVENT|FEATURE_PERF", RegexOptions.IgnoreCase))) brief.AppendLine(Relative(cache) + ": " + line);
File.WriteAllText(Path.Combine(output, "findings.txt"), brief.ToString());
Save("missing-classification.json", comparisons.Select(c => new { c.Target, c.ReferenceVersion, Missing = c.Files.Where(d => d.Status == "missing").Select(d => new { d.Path, Classification = d.Path.EndsWith("/.version", StringComparison.Ordinal) ? "conditional metadata: CreateDotVersionFile requires EnableSourceControlManagerQueries=true; not a runtime assembly" : d.Path.Contains("mscordaccore_", StringComparison.Ordinal) ? "architecture and build-version-qualified DAC alias; unqualified mscordaccore.dll exists; compare target alias" : d.Path.Contains("DiaSymReader.Native", StringComparison.Ordinal) ? "architecture-specific DiaSymReader name; compare target x86/amd64 counterpart" : c.ReferenceVersion.StartsWith("10.", StringComparison.Ordinal) ? "requires further investigation" : "cross-major-version distribution difference; not a same-version omission verdict" }) }));
Save("mapping.json", mapping);
var mappingSummary = mapping.Select(item =>
{
    var element = JsonSerializer.SerializeToElement(item);
    return new { Target = element.GetProperty("Target").GetString(), FileCount = element.GetProperty("FileCount").GetInt32(), ManifestMismatchCount = element.GetProperty("Mismatches").GetArrayLength(), ManifestMissingCount = element.GetProperty("Missing").GetArrayLength(), OriginalMismatchCount = element.GetProperty("OriginalManifestMismatches").GetArrayLength(), DepsAssetCount = element.GetProperty("DepsAssets").GetArrayLength(), MissingDeps = element.GetProperty("DepsAssets").EnumerateArray().Where(e => !e.GetProperty("Exists").GetBoolean()).ToArray(), GeneratedManifests = element.GetProperty("OfficialGeneratedManifests") };
}).ToArray();
Save("mapping-summary.json", mappingSummary);
Save("host-fxr-comparisons.json", installations.Skip(1).SelectMany(target => installations[0].Versions.Where(v => v.Component == "host/fxr").Select(referenceVersion => new { target.Root, ReferenceVersion = referenceVersion.Version, TargetVersion = target.Versions.Single(v => v.Component == "host/fxr").Version, ReferenceFiles = referenceVersion.Files, TargetFiles = target.Versions.Single(v => v.Component == "host/fxr").Files })));
Save("sdk-validation-package-paths.json", sdkPackages);
string acceptance = Path.Combine(builder, "artifacts", "acceptance-baseline-20261030-passed", "evidence");
if (Directory.Exists(acceptance)) foreach (string file in Directory.GetFiles(acceptance)) Capture(file, "historical-acceptance-not-completeness-proof");
var checks = new Dictionary<string, bool>
{
    ["two-product-inventories"] = installations.Count == 3,
    ["all-reference-framework-comparisons"] = comparisons.Count == installations[0].Versions.Count(v => v.Component == "shared/Microsoft.NETCore.App") * 2,
    ["comparison-partitions"] = comparisons.All(c => c.Files.Count(d => d.Target is not null) == installations.Single(i => i.Root == c.Target).RootFiles.Length + installations.Single(i => i.Root == c.Target).Versions.Sum(v => v.Files.Length)),
    ["published-and-original-hashes-match"] = mappingSummary.All(m => m.ManifestMismatchCount == 0 && m.ManifestMissingCount == 0 && m.OriginalMismatchCount == 0),
    ["all-deps-assets-present"] = mappingSummary.All(m => m.MissingDeps.Length == 0)
};
Save("audit-self-checks.json", checks);
if (checks.Any(c => !c.Value)) throw new InvalidOperationException("Integrity audit self-check failed; inspect audit-self-checks.json.");
Save("decoded-commands.json", plans);
Save("evidence-index.json", evidence);
Save("evidence-lines.json", textHits);
Save("summary.json", new { AuditUtc = DateTimeOffset.UtcNow, Scope = "runtime only; no SDK/packs/aspnet/windowsdesktop; read-only products; no runtime rebuild", CurrentCacheCount = caches.Length, Comparisons = comparisons.Select(c => new { c.Target, c.ReferenceVersion, c.TargetVersion, c.Counts }), Mapping = mapping });
Console.WriteLine(JsonSerializer.Serialize(new { Output = output, CacheCount = caches.Length, Comparisons = comparisons.Select(c => new { c.Target, c.ReferenceVersion, c.TargetVersion, c.Counts }) }, jsonOptions));

internal sealed record Entry(string Path, string Source, long Length, string Sha256, string Machine);
internal sealed record VersionInventory(string Component, string Version, Entry[] Files);
internal sealed record Installation(string Root, string?[] TopDirectories, Entry[] RootFiles, VersionInventory[] Versions);
internal sealed record Difference(string Path, string Status, Entry? Reference, Entry? Target);
internal sealed record Comparison(string Target, string ReferenceVersion, string TargetVersion, Dictionary<string, int> Counts, Difference[] Files);
