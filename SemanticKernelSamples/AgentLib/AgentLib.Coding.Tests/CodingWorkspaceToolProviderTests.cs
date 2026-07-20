using AgentLib.Coding;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Tests;

/// <summary>
/// <see cref="CodingWorkspaceToolProvider"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class CodingWorkspaceToolProviderTests
{
    [TestMethod(DisplayName = "Language Server 启动失败时仍应发布完整工作区工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenLanguageServerCannotStart_PublishesAllTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);

        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        CollectionAssert.AreEquivalent(
            new[]
            {
                "get_projects_in_solution",
                "get_files_in_project",
                "code_search",
                "find_symbol",
                "find_all_references",
                "run_build",
                "run_tests",
                "read_last_log_lines",
                "search_last_log",
            },
            roleTool.AITools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod(DisplayName = "Language Server 启动失败时符号工具应返回错误信息")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task CodeSearchAsync_WhenLanguageServerCannotStart_ReturnsErrorMessage()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        AIFunction codeSearch = roleTool.AITools
            .OfType<AIFunction>()
            .Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["searchQueries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
    }

    [TestMethod(DisplayName = "清空工作区时应移除已发布工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenWorkspaceIsCleared_RemovesTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);

        await toolProvider.SetWorkspacePathAsync(null, CancellationToken.None);

        Assert.IsEmpty(toolProvider.AITools);
    }

    [TestMethod(DisplayName = "切换到无效工作区失败时应保留现有工具")]
    [Timeout(15000, CooperativeCancellation = true)]
    public async Task SetWorkspacePathAsync_WhenNewWorkspaceIsInvalid_KeepsExistingTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var toolProvider = new CodingWorkspaceToolProvider(invalidLanguageServerPath);
        await toolProvider.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        string[] originalToolNames = toolProvider.AITools.Select(tool => tool.Name).ToArray();

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => toolProvider.SetWorkspacePathAsync(
            Path.Join(workspacePath, "missing"),
            CancellationToken.None));

        CollectionAssert.AreEqual(originalToolNames, toolProvider.AITools.Select(tool => tool.Name).ToArray());
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
            nameof(CodingWorkspaceToolProviderTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
