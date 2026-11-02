using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

if (args.Length == 2 && args[0] == "--inspect")
{
    try { Console.WriteLine(JsonSerializer.Serialize(ReadImports(args[1]))); return 0; }
    catch (Exception ex) when (ex is BadImageFormatException or ArgumentOutOfRangeException or OverflowException or IndexOutOfRangeException) { Console.Error.WriteLine("Malformed PE imports: " + ex.Message); return 1; }
}
if (args.Length == 2 && args[0] == "--map-check") return File.Exists(args[1]) && File.ReadLines(args[1]).Any(l => l.Contains("YY_Thunks_for_Win7.obj", StringComparison.OrdinalIgnoreCase)) ? 0 : 1;
string builder = Path.GetFullPath(args[0]);
string runtime = Path.GetFullPath(args[1]);
bool published = args.Contains("--published");
bool allowUnverifiedThirdParty = args.Contains("--allow-unverified-third-party");
if (published && !allowUnverifiedThirdParty) throw new InvalidOperationException("Explicit --allow-unverified-third-party required for published candidates");
string output = Path.Combine(builder, "artifacts", "yythunks-audit");
Directory.CreateDirectory(output);
var json = new JsonSerializerOptions { WriteIndented = true };
string Hash(string p) { using var s = File.OpenRead(p); return Convert.ToHexString(SHA256.HashData(s)); }
var runs = new Dictionary<string, string> { ["x86"] = "build-win7-x86-20261030125311-7969919196624435bcc524b61ab39253", ["x64"] = "build-win7-x64-20261030125619-95c42902e51446dcb62ce1860fe89243" };
var summary = new StringBuilder("YY-Thunks audit; Win7Unverified. No authoritative Win7 import allowlist. All imports require certification, including dynamically resolved APIs not visible here.\n");
bool failed = false;
var thirdParty = new List<object>();
var gates = new List<object>();
foreach (var (arch, run) in runs)
{
    string source = published ? Path.Combine(builder, "artifacts", "runtime-yythunks-win-" + arch) : Path.Combine(builder, "artifacts", run, "runtime");
    var rows = new List<object>();
    var evidence = new List<object>();
    var maps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    bool selfBuiltPassed = true;
    using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(builder, "artifacts", run, "manifest.json")));
    string obj = manifest.RootElement.GetProperty("Options").GetProperty("Obj").GetString()!;
    byte[] coff = File.ReadAllBytes(obj);
    bool objectMatches = coff.Length >= 20 && BitConverter.ToUInt16(coff, 0) == (arch == "x64" ? 0x8664 : 0x14c)
        && Hash(obj) == manifest.RootElement.GetProperty("ThunkObject").GetProperty("Sha256").GetString();
    selfBuiltPassed &= objectMatches;
    evidence.Add(new { Object = Path.GetRelativePath(builder, obj), Sha256 = Hash(obj), Machine = BitConverter.ToUInt16(coff, 0), MatchesBuildManifest = objectMatches });
    foreach (string root in new[] { Path.Combine(runtime, "artifacts", "obj", "coreclr", $"windows.{arch}.Release"), Path.Combine(runtime, "artifacts", "obj", $"win-{arch}.Release", "corehost"), Path.Combine(runtime, "artifacts", "obj", "native", $"net10.0-windows-Release-{arch}") })
    foreach (string map in Directory.GetFiles(root, "*.map", SearchOption.AllDirectories))
    {
        string relative = Path.GetRelativePath(Path.Combine(runtime, "artifacts", "obj"), map);
        string copy = Path.Combine(output, arch, "maps", relative);
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.Copy(map, copy, true);
        string[] lines = File.ReadAllLines(map).Where(l => l.Contains("YY_Thunks_for_Win7.obj", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (lines.Length > 0) maps[Path.GetFileNameWithoutExtension(map)] = map;
        evidence.Add(new { Map = Path.GetRelativePath(output, copy), Sha256 = Hash(copy), ThunkSymbolLines = lines });
    }
    foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories).Where(p => Path.GetExtension(p) is ".dll" or ".exe"))
    {
        using var stream = File.OpenRead(file);
        using var pe = new PEReader(stream);
        if (pe.PEHeaders.CorHeader is not null) continue;
        var header = pe.PEHeaders.PEHeader ?? throw new BadImageFormatException(file);
        string relative = Path.GetRelativePath(source, file);
        string baseline = Path.Combine(builder, "artifacts", "runtime-win-" + arch, relative);
        var imports = ReadImports(file);
        var before = File.Exists(baseline) ? ReadImports(baseline) : [];
        bool exempt = (Path.GetFileNameWithoutExtension(file) is "mscorrc" or "clretwrc") && header.AddressOfEntryPoint == 0 && imports.Length == 0 && pe.PEHeaders.SectionHeaders.All(s => (s.SectionCharacteristics & SectionCharacteristics.MemExecute) == 0);
        string name = Path.GetFileNameWithoutExtension(file);
        bool dacAlias = name.StartsWith("mscordaccore_", StringComparison.OrdinalIgnoreCase) && Hash(file) == Hash(Path.Combine(Path.GetDirectoryName(file)!, "mscordaccore.dll"));
        bool linked = maps.TryGetValue(dacAlias ? "mscordaccore" : name, out string? targetMap);
        bool external = name is "Microsoft.DiaSymReader.Native.x86" or "Microsoft.DiaSymReader.Native.amd64" or "msquic";
        bool outputMatches = false, executedLink = false;
        if (targetMap is not null)
        {
            string binary = Path.ChangeExtension(targetMap, Path.GetExtension(file));
            outputMatches = File.Exists(binary) && Hash(binary) == Hash(file);
            DirectoryInfo? buildRoot = new(Path.GetDirectoryName(targetMap)!);
            while (buildRoot is not null && !File.Exists(Path.Combine(buildRoot.FullName, "build.ninja"))) buildRoot = buildRoot.Parent;
            if (buildRoot is not null)
            {
                string ninja = Path.Combine(buildRoot.FullName, "build.ninja"), log = Path.Combine(buildRoot.FullName, ".ninja_log");
                string target = Path.GetRelativePath(buildRoot.FullName, binary).Replace('\\', '/');
                executedLink = File.Exists(log) && File.ReadLines(log).Any(l => l.Split('\t') is var fields && fields.Length >= 5 && fields[3].Replace('\\', '/') == target)
                    && File.ReadLines(ninja).Any(l => l.StartsWith("build ", StringComparison.Ordinal) && l.Contains("YY_Thunks_for_Win7.obj", StringComparison.Ordinal) && l.Replace('\\', '/').Contains(target, StringComparison.OrdinalIgnoreCase));
                evidence.Add(new { File = relative, OutputMatches = outputMatches, ExecutedLink = executedLink, BuildGraphSha256 = Hash(ninja), ExecutionLogSha256 = File.Exists(log) ? Hash(log) : null, Map = Path.GetRelativePath(output, Path.Combine(output, arch, "maps", Path.GetRelativePath(Path.Combine(runtime, "artifacts", "obj"), targetMap))) });
                foreach (string input in new[] { ninja, log }.Where(File.Exists))
                {
                    string copy = Path.Combine(output, arch, "link-inputs", Path.GetRelativePath(Path.Combine(runtime, "artifacts", "obj"), input));
                    Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                    File.Copy(input, copy, true);
                }
            }
        }
        if (external)
        {
            string id = name == "msquic" ? "microsoft.native.quic.msquic.schannel" : "microsoft.diasymreader.native";
            string version = name == "msquic" ? "2.4.16" : "17.10.0-beta1.24272.1";
            string packageRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages", id, version);
            string packageFile = name == "msquic" ? Path.Combine(packageRoot, "build", "native", "bin", arch, "msquic.dll") : Path.Combine(packageRoot, "runtimes", "win", "native", Path.GetFileName(file));
            bool packageMatches = File.Exists(packageFile) && Hash(packageFile) == Hash(file);
            if (!packageMatches) selfBuiltPassed = false;
            thirdParty.Add(new { Architecture = arch, File = relative, Sha256 = Hash(file), PackageId = id, PackageVersion = version, PackageAsset = Path.GetRelativePath(packageRoot, packageFile), PackageAssetMatches = packageMatches, PackageSha512 = File.Exists(Path.Combine(packageRoot, id + "." + version + ".nupkg.sha512")) ? File.ReadAllText(Path.Combine(packageRoot, id + "." + version + ".nupkg.sha512")).Trim() : null, Precompiled = true, Relinked = false, Compatibility = "Win7Unverified", Imports = imports, SuspectedBlockers = imports.Where(i => i.Contains("InterlockedPushListSListEx", StringComparison.Ordinal) || i.Contains("QueryContextAttributesExW", StringComparison.Ordinal) || i.Contains("api-ms-win-crt-", StringComparison.Ordinal)).ToArray(), Risk = "No Win7 export allowlist or execution certification; dynamic API and QUIC TLS/debug paths remain unverified" });
        }
        bool matches = pe.PEHeaders.CoffHeader.Machine == (arch == "x64" ? Machine.Amd64 : Machine.I386);
        bool sameBaseline = File.Exists(baseline) && Hash(file) == Hash(baseline);
        if (!matches || (!exempt && !external && (!linked || sameBaseline || !outputMatches || !executedLink))) selfBuiltPassed = false;
        if (external || !selfBuiltPassed) failed = true;
        rows.Add(new { File = relative, Sha256 = Hash(file), Machine = pe.PEHeaders.CoffHeader.Machine.ToString(), Magic = header.Magic.ToString(), header.AddressOfEntryPoint, Subsystem = header.Subsystem.ToString(), OsVersion = $"{header.MajorOperatingSystemVersion}.{header.MinorOperatingSystemVersion}", SubsystemVersion = $"{header.MajorSubsystemVersion}.{header.MinorSubsystemVersion}", Imports = imports, Removed = before.Except(imports).ToArray(), Added = imports.Except(before).ToArray(), SameAsBaseline = sameBaseline, MapContainsThunkSymbols = linked, Exemption = exempt ? "NOENTRY resource-only DLL; no executable code or CRT" : null, Win7Certification = "Unverified" });
        summary.AppendLine($"{arch} {relative}: map={linked}, exempt={exempt}, baselineEqual={sameBaseline}, imports={imports.Length}");
        foreach (string import in imports) summary.AppendLine("  Uncertified " + import);
    }
    await File.WriteAllTextAsync(Path.Combine(output, arch + "-native-imports.json"), JsonSerializer.Serialize(rows, json));
    await File.WriteAllTextAsync(Path.Combine(output, arch + "-link-evidence.json"), JsonSerializer.Serialize(evidence, json));
    var inventory = Directory.GetFiles(source, "*", SearchOption.AllDirectories).ToDictionary(p => Path.GetRelativePath(source, p), Hash);
    var verified = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(builder, "artifacts", "acceptance-yythunks-verified", "evidence", arch + "-hashes.json")))!;
    bool sandboxMatches = inventory.OrderBy(p => p.Key).SequenceEqual(verified.OrderBy(p => p.Key));
    if (!sandboxMatches) { failed = true; selfBuiltPassed = false; }
    if (published)
    {
        using var release = JsonDocument.Parse(File.ReadAllText(source + ".manifest.json"));
        var recorded = release.RootElement;
        var hashes = recorded.GetProperty("FilesSha256").Deserialize<Dictionary<string, string>>()!;
        bool manifestMatches = recorded.GetProperty("BuildKind").GetString() == "candidate"
            && !recorded.GetProperty("YYThunksIntegrated").GetBoolean()
            && recorded.GetProperty("YYThunksIntegratedScope").GetString() == "SelfBuiltNativeTargets"
            && recorded.GetProperty("Compatibility").GetString() == "Win7Unverified"
            && recorded.GetProperty("StaticAuditExitCode").GetInt32() == 1
            && recorded.GetProperty("Architecture").GetString() == arch
            && hashes.OrderBy(p => p.Key).SequenceEqual(inventory.OrderBy(p => p.Key));
        if (!manifestMatches) { failed = true; selfBuiltPassed = false; }
        summary.AppendLine($"{arch} published manifest matches: {manifestMatches}");
    }
    gates.Add(new { Architecture = arch, SelfBuiltPassed = selfBuiltPassed, SandboxHashesMatch = sandboxMatches, FilesSha256 = inventory });
    summary.AppendLine($"{arch} sandbox full inventory hash matches: {sandboxMatches}");
}
if (published)
{
    foreach (string arch in runs.Keys)
    {
        using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(builder, "artifacts", "runtime-yythunks-win-" + arch + ".manifest.json")));
        bool thirdPartyMatches = System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(release.RootElement.GetProperty("ThirdPartyUncovered").GetRawText()),
            System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(thirdParty, json)));
        if (!thirdPartyMatches) throw new InvalidDataException("Published third-party manifest mismatch: " + arch);
        summary.AppendLine($"{arch} published third-party manifest matches: {thirdPartyMatches}; explicit allow-unverified-third-party={allowUnverifiedThirdParty}");
    }
}
await File.WriteAllTextAsync(Path.Combine(output, "report.txt"), summary.ToString());
await File.WriteAllTextAsync(Path.Combine(output, "status.json"), JsonSerializer.Serialize(new { Passed = !failed, Compatibility = "Win7Unverified", Scope = "Pure native DLL/EXE; normal and delay imports; MAP evidence; baseline differences; sandbox hashes" }, json));
await File.WriteAllTextAsync(Path.Combine(output, "candidate-gate.json"), JsonSerializer.Serialize(new { YYThunksIntegratedScope = "SelfBuiltNativeTargets", Compatibility = "Win7Unverified", StaticAuditExitCode = failed ? 1 : 0, Architectures = gates, ThirdPartyUncovered = thirdParty }, json));
Console.WriteLine(output);
return failed ? 1 : 0;

