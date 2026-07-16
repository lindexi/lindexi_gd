using AgentLib.ChatRoom.Tools.Coding;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tests.Tools.Coding;

/// <summary>
/// <see cref="CodingWorkspaceRoleTool"/> 的单元测试。
/// </summary>
[TestClass]
public sealed class CodingWorkspaceRoleToolTests
{
    [TestMethod(DisplayName = "Language Server 启动失败时仍应发布完整工作区工具")]
    [Timeout(15000)]
    public async Task SetWorkspacePathAsync_WhenLanguageServerCannotStart_PublishesAllTools()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceRoleTool(invalidLanguageServerPath);

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
    [Timeout(15000)]
    public async Task CodeSearchAsync_WhenLanguageServerCannotStart_ReturnsErrorMessage()
    {
        string workspacePath = CreateTestDirectory();
        string invalidLanguageServerPath = CreateInvalidLanguageServerFile(workspacePath);
        await using var roleTool = new CodingWorkspaceRoleTool(invalidLanguageServerPath);
        await roleTool.SetWorkspacePathAsync(workspacePath, CancellationToken.None);
        AIFunction codeSearch = roleTool.AITools
            .OfType<AIFunction>()
            .Single(tool => tool.Name == "code_search");

        object? result = await codeSearch.InvokeAsync(new AIFunctionArguments
        {
            ["search_queries"] = new[] { "Sample" },
        });

        StringAssert.Contains(result?.ToString(), "roslyn_language_server_unavailable");
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
            nameof(CodingWorkspaceRoleToolTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }
}
