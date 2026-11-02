using Xunit;

namespace Builder.Tests;

public class ProgramTests
{
    [Theory]
    [InlineData("xp", "x86")]
    [InlineData("xp", "x64")]
    [InlineData("vista", "x86")]
    [InlineData("vista", "x64")]
    public async Task UnsupportedOperatingSystemsAreRejected(string os, string arch)
    {
        Assert.Equal(2, await Program.Main(["build", "--target-os", os, "--arch", arch]));
    }

    [Theory]
    [InlineData("build")]
    [InlineData("build", "--arch", "x64")]
    [InlineData("build", "--target-os", "win7")]
    [InlineData("plan", "--target-os", "win7", "--arch", "arm64")]
    [InlineData("plan", "--target-os", "win7", "--arch", "x64", "--timeout", "0")]
    [InlineData("plan", "--target-os", "win7", "--arch", "x64", "--arch", "x86")]
    [InlineData("plan", "--target-os", "win7", "--arch", "x64", "--unknown", "foo")]
    [InlineData("plan", "--target-os", "win7", "--arch", "x64", "--obj", "bad;path")]
    [InlineData("plan", "--target-os", "win7", "--arch", "x64", "--configuration", "other")]
    public async Task InvalidArgumentsAreRejected(params string[] args)
    {
        Assert.Equal(2, await Program.Main(args));
    }

    [Theory]
    [InlineData("unsafe&path")]
    [InlineData("unsafe|path")]
    [InlineData("unsafe^path")]
    [InlineData("unsafe%PATH%")]
    [InlineData("unsafe!path")]
    [InlineData("unsafe\"path")]
    [InlineData("unsafe(path)")]
    public async Task ShellMetacharactersAreRejected(string path)
    {
        Assert.Equal(2, await Program.Main(["plan", "--target-os", "win7", "--arch", "x64", "--runtime", path]));
    }

    [Theory]
    [InlineData("x86", "Release")]
    [InlineData("x64", "Checked")]
    public async Task PlanUsesAllThreeOfficialSubsetsWithSpaceSafePaths(string arch, string configuration)
    {
        string runtime = Path.Combine(Path.GetTempPath(), "runtime space ' quote " + Guid.NewGuid().ToString("N"));
        await Program.Main(["baseline", "--dry-run", "--target-os", "win7", "--arch", arch,
            "--configuration", configuration, "--runtime", runtime]);
        DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Builder.sln"))) root = root.Parent;
        string artifacts = Path.Combine(root!.FullName, "artifacts");
        string manifest = Directory.EnumerateFiles(artifacts, "manifest.json", SearchOption.AllDirectories)
            .First(path => System.Text.Json.JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("Options").GetProperty("Runtime").GetString() == runtime);
        using var plan = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(manifest)!, "plan.json")));
        string[] scripts = plan.RootElement.GetProperty("OfficialScripts").EnumerateArray().Select(s => s.GetString()!).ToArray();
        Assert.Equal(new[] { "clr.native", "libs.native", "host.native" }, scripts.Select(script =>
            script.Split(" -subset ")[1].Split(' ')[0]).ToArray());
    }

    [Theory]
    [InlineData("x86")]
    [InlineData("x64")]
    public async Task FullRuntimeStillRequiresThunkObject(string arch)
    {
        Assert.Equal(2, await Program.Main(["build", "--full-runtime", "--target-os", "win7", "--arch", arch,
            "--obj", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.obj")]));
    }

    [Fact]
    public async Task EmptyArgumentsAreRejected() => Assert.Equal(2, await Program.Main([]));

    [Fact]
    public async Task HelpSucceedsWithoutTarget() => Assert.Equal(0, await Program.Main(["--help"]));

    [Fact]
    public async Task InspectRequiresFile() => Assert.Equal(2, await Program.Main(["inspect", "--target-os", "win7", "--arch", "x64"]));

    [Fact]
    public async Task ManagedAssemblyIsNotAcceptedAsNativeCompatibility()
    {
        Assert.Equal(3, await Program.Main(["inspect", "--target-os", "win7", "--arch", "x64", "--file", typeof(Program).Assembly.Location]));
    }

    [Theory]
    [InlineData("x86")]
    [InlineData("x64")]
    public async Task MissingObjectBlocksPlan(string arch)
    {
        Assert.Equal(2, await Program.Main(["plan", "--target-os", "win7", "--arch", arch,
            "--obj", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing object.obj")]));
    }
}
