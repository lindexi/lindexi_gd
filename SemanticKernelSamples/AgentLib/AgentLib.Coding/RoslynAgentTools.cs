using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 提供基于 Roslyn Language Server 的解决方案目录与代码符号查询工具。
/// </summary>
public sealed class RoslynAgentTools : IAsyncDisposable
{
    private const string DefaultLanguageServerCommand = "roslyn-language-server";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string LanguageServerUnavailableResult = JsonSerializer.Serialize(new
    {
        error = new
        {
            code = "roslyn_language_server_unavailable",
            message = "Roslyn Language Server 不可用，无法执行代码符号查询。请确认已安装 roslyn-language-server 且启动命令可用。"
        }
    }, JsonOptions);
    private readonly RoslynLspClient? _lspClient;
    private readonly WorkspaceProjectCatalog _catalog;
    private readonly string _workspacePath;

    private RoslynAgentTools(RoslynLspClient? lspClient, string workspacePath)
    {
        _lspClient = lspClient;
        _workspacePath = Path.GetFullPath(workspacePath);
        _catalog = new WorkspaceProjectCatalog(workspacePath);
    }

    /// <summary>
    /// 启动 Roslyn Language Server 并创建代码工作区工具会话。
    /// </summary>
    /// <param name="workspacePath">工作区根目录。</param>
    /// <param name="languageServerCommand">Roslyn Language Server 命令或可执行文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>需要异步释放的 Roslyn 工具会话。</returns>
    public static async Task<RoslynAgentTools> CreateAsync(
        string workspacePath,
        string languageServerCommand = DefaultLanguageServerCommand,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(workspacePath, nameof(workspacePath));
        ThrowIfNullOrWhiteSpace(languageServerCommand, nameof(languageServerCommand));

        string fullWorkspacePath = Path.GetFullPath(workspacePath);
        RoslynLspClient lspClient = await RoslynLspClient
            .StartAsync(fullWorkspacePath, languageServerCommand, cancellationToken)
            .ConfigureAwait(false);
        return new RoslynAgentTools(lspClient, fullWorkspacePath);
    }

    internal static RoslynAgentTools CreateUnavailable(string workspacePath)
    {
        ThrowIfNullOrWhiteSpace(workspacePath, nameof(workspacePath));
        return new RoslynAgentTools(null, Path.GetFullPath(workspacePath));
    }

