using XiaoXiIme.Cli;

namespace XiaoXiIme.Cli.Tests;

public sealed class IntegrationTestRunnerTests
{
    [Fact]
    public void ResolveManifestPathFindsManifestAboveExecutableDirectory()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();
        var executableDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "app", "cli")).FullName;

        var result = IntegrationTestRunner.ResolveManifestPath(null, executableDirectory);

        Assert.Equal(manifestPath, result);
    }

    [Fact]
    public void ResolveManifestPathFallsBackToCurrentDirectoryHierarchy()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();
        var currentDirectory = Directory.CreateDirectory(Path.Combine(directory.Path, "work", "child")).FullName;
        var unrelatedDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"XiaoXiIme.Cli.Tests-{Guid.NewGuid():N}")).FullName;
        try
        {
            var result = IntegrationTestRunner.ResolveManifestPath(null, unrelatedDirectory, currentDirectory);

            Assert.Equal(manifestPath, result);
        }
        finally
        {
            Directory.Delete(unrelatedDirectory, true);
        }
    }

    [Fact]
    public void ResolveManifestPathUsesExplicitManifestPath()
    {
        using var directory = new TemporaryDirectory();
        var manifestPath = directory.CreateManifest();

        var result = IntegrationTestRunner.ResolveManifestPath(manifestPath, Path.GetTempPath());

        Assert.Equal(manifestPath, result);
    }

    [Fact]
    public void ResolveManifestPathReturnsNullWhenManifestDoesNotExist()
    {
        using var directory = new TemporaryDirectory();

        var result = IntegrationTestRunner.ResolveManifestPath(null, directory.Path);

        Assert.Null(result);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"XiaoXiIme.Cli.Tests-{Guid.NewGuid():N}")).FullName;
        }

        public string Path { get; }

        public string CreateManifest()
        {
            var manifestPath = System.IO.Path.Combine(Path, IntegrationPayloadManifest.FileName);
            File.WriteAllText(manifestPath, "{}");
            return manifestPath;
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
