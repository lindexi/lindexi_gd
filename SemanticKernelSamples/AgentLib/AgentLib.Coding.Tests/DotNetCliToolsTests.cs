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
    [Timeout(5000)]
    public void Constructor_WhenWorkspaceDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "missing");

        Assert.ThrowsExactly<DirectoryNotFoundException>(() => new DotNetCliTools(workspacePath));
    }

    [TestMethod(DisplayName = "构建工具应允许工作区外的项目路径")]
    [Timeout(30000)]
    public async Task RunBuildAsync_WhenTargetIsOutsideWorkspace_ReturnsSuccessfulResult()
    {
        string testRoot = CreateTestDirectory();
        string workspacePath = Path.Join(testRoot, "workspace");
        string outsideDirectoryPath = Path.Join(testRoot, "outside");
        string outsideProjectPath = Path.Join(outsideDirectoryPath, "Outside.csproj");
        Directory.CreateDirectory(workspacePath);
        Directory.CreateDirectory(outsideDirectoryPath);
        await File.WriteAllTextAsync(
            outsideProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Join(outsideDirectoryPath, "Outside.cs"), "internal static class Outside { }");
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync(outsideProjectPath);

        StringAssert.Contains(result, "执行成功");
    }

    [TestMethod(DisplayName = "构建工具应将非解决方案或项目文件交给 dotnet build 处理")]
    [Timeout(5000)]
    public async Task RunBuildAsync_WhenTargetIsNotProjectOrSolution_ReturnsDotNetBuildError()
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "workspace");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(Path.Join(workspacePath, "note.txt"), "content");
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync("note.txt");

        StringAssert.Contains(result, "MSB4025");
    }

    [TestMethod(DisplayName = "构建工具应使用 dotnet build 构建指定项目")]
    [Timeout(30000)]
    public async Task RunBuildAsync_WhenProjectIsValid_ReturnsSuccessfulResult()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunBuildAsync("Sample.csproj");

        StringAssert.Contains(result, "执行成功");
        StringAssert.Contains(result, "可使用 read_last_log_lines 按行读取");
    }

    [TestMethod(DisplayName = "构建工具应将常用构建参数传递给 dotnet build")]
    [Timeout(30000)]
    public async Task RunBuildAsync_WhenBuildOptionsAreSpecified_RecordsOptionsInLog()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);

        await tools.RunBuildAsync("Sample.csproj", "Release", "linux-x64", "net6.0");
        string result = tools.ReadLastLogLines(1, 10);

        StringAssert.Contains(result, "附加参数: --configuration Release --runtime linux-x64 --framework net6.0");
    }

    [TestMethod(DisplayName = "发布工具应使用完整 dotnet publish 命令发布指定项目")]
    [Timeout(30000)]
    public async Task RunDotNetPublishAsync_WhenCommandIsValid_ReturnsSuccessfulResult()
    {
        string projectName = $"Publish{Guid.NewGuid():N}";
        string workspacePath = await CreateMinimalProjectAsync(projectName);
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunDotNetPublishAsync($"dotnet publish {projectName}.csproj");

        StringAssert.Contains(result, "执行成功");
        StringAssert.Contains(result, "可使用 read_last_log_lines 按行读取");
    }

    [DataTestMethod(DisplayName = "发布工具应拒绝未严格以 dotnet publish 开头的命令")]
    [DataRow(" dotnet publish Sample.csproj")]
    [DataRow("Dotnet publish Sample.csproj")]
    [DataRow("dotnet publisher Sample.csproj")]
    [DataRow("dotnet publish\nSample.csproj")]
    [Timeout(5000)]
    public async Task RunDotNetPublishAsync_WhenCommandPrefixIsInvalid_ReturnsValidationError(string commandLine)
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "workspace");
        Directory.CreateDirectory(workspacePath);
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunDotNetPublishAsync(commandLine);

        StringAssert.Contains(result, "命令行必须严格以 dotnet publish 开头");
    }

    [TestMethod(DisplayName = "测试工具应使用 dotnet test 测试指定项目")]
    [Timeout(30000)]
    public async Task RunTestsAsync_WhenProjectIsValid_ReturnsSuccessfulResult()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);

        string result = await tools.RunTestsAsync("Sample.csproj");

        StringAssert.Contains(result, "执行成功");
        StringAssert.Contains(result, "可使用 read_last_log_lines 按行读取");
    }

    [TestMethod(DisplayName = "测试工具应将测试筛选器传递给 dotnet test")]
    [Timeout(30000)]
    public async Task RunTestsAsync_WhenFilterIsSpecified_RecordsFilterInLog()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);
        const string filter = "FullyQualifiedName~SampleTests";

        await tools.RunTestsAsync("Sample.csproj", filter);
        string result = tools.ReadLastLogLines(1, 10);

        StringAssert.Contains(result, $"附加参数: --filter {filter}");
    }

    [TestMethod(DisplayName = "读取构建日志应返回元数据和实际行范围")]
    [Timeout(30000)]
    public async Task ReadLastLogLines_WhenBuildHasCompleted_ReturnsFriendlyMetadata()
    {
        string workspacePath = await CreateMinimalProjectAsync();
        var tools = new DotNetCliTools(workspacePath);
        await tools.RunBuildAsync("Sample.csproj");

        string result = tools.ReadLastLogLines(1, 2);

        StringAssert.Contains(result, "<MetaData>");
        StringAssert.Contains(result, "返回行范围: 1-2");
    }

    private static async Task<string> CreateMinimalProjectAsync(string projectName = "Sample")
    {
        string workspacePath = Path.Join(CreateTestDirectory(), "workspace");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(
            Path.Join(workspacePath, $"{projectName}.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");
        await File.WriteAllTextAsync(Path.Join(workspacePath, $"{projectName}.cs"), $"internal static class {projectName} {{ }}");
        return workspacePath;
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(AppContext.BaseDirectory, nameof(DotNetCliToolsTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
