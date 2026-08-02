using System.Security.Cryptography;
using WinRemoteShell.Client;

namespace WinRemoteShell.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LargePushIntegrationTests
{
    private const long OneGibibyte = 1024L * 1024 * 1024;
    private const int BufferSize = 4 * 1024 * 1024;

    [TestMethod]
    [TestCategory("LargeTransfer")]
    public async Task WhenDirectoryLargerThanOneGiBIsPushedThenEntireTreeIsPreserved()
    {
        var root = Path.Join(AppContext.BaseDirectory, $"WinRemoteShell_LargePush_{Guid.NewGuid():N}");
        var source = Path.Join(root, "source");
        var target = Path.Join(root, "target");

        try
        {
            await CreateSourceTreeAsync(source);
            var sourceManifest = await CreateManifestAsync(source);

            await using (var host = await TestServerHost.StartAsync())
            {
                await PushClient.PushAsync(host.Address, source, target);
            }

            var targetManifest = await CreateManifestAsync(target);

            CollectionAssert.AreEqual(sourceManifest, targetManifest);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static async Task CreateSourceTreeAsync(string source)
    {
        var firstLevel = Path.Join(source, "first level");
        var secondLevel = Path.Join(firstLevel, "second-level");
        var thirdLevel = Path.Join(secondLevel, "第三层");
        Directory.CreateDirectory(Path.Join(source, "empty-root-directory"));
        Directory.CreateDirectory(Path.Join(thirdLevel, "empty-deep-directory"));

        await WriteRandomFileAsync(Path.Join(source, "root-large.bin"), 640L * 1024 * 1024, 2701);
        await WriteRandomFileAsync(Path.Join(firstLevel, "nested-large.bin"), 384L * 1024 * 1024, 2702);
        await WriteRandomFileAsync(Path.Join(thirdLevel, "deep file.bin"), 64L * 1024 * 1024, 2703);

        var totalLength = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Sum(path => new FileInfo(path).Length);
        if (totalLength <= OneGibibyte)
        {
            throw new InvalidOperationException($"Large push test data must exceed one GiB, but was {totalLength} bytes.");
        }
    }

    private static async Task WriteRandomFileAsync(string path, long length, int seed)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var random = new Random(seed);
        var buffer = new byte[BufferSize];
        await using var file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var remaining = length;
        while (remaining > 0)
        {
            var count = (int)Math.Min(buffer.Length, remaining);
            random.NextBytes(buffer.AsSpan(0, count));
            await file.WriteAsync(buffer.AsMemory(0, count));
            remaining -= count;
        }
    }

    private static async Task<string[]> CreateManifestAsync(string root)
    {
        var entries = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            entries.Add($"D:{NormalizeRelativePath(root, directory)}");
        }

        foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            await using var file = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(file);
            entries.Add($"F:{NormalizeRelativePath(root, filePath)}:{file.Length}:{Convert.ToHexString(hash)}");
        }

        entries.Sort(StringComparer.Ordinal);
        return entries.ToArray();
    }

    private static string NormalizeRelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');
}
