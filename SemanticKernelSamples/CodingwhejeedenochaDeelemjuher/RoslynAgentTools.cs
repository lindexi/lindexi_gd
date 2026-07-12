using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace CodingwhejeedenochaDeelemjuher;

public sealed class RoslynAgentTools : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RoslynLspClient _lsp;
    private readonly WorkspaceProjectCatalog _catalog;

    private RoslynAgentTools(RoslynLspClient lsp, string workspacePath)
    {
        _lsp = lsp;
        _catalog = new WorkspaceProjectCatalog(workspacePath);
    }

    public static async Task<RoslynAgentTools> CreateAsync(
        string workspacePath,
        string languageServerCommand = "roslyn-language-server",
        CancellationToken cancellationToken = default) =>
        new(await RoslynLspClient.StartAsync(workspacePath, languageServerCommand, cancellationToken), workspacePath);

    public IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(GetProjectsInSolutionAsync, "get_projects_in_solution"),
        AIFunctionFactory.Create(GetFilesInProjectAsync, "get_files_in_project"),
        AIFunctionFactory.Create(CodeSearchAsync, "code_search"),
        AIFunctionFactory.Create(FindSymbolAsync, "find_symbol"),
        AIFunctionFactory.Create(FindAllReferencesAsync, "find_all_references")
    ];

    [Description("返回当前解决方案中的项目。仅在需要了解工作区整体结构时使用。solution_path 可省略。")]
    public Task<string> GetProjectsInSolutionAsync(
        [Description("解决方案的绝对路径或工作区相对路径；省略时自动选择工作区根目录中的唯一 .sln/.slnx。")]
        string? solution_path = null) =>
        Task.FromResult(_catalog.GetProjectsInSolution(solution_path));

    [Description("返回指定项目包含的文件。先用 get_projects_in_solution 获取项目路径。")]
    public Task<string> GetFilesInProjectAsync(
        [Description("项目的绝对路径或工作区相对路径。")]
        string project_path) =>
        Task.FromResult(_catalog.GetFilesInProject(project_path));

    [Description("按功能、行为或错误关键词搜索工作区中的代码符号。使用 Roslyn 语义符号索引，不是纯文本搜索；适合类、方法、属性及相关概念定位。")]
    public async Task<string> CodeSearchAsync(
        [Description("一个或多个用于描述功能、行为或错误的搜索词。")]
        string[] search_queries,
        CancellationToken cancellationToken = default)
    {
        List<JsonNode?> results = [];
        foreach (string query in search_queries.Where(query => !string.IsNullOrWhiteSpace(query)).Distinct())
        {
            JsonNode? symbols = await _lsp.RequestAsync("workspace/symbol", new { query }, cancellationToken);
            results.Add(new JsonObject
            {
                ["query"] = query,
                ["symbols"] = LimitArray(symbols, 20)
            });
        }

        return JsonSerializer.Serialize(results, JsonOptions);
    }

    [Description("查找符号并追踪其定义、实现和调用引用。需要了解符号用法、定义、接口实现或代码依赖时使用。")]
    public async Task<string> FindSymbolAsync(
        [Description("要查找的类型、方法、属性、字段或其他符号名称。")]
        string symbol_name,
        [Description("是否包含符号声明本身。")]
        bool include_declaration = true,
        CancellationToken cancellationToken = default)
    {
        JsonNode? symbols = await _lsp.RequestAsync("workspace/symbol", new { query = symbol_name }, cancellationToken);
        JsonArray matches = SelectExactOrBestMatches(symbols, symbol_name, 10);
        JsonArray detailedMatches = [];

        foreach (JsonNode? match in matches)
        {
            JsonNode? location = match?["location"];
            if (!TryReadLocation(location, out string? filePath, out int line, out int character))
            {
                detailedMatches.Add(match?.DeepClone());
                continue;
            }

            await _lsp.OpenDocumentAsync(filePath, cancellationToken);
            object position = CreateTextDocumentPosition(filePath, line, character);
            JsonNode? definitions = await _lsp.RequestAsync("textDocument/definition", position, cancellationToken);
            JsonNode? implementations = await _lsp.RequestAsync("textDocument/implementation", position, cancellationToken);
            JsonNode? references = await RequestReferencesAsync(filePath, line, character, include_declaration, cancellationToken);

            detailedMatches.Add(new JsonObject
            {
                ["symbol"] = match?.DeepClone(),
                ["definitions"] = definitions?.DeepClone(),
                ["implementations"] = implementations?.DeepClone(),
                ["references"] = references?.DeepClone()
            });
        }

        return detailedMatches.ToJsonString(JsonOptions);
    }

    [Description("查找给定源码位置处符号的所有引用。位置使用从 0 开始的 LSP 行号和 UTF-16 字符偏移。")]
    public async Task<string> FindAllReferencesAsync(
        [Description("源码文件的绝对路径。")]
        string file_path,
        [Description("从 0 开始的行号。")]
        int line,
        [Description("从 0 开始、按 UTF-16 代码单元计算的字符位置。")]
        int character,
        [Description("是否包含符号声明本身。")]
        bool include_declaration = true,
        CancellationToken cancellationToken = default)
    {
        file_path = Path.GetFullPath(file_path);
        await _lsp.OpenDocumentAsync(file_path, cancellationToken);
        JsonNode? references = await RequestReferencesAsync(
            file_path, line, character, include_declaration, cancellationToken);
        return references?.ToJsonString(JsonOptions) ?? "null";
    }

    public ValueTask DisposeAsync() => _lsp.DisposeAsync();

    private Task<JsonNode?> RequestReferencesAsync(
        string filePath,
        int line,
        int character,
        bool includeDeclaration,
        CancellationToken cancellationToken) =>
        _lsp.RequestAsync(
            "textDocument/references",
            new
            {
                textDocument = new { uri = new Uri(filePath).AbsoluteUri },
                position = new { line, character },
                context = new { includeDeclaration }
            },
            cancellationToken);

    private static object CreateTextDocumentPosition(string filePath, int line, int character) => new
    {
        textDocument = new { uri = new Uri(filePath).AbsoluteUri },
        position = new { line, character }
    };

    private static JsonArray SelectExactOrBestMatches(JsonNode? symbols, string symbolName, int limit)
    {
        JsonArray source = symbols as JsonArray ?? [];
        IEnumerable<JsonNode?> exact = source.Where(node =>
            string.Equals(node?["name"]?.GetValue<string>(), symbolName, StringComparison.Ordinal));
        IEnumerable<JsonNode?> selected = exact.Any()
            ? exact
            : source.Where(node => node?["name"]?.GetValue<string>()?.Contains(symbolName, StringComparison.OrdinalIgnoreCase) == true);
        return new JsonArray(selected.Take(limit).Select(node => node?.DeepClone()).ToArray());
    }

    private static JsonNode? LimitArray(JsonNode? node, int limit) => node is JsonArray array
        ? new JsonArray(array.Take(limit).Select(item => item?.DeepClone()).ToArray())
        : node?.DeepClone();

    private static bool TryReadLocation(JsonNode? location, out string filePath, out int line, out int character)
    {
        filePath = string.Empty;
        line = 0;
        character = 0;
        string? uriText = location?["uri"]?.GetValue<string>();
        JsonNode? start = location?["range"]?["start"];
        if (uriText is null || start is null || !Uri.TryCreate(uriText, UriKind.Absolute, out Uri? uri) || !uri.IsFile)
        {
            return false;
        }

        filePath = uri.LocalPath;
        line = start["line"]?.GetValue<int>() ?? 0;
        character = start["character"]?.GetValue<int>() ?? 0;
        return true;
    }
}
