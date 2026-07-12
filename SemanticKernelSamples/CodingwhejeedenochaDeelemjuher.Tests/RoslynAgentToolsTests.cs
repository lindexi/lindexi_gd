using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodingwhejeedenochaDeelemjuher.Tests;

[TestClass]
public sealed class RoslynAgentToolsTests
{
    [TestMethod]
    public async Task AsAITools_ExportsExpectedTools()
    {
        using TestWorkspace workspace = new();
        string languageServer = GetLanguageServerPath();
        await using RoslynAgentTools tools = await RoslynAgentTools.CreateAsync(workspace.RootPath, languageServer);

        string[] names = tools.AsAITools().Select(tool => tool.Name).Order().ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                "code_search",
                "find_all_references",
                "find_symbol",
                "get_files_in_project",
                "get_projects_in_solution"
            },
            names);
    }

    [TestMethod]
    [Timeout(120_000, CooperativeCancellation = true)]
    public async Task RoslynTools_ReturnSemanticSearchSymbolAndReferenceResults()
    {
        using TestWorkspace workspace = new();
        string languageServer = GetLanguageServerPath();
        await using RoslynAgentTools tools = await RoslynAgentTools.CreateAsync(workspace.RootPath, languageServer);

        JsonArray searchResults = await WaitForSearchResultAsync(tools, "Calculator");
        Assert.IsGreaterThan(0, searchResults.Count, "code_search 应返回 Calculator 符号。");

        JsonArray symbolResults = JsonNode.Parse(await tools.FindSymbolAsync("Calculator"))!.AsArray();
        Assert.IsGreaterThan(0, symbolResults.Count, "find_symbol 应返回 Calculator。");
        Assert.IsTrue(symbolResults.Any(result => result?["references"] is JsonArray references && references.Count >= 2),
            "find_symbol 应返回声明和使用位置。");

        (int line, int character) = FindPosition(workspace.UsagePath, "Add");
        JsonArray referencesResult = JsonNode.Parse(await tools.FindAllReferencesAsync(
            workspace.UsagePath,
            line,
            character,
            include_declaration: true))!.AsArray();
        Assert.IsGreaterThanOrEqualTo(2, referencesResult.Count, "find_all_references 应至少返回接口声明和调用位置。");
    }

    private static async Task<JsonArray> WaitForSearchResultAsync(RoslynAgentTools tools, string query)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            JsonArray result = JsonNode.Parse(await tools.CodeSearchAsync([query]))!.AsArray();
            JsonArray symbols = result[0]!["symbols"]?.AsArray() ?? [];
            if (symbols.Count > 0)
            {
                return symbols;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return [];
    }

    private static (int Line, int Character) FindPosition(string filePath, string symbol)
    {
        string[] lines = File.ReadAllLines(filePath);
        for (int line = 0; line < lines.Length; line++)
        {
            int character = lines[line].IndexOf(symbol, StringComparison.Ordinal);
            if (character >= 0)
            {
                return (line, character);
            }
        }

        throw new InvalidOperationException($"在 {filePath} 中找不到 {symbol}。");
    }

    private static string GetLanguageServerPath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("ROSLYN_LANGUAGE_SERVER_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        string executableName = OperatingSystem.IsWindows()
            ? "roslyn-language-server.exe"
            : "roslyn-language-server";
        string localPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", ".tools", executableName));
        if (File.Exists(localPath))
        {
            return localPath;
        }

        string testServerPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "CodingwhejeedenochaDeelemjuher.TestLanguageServer",
            "bin", "Debug", "net10.0",
            OperatingSystem.IsWindows()
                ? "CodingwhejeedenochaDeelemjuher.TestLanguageServer.exe"
                : "CodingwhejeedenochaDeelemjuher.TestLanguageServer"));
        Assert.IsTrue(File.Exists(testServerPath), $"找不到测试语言服务器：{testServerPath}");
        return testServerPath;
    }
}