    /// <summary>
    /// 创建可注入 Agent 的 Roslyn 查询工具集合。
    /// </summary>
    /// <returns>解决方案目录、项目文件、符号搜索、符号详情和引用查询工具。</returns>
    public IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(GetProjectsInSolution, "get_projects_in_solution"),
        AIFunctionFactory.Create(GetFilesInProject, "get_files_in_project"),
        AIFunctionFactory.Create(CodeSearchAsync, "code_search"),
        AIFunctionFactory.Create(FindSymbolAsync, "find_symbol"),
        AIFunctionFactory.Create(FindAllReferencesAsync, "find_all_references")
    ];

    /// <summary>
    /// 返回当前解决方案中的项目。
    /// </summary>
    /// <param name="solution_path">解决方案的绝对路径或工作区相对路径。</param>
    /// <returns>包含项目路径的 JSON。</returns>
    [Description("返回当前解决方案中的项目。仅在需要了解工作区整体结构时使用。solution_path 可省略。")]
    public string GetProjectsInSolution(
        [Description("解决方案的绝对路径或工作区相对路径；省略时自动选择工作区根目录中的唯一 .sln/.slnx。")]
        string? solution_path = null) =>
        _catalog.GetProjectsInSolution(solution_path);

    /// <summary>
    /// 返回指定项目包含的文件。
    /// </summary>
    /// <param name="project_path">项目的绝对路径或工作区相对路径。</param>
    /// <returns>包含项目和文件路径的 JSON。</returns>
    [Description("返回指定项目包含的文件。先用 get_projects_in_solution 获取项目路径。")]
    public string GetFilesInProject(
        [Description("项目的绝对路径或工作区相对路径。")]
        string project_path) =>
        _catalog.GetFilesInProject(project_path);

    /// <summary>
    /// 使用 Roslyn 工作区符号索引按查询词搜索代码符号。
    /// </summary>
    /// <param name="search_queries">符号名称或与符号名称相关的查询词。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按查询词分组的符号结果 JSON。</returns>
    [Description("按符号名称或名称片段搜索工作区代码符号。该工具使用 Roslyn workspace/symbol 索引，不是自然语言语义搜索。")]
    public async Task<string> CodeSearchAsync(
        [Description("一个或多个符号名称、名称片段或代码概念关键词。")]
        string[] search_queries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search_queries);
        RoslynLspClient? lspClient = _lspClient;
        if (lspClient is null)
        {
            return LanguageServerUnavailableResult;
        }

        List<JsonNode?> results = new(search_queries.Length);
        foreach (string query in search_queries
                     .Where(query => !string.IsNullOrWhiteSpace(query))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            JsonNode? symbols = await lspClient
                .RequestAsync("workspace/symbol", new { query }, cancellationToken)
                .ConfigureAwait(false);
            results.Add(new JsonObject
            {
                ["query"] = query,
                ["symbols"] = SanitizeForOutput(LimitArray(symbols, 20))
            });
        }

        return JsonSerializer.Serialize(results, JsonOptions);
    }

    /// <summary>
    /// 查找符号，并返回其定义、实现和引用。
    /// </summary>
    /// <param name="symbol_name">符号名称。</param>
    /// <param name="include_declaration">引用结果是否包含声明。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>符号详情 JSON。</returns>
    [Description("查找符号并追踪其定义、实现和调用引用。需要了解符号用法、定义、接口实现或代码依赖时使用。")]
    public async Task<string> FindSymbolAsync(
        [Description("要查找的类型、方法、属性、字段或其他符号名称。")]
        string symbol_name,
        [Description("是否在引用结果中包含符号声明本身。")]
        bool include_declaration = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(symbol_name, nameof(symbol_name));
        RoslynLspClient? lspClient = _lspClient;
        if (lspClient is null)
        {
            return LanguageServerUnavailableResult;
        }

        JsonNode? symbols = await lspClient
            .RequestAsync("workspace/symbol", new { query = symbol_name }, cancellationToken)
            .ConfigureAwait(false);
        JsonArray matches = SelectExactOrBestMatches(symbols, symbol_name, 10);
        JsonArray detailedMatches = [];

        foreach (JsonNode? match in matches)
        {
            JsonNode? location = match?["location"];
            if (!TryReadLocation(location, out string filePath, out int line, out int character))
            {
                detailedMatches.Add(SanitizeForOutput(match?.DeepClone()));
                continue;
            }

            if (!IsPathInsideWorkspace(filePath))
            {
                detailedMatches.Add(new JsonObject
                {
                    ["symbol"] = SanitizeForOutput(match?.DeepClone()),
                    ["warning"] = "符号位置位于工作区外，已跳过详情查询。"
                });
                continue;
            }

            await lspClient.OpenDocumentAsync(filePath, cancellationToken).ConfigureAwait(false);
            object position = CreateTextDocumentPosition(filePath, line, character);
            JsonNode? definitions = await lspClient
                .RequestAsync("textDocument/definition", position, cancellationToken)
                .ConfigureAwait(false);
            JsonNode? implementations = await lspClient
                .RequestAsync("textDocument/implementation", position, cancellationToken)
                .ConfigureAwait(false);
            JsonNode? references = await RequestReferencesAsync(
                lspClient,
                filePath,
                line,
                character,
                include_declaration,
                cancellationToken).ConfigureAwait(false);

            detailedMatches.Add(new JsonObject
            {
                ["symbol"] = SanitizeForOutput(match?.DeepClone()),
                ["definitions"] = SanitizeForOutput(definitions?.DeepClone()),
                ["implementations"] = SanitizeForOutput(implementations?.DeepClone()),
                ["references"] = SanitizeForOutput(references?.DeepClone())
            });
        }

        return detailedMatches.ToJsonString(JsonOptions);
    }

    /// <summary>
    /// 查找指定源码位置处符号的全部引用。
    /// </summary>
    /// <param name="file_path">源码文件路径。</param>
    /// <param name="line">从零开始的 LSP 行号。</param>
    /// <param name="character">从零开始、按 UTF-16 代码单元计算的字符位置。</param>
    /// <param name="include_declaration">是否包含符号声明。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>引用位置 JSON。</returns>
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
        ThrowIfNullOrWhiteSpace(file_path, nameof(file_path));
        if (line < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "行号不能小于零。");
        }

        if (character < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(character), character, "字符位置不能小于零。");
        }

        RoslynLspClient? lspClient = _lspClient;
        if (lspClient is null)
        {
            return LanguageServerUnavailableResult;
        }

        file_path = Path.GetFullPath(file_path);
        EnsurePathInsideWorkspace(file_path);
        await lspClient.OpenDocumentAsync(file_path, cancellationToken).ConfigureAwait(false);
        JsonNode? references = await RequestReferencesAsync(
            lspClient,
            file_path,
            line,
            character,
            include_declaration,
            cancellationToken).ConfigureAwait(false);
        return SanitizeForOutput(references)?.ToJsonString(JsonOptions) ?? "null";
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _lspClient is null ? default : _lspClient.DisposeAsync();

    private Task<JsonNode?> RequestReferencesAsync(
        RoslynLspClient lspClient,
        string filePath,
        int line,
        int character,
        bool includeDeclaration,
        CancellationToken cancellationToken) =>
        lspClient.RequestAsync(
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
        JsonNode?[] exact = source.Where(node =>
                string.Equals(node?["name"]?.GetValue<string>(), symbolName, StringComparison.Ordinal))
            .Take(limit)
            .ToArray();
        IEnumerable<JsonNode?> selected = exact.Length > 0
            ? exact
            : source.Where(node => node?["name"]?.GetValue<string>()?.Contains(
                symbolName,
                StringComparison.OrdinalIgnoreCase) == true).Take(limit);
        return new JsonArray(selected.Select(node => node?.DeepClone()).ToArray());
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
        if (uriText is null
            || start is null
            || !Uri.TryCreate(uriText, UriKind.Absolute, out Uri? uri)
            || !uri.IsFile)
        {
            return false;
        }

        filePath = uri.LocalPath;
        line = start["line"]?.GetValue<int>() ?? 0;
        character = start["character"]?.GetValue<int>() ?? 0;
        return true;
    }

    private JsonNode? SanitizeForOutput(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            var sanitizedObject = new JsonObject();
            foreach ((string propertyName, JsonNode? value) in jsonObject)
            {
                sanitizedObject[propertyName] = propertyName.Equals("uri", StringComparison.OrdinalIgnoreCase)
                    ? SanitizeUri(value)
                    : SanitizeForOutput(value);
            }

            return sanitizedObject;
        }

        if (node is JsonArray jsonArray)
        {
            return new JsonArray(jsonArray.Select(SanitizeForOutput).ToArray());
        }

        return node?.DeepClone();
    }

    private JsonNode? SanitizeUri(JsonNode? node)
    {
        string? uriText = node?.GetValue<string>();
        if (uriText is null || !Uri.TryCreate(uriText, UriKind.Absolute, out Uri? uri) || !uri.IsFile)
        {
            return node?.DeepClone();
        }

        string fullPath = Path.GetFullPath(uri.LocalPath);
        return IsPathInsideWorkspace(fullPath)
            ? JsonValue.Create(Path.GetRelativePath(_workspacePath, fullPath).Replace(Path.DirectorySeparatorChar, '/'))
            : JsonValue.Create("[工作区外位置已隐藏]");
    }

    private void EnsurePathInsideWorkspace(string path)
    {
        if (!IsPathInsideWorkspace(path))
        {
            throw new UnauthorizedAccessException("目标文件必须位于工作区内。");
        }
    }

    private bool IsPathInsideWorkspace(string path)
    {
        string relativePath = Path.GetRelativePath(_workspacePath, Path.GetFullPath(path));
        return !Path.IsPathRooted(relativePath)
            && !relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("参数不能为空。", parameterName);
        }
    }
}
