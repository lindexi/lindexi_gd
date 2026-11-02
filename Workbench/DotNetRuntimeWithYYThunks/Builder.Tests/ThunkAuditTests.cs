using System.Buffers.Binary;
using System.Diagnostics;
using Xunit;

namespace Builder.Tests;

public class ThunkAuditTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MalformedImportDescriptorFails(bool delay)
    {
        byte[] image = new byte[512];
        image[0] = 0x4d; image[1] = 0x5a;
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x3c), 0x80);
        image[0x80] = 0x50; image[0x81] = 0x45;
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(0x84), 0x14c);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(0x94), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(0x98), 0x10b);
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x98 + 60), 512);
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(0x98 + 92), 16);
        int directory = 0x98 + 96 + (delay ? 13 : 1) * 8;
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(directory), 500);
        BinaryPrimitives.WriteInt32LittleEndian(image.AsSpan(directory + 4), delay ? 32 : 20);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        await File.WriteAllBytesAsync(path, image);
        Assert.Equal(1, await RunAsync("--inspect", path));
    }

    [Fact]
    public async Task MissingMapFails()
    {
        Assert.Equal(1, await RunAsync("--map-check", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".map")));
    }

    [Fact]
    public async Task MapWithoutThunkFails()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".map");
        await File.WriteAllTextAsync(path, "Other.obj");
        Assert.Equal(1, await RunAsync("--map-check", path));
    }

    private static async Task<int> RunAsync(string mode, string path)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Builder.sln"))) root = root.Parent;
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        foreach (string argument in new[] { Path.Combine(root!.FullName, "ThunkAudit", "bin", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net10.0", "ThunkAudit.dll"), mode, path }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); await process.WaitForExitAsync(); throw; }
        return process.ExitCode;
    }
}
