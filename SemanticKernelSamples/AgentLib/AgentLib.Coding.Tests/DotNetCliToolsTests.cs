using AgentLib.Coding;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="DotNetCliTools"/> 的单元测试。
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DotNetCliToolsTests
{
    [TestMethod(DisplayName = "不存在的工作区应拒绝创建工具")]
    [Timeout(5000, CooperativeCancellation = true)]
    public void Constructor_WhenWorkspaceDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "missing");

        Assert.ThrowsExactly<DirectoryNotFoundException>(() => new DotNetCliTools(workspacePath));
    }

    [TestMethod(DisplayName = "构建工具应拒绝工作区外的项目路径")]
    [Timeout(5000, CooperativeCancellation = true)]
    public async Task RunBuildAsync_WhenTargetIsOutsideWorkspace_ReturnsErrorMessage()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        string outsideProjectPath = Path.Join(testRoot, "outside", "Outside.csproj");
        Directory.CreateDirectory(workspacePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outsideProjectPath)!);
        await File.WriteAllTextAsync(outsideProjectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync(outsideProjectPath);

        StringAssert.Contains(result, "目标不在代码工作区范围内");
    }

    [TestMethod(DisplayName = "构建工具应拒绝非解决方案或项目文件")]
    [Timeout(5000, CooperativeCancellation = true)]
    public async Task RunBuildAsync_WhenTargetIsNotProjectOrSolution_ReturnsErrorMessage()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "workspace");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(Path.Join(workspacePath, "note.txt"), "content");
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync("note.txt");

        StringAssert.Contains(result, "仅支持 .sln、.slnx、.csproj、.vbproj 或 .fsproj 文件");
    }

    [TestMethod(DisplayName = "构建工具应使用 dotnet build 构建指定项目")]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task RunBuildAsync_WhenProjectIsValid_ReturnsSuccessfulResult()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync("Sample.csproj");

        StringAssert.Contains(result, "执行成功");
        StringAssert.Contains(result, "可使用 read_last_log_lines 按行读取");
    }

    [TestMethod(DisplayName = "测试工具应使用 dotnet test 测试指定项目")]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task RunTestsAsync_WhenProjectIsValid_ReturnsSuccessfulResult()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunTestsAsync("Sample.csproj");

        StringAssert.Contains(result, "执行成功");
        StringAssert.Contains(result, "可使用 read_last_log_lines 按行读取");
    }

    [TestMethod(DisplayName = "读取构建日志应返回元数据和实际行范围")]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task ReadLastLogLines_WhenBuildHasCompleted_ReturnsFriendlyMetadata()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);
        await tools.RunBuildAsync("Sample.csproj");

        string result = tools.ReadLastLogLines(1, 2);

        StringAssert.Contains(result, "<MetaData>");
        StringAssert.Contains(result, "返回行范围: 1-2");
    }

    private static async Task<string> CreateMinimalProjectAsync()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "workspace");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(
            Path.Join(workspacePath, "Sample.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Join(workspacePath, "Sample.cs"), "internal static class Sample { }");
        return workspacePath;
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(AppContext.BaseDirectory, nameof(DotNetCliToolsTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
