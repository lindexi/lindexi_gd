using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Builder.Tests;

public class ThunkPackageTests
{
    [Theory]
    [InlineData("x86")]
    [InlineData("x64")]
    public async Task DefaultObjectComesFromRestoredPackage(string arch)
    {
        using var manifest = await RunAsync(arch);
        var source = manifest.RootElement.GetProperty("ThunkObject");
        string path = manifest.RootElement.GetProperty("Options").GetProperty("Obj").GetString()!;
        using var package = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "yy-thunks", "package.json")));
        Assert.Equal(new[] { "NuGetPackage", "YY-Thunks", package.RootElement.GetProperty("PackageVersion").GetString(), $"build/native/objs/{arch}/YY_Thunks_for_Win7.obj", Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) },
            new[] { source.GetProperty("Source").GetString(), source.GetProperty("PackageId").GetString(), source.GetProperty("PackageVersion").GetString(), source.GetProperty("PackagePath").GetString(), source.GetProperty("Sha256").GetString() });
    }

    [Theory]
    [InlineData("x86", 0x14c)]
    [InlineData("x64", 0x8664)]
    public async Task RestoredObjectMatchesArchitecture(string arch, int machine)
    {
        using var manifest = await RunAsync(arch);
        byte[] data = File.ReadAllBytes(manifest.RootElement.GetProperty("Options").GetProperty("Obj").GetString()!);
        Assert.Equal(machine, BinaryPrimitives.ReadUInt16LittleEndian(data));
    }

    [Theory]
    [InlineData("x86")]
    [InlineData("x64")]
    public async Task PackageFixObjIsNotIndependentlyVerified(string arch)
    {
        using var manifest = await RunAsync(arch);
        Assert.Equal("NotIndependentlyVerified", manifest.RootElement.GetProperty("ThunkObject").GetProperty("FixObj").GetString());
    }

    [Theory]
    [InlineData("x86", "doctor")]
    [InlineData("x64", "doctor")]
    [InlineData("x86", "plan")]
    [InlineData("x64", "plan")]
    [InlineData("x86", "build")]
    [InlineData("x64", "build")]
    public async Task OfficialPackageCanPrepareRuntimeBuildWithoutLinking(string arch, string command)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Builder.sln"))) root = root.Parent;
        string runtime = Path.GetFullPath(Path.Combine(root!.FullName, "..", "runtime"));
        int exit = await Program.Main([command, "--target-os", "win7", "--arch", arch, "--runtime", runtime, "--dry-run"]);
        Assert.Equal(0, exit);
    }

    [Theory]
    [InlineData("x86", "x64")]
    [InlineData("x64", "x86")]
    public async Task WrongArchitectureObjectIsBlocked(string arch, string other)
    {
        using var manifest = await RunAsync(arch, "--obj", PackageObject(other));
        Assert.Contains(manifest.RootElement.GetProperty("Details").GetProperty("Blockers").EnumerateArray(),
            item => item.GetString()!.Contains("COFF", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExplicitObjectOverridesYYAndIsNotAttributedToPackage()
    {
        using var manifest = await RunAsync("x64", "--obj", PackageObject("x64"), "--yy", Path.Combine(Path.GetTempPath(), "unused"));
        Assert.Equal("ExplicitObject", manifest.RootElement.GetProperty("ThunkObject").GetProperty("Source").GetString());
    }

    [Fact]
    public async Task ExplicitYYDirectoryRetainsLegacyObjectLocation()
    {
        string yy = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using var manifest = await RunAsync("x86", "--yy", yy);
        Assert.Equal(Path.Combine(yy, "objs", "x86", "YY_Thunks_for_Win7.obj"), manifest.RootElement.GetProperty("Options").GetProperty("Obj").GetString());
    }

    [Fact]
    public async Task MissingObjectHasActionableFailure()
    {
        using var manifest = await RunAsync("x64", "--obj", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.obj"));
        Assert.Contains(manifest.RootElement.GetProperty("Details").GetProperty("Blockers").EnumerateArray(),
            item => item.GetString()!.Contains("--obj", StringComparison.Ordinal));
    }

    private static string PackageObject(string arch) => Path.Combine(AppContext.BaseDirectory, "yy-thunks", arch, "YY_Thunks_for_Win7.obj");

    private static async Task<JsonDocument> RunAsync(string arch, params string[] extra)
    {
        string runtime = Path.Combine(Path.GetTempPath(), "thunk-test-" + Guid.NewGuid().ToString("N"));
        await Program.Main(["doctor", "--target-os", "win7", "--arch", arch, "--runtime", runtime, .. extra]);
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Builder.sln"))) root = root.Parent;
        string path = Directory.EnumerateFiles(Path.Combine(root!.FullName, "artifacts"), "manifest.json", SearchOption.AllDirectories)
            .First(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("Options").GetProperty("Runtime").GetString() == runtime);
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
