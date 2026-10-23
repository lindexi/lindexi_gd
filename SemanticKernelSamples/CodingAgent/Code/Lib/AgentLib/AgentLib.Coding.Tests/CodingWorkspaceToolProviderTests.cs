using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingWorkspaceCacheTests
{
    [TestMethod(DisplayName = "工作区缓存应装配文件、Roslyn、CLI 和内容工具")]
    [Timeout(15000)]
    public async Task CreateShouldComposeWorkspaceTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);

        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);
        string[] names = cache.Tools.Select(tool => tool.Name).ToArray();

        CollectionAssert.Contains(names, "code_search");
        CollectionAssert.Contains(names, nameof(WorkspaceToolProvider.ListDirectory));
        CollectionAssert.Contains(names, "run_build");
        CollectionAssert.DoesNotContain(names, "RunDotNetRun");
        CollectionAssert.Contains(names, "ListDotNetApi");
        CollectionAssert.Contains(names, "load_image");
    }

    [TestMethod(DisplayName = "启用 dotnet run 时本轮工具快照应包含运行工具")]
    public async Task CreateRunContextWhenDotNetRunIsEnabledShouldIncludeRunTool()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);

        CodingRunWorkspaceContext context = cache.CreateRunContext([], enableDotNetRun: true);

        CollectionAssert.Contains(context.Tools.Select(tool => tool.Name).ToArray(), "RunDotNetRun");
    }

    [TestMethod(DisplayName = "Language Server 启动失败时符号工具应返回可读错误")]
    [Timeout(15000)]
    public async Task CodeSearchWhenLanguageServerCannotStartShouldReturnError()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);
        AIFunction codeSearch = cache.Tools.OfType<AIFunction>().Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["searchQueries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
    }

    [TestMethod(DisplayName = "没有活动 Language Server 时停止应返回 false")]
    public async Task StopLanguageServerWhenUnavailableShouldReturnFalse()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);

        bool stopped = await cache.StopLanguageServerAsync();

        Assert.IsFalse(stopped);
    }

    [TestMethod(DisplayName = "停止后符号工具应继续尝试启动 Language Server")]
    public async Task CodeSearchAfterStopShouldRetryLanguageServerStartup()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);
        await cache.StopLanguageServerAsync();
        AIFunction codeSearch = cache.Tools.OfType<AIFunction>().Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["searchQueries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
    }

    [TestMethod(DisplayName = "运行上下文应复用缓存中的工具和展示注册表")]
    public void CreateRunContextShouldReuseCachedState()
    {
        string workspacePath = CreateTestDirectory();
        ToolRegistration registration = new(AIFunctionFactory.Create(() => "ok", "cached_tool"));
        var cache = new CodingWorkspaceCache(workspacePath, [registration]);

        CodingRunWorkspaceContext first = cache.CreateRunContext([]);
        CodingRunWorkspaceContext second = cache.CreateRunContext([]);

        Assert.AreSame(first.Tools, second.Tools);
        Assert.AreSame(first.ToolRegistrationRegistry, second.ToolRegistrationRegistry);
        Assert.AreEqual(Path.GetFullPath(workspacePath), first.WorkspacePath);
    }

    [TestMethod(DisplayName = "工作区缓存应保留 CLI 工具的跨运行状态")]
    public async Task RunContextsShouldShareDotNetCliToolInstance()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);

        CodingRunWorkspaceContext first = cache.CreateRunContext([]);
        CodingRunWorkspaceContext second = cache.CreateRunContext([]);

        AIFunction firstSearchLog = first.Tools.OfType<AIFunction>().Single(tool => tool.Name == "search_last_log");
        AIFunction secondSearchLog = second.Tools.OfType<AIFunction>().Single(tool => tool.Name == "search_last_log");
        Assert.AreSame(firstSearchLog, secondSearchLog);
    }

    [TestMethod(DisplayName = "释放工作区缓存应释放其拥有的资源一次")]
    public async Task DisposeAsyncShouldDisposeOwnedResource()
    {
        string workspacePath = CreateTestDirectory();
        var resource = new TrackingAsyncDisposable();
        var cache = new CodingWorkspaceCache(workspacePath, [], resource);

        await cache.DisposeAsync();
        await cache.DisposeAsync();

        Assert.AreEqual(1, resource.DisposeCount);
    }

    [TestMethod(DisplayName = "工作区文件工具默认应排除构建输出目录")]
    [Timeout(15000)]
    public async Task ListDirectoryShouldExcludeBuildOutputDirectories()
    {
        string workspacePath = CreateTestDirectory();
        Directory.CreateDirectory(Path.Join(workspacePath, "bin"));
        Directory.CreateDirectory(Path.Join(workspacePath, "obj"));
        await File.WriteAllTextAsync(Path.Join(workspacePath, "bin", "binary.txt"), "bin-content");
        await File.WriteAllTextAsync(Path.Join(workspacePath, "source.txt"), "source-content");
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using CodingWorkspaceCache cache = CodingWorkspaceCache.Create(
            workspacePath,
            invalidLanguageServerPath);
        AIFunction listDirectory = cache.Tools.OfType<AIFunction>()
            .Single(tool => tool.Name == nameof(WorkspaceToolProvider.ListDirectory));

        object? result = await listDirectory.InvokeAsync(new AIFunctionArguments
        {
            ["recursive"] = true,
        });
        string resultText = result?.ToString() ?? string.Empty;

        StringAssert.Contains(resultText, "source.txt");
        Assert.IsFalse(resultText.Contains("binary.txt", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }
    }

    private static string CreateInvalidLanguageServerFile(string workspacePath)
    {
        string filePath = Path.Join(workspacePath, "invalid-language-server.txt");
        File.WriteAllText(filePath, "not an executable");
        return filePath;
    }

    private static string CreateTestDirectory()
    {
        string testRoot = Path.Join(
            AppContext.BaseDirectory,
            nameof(CodingWorkspaceCacheTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