static string[] ReadImports(string path)
{
    using var s = File.OpenRead(path);
    using var pe = new PEReader(s);
    var h = pe.PEHeaders.PEHeader ?? throw new BadImageFormatException(path);
    byte[] bytes = File.ReadAllBytes(path);
    int Offset(uint rva)
    {
        foreach (var section in pe.PEHeaders.SectionHeaders)
            if (rva >= section.VirtualAddress && rva < (long)section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
                return checked((int)(rva - section.VirtualAddress + section.PointerToRawData));
        if (rva < h.SizeOfHeaders) return checked((int)rva);
        throw new BadImageFormatException("Invalid import RVA");
    }
    void Bounds(int o, int count) { if (o < 0 || count < 0 || o > bytes.Length - count) throw new BadImageFormatException("Truncated import table"); }
    uint U32(int o) { Bounds(o, 4); return BitConverter.ToUInt32(bytes, o); }
    string Name(uint rva) { int o = Offset(rva); int end = Array.IndexOf(bytes, (byte)0, o); if (end < 0) throw new BadImageFormatException("Unterminated import"); return Encoding.ASCII.GetString(bytes, o, end - o); }
    var result = new List<string>();
    foreach (bool delay in new[] { false, true })
    {
        var dir = delay ? h.DelayImportTableDirectory : h.ImportTableDirectory;
        if (dir.RelativeVirtualAddress == 0) continue;
        int start = Offset((uint)dir.RelativeVirtualAddress), size = delay ? 32 : 20;
        Bounds(start, dir.Size);
        if (dir.Size < size) throw new BadImageFormatException("Truncated import descriptor");
        uint Rva(uint value, bool isRva) => isRva ? value : checked((uint)(value - h.ImageBase));
        for (int d = start; d < start + dir.Size; d += size)
        {
            if (d > start + dir.Size - size) throw new BadImageFormatException("Truncated import descriptor");
            bool isRva = !delay || (U32(d) & 1) != 0;
            uint name = U32(d + (delay ? 4 : 12));
            if (name == 0) break;
            string dll = Name(Rva(name, isRva));
            uint thunk = U32(d + (delay ? 16 : 0));
            if (thunk == 0) thunk = U32(d + (delay ? 12 : 16));
            int width = h.Magic == PEMagic.PE32Plus ? 8 : 4;
            for (int o = Offset(Rva(thunk, isRva)); ; o += width)
            {
                Bounds(o, width);
                ulong value = width == 8 ? BitConverter.ToUInt64(bytes, o) : U32(o);
                if (value == 0) break;
                ulong flag = width == 8 ? 0x8000000000000000UL : 0x80000000UL;
                string symbol = (value & flag) != 0 ? "#" + (value & 0xffff) : Name(checked(Rva((uint)value, isRva) + 2));
                result.Add((delay ? "delay:" : "normal:") + dll + "!" + symbol);
            }
        }
    }
    return result.Order(StringComparer.Ordinal).ToArray();
}
