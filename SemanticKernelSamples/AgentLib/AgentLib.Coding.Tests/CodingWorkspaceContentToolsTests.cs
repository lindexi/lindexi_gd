using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingWorkspaceContentToolsTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0xF0,
        0x1F, 0x00, 0x05, 0x00, 0x01, 0xFF, 0x89, 0x99,
        0x3D, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45,
        0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
    ];

    [TestMethod(DisplayName = "加载工作区图片应返回图片 DataContent")]
    [Timeout(5000)]
    public async Task LoadImageAsync_WhenImageIsInsideWorkspace_ReturnsDataContent()
    {
        string workspacePath = CreateTestDirectory();
        string imagePath = Path.Join(workspacePath, "sample.png");
        await File.WriteAllBytesAsync(imagePath, PngBytes);
        var tools = new CodingWorkspaceContentTools(workspacePath);

        DataContent content = await tools.LoadImageAsync("sample.png");

        Assert.AreEqual("image/png", content.MediaType);
    }

    [TestMethod(DisplayName = "加载工作区外图片应拒绝访问")]
    [Timeout(5000)]
    public async Task LoadImageAsync_WhenImageIsOutsideWorkspace_ThrowsUnauthorizedAccessException()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        string imagePath = Path.Join(testRoot, "outside.png");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllBytesAsync(imagePath, PngBytes);
        var tools = new CodingWorkspaceContentTools(workspacePath);

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => tools.LoadImageAsync(imagePath));
    }

    [TestMethod(DisplayName = "加载非图片文件应拒绝返回内容")]
    [Timeout(5000)]
    public async Task LoadImageAsync_WhenFileIsNotImage_ThrowsInvalidDataException()
    {
        string workspacePath = CreateTestDirectory();
        await File.WriteAllTextAsync(Path.Join(workspacePath, "sample.txt"), "content");
        var tools = new CodingWorkspaceContentTools(workspacePath);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(
            () => tools.LoadImageAsync("sample.txt"));
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(
            AppContext.BaseDirectory,
            nameof(CodingWorkspaceContentToolsTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
