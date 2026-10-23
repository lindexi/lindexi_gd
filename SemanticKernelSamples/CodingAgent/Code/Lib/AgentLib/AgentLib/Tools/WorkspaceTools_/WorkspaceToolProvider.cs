using AgentLib.Model;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 为 Copilot 提供基于工作路径的默认文件系统工具。
/// </summary>
public sealed class WorkspaceToolProvider
{
    private const int DefaultMaxResults = 100;
    private const int DefaultMaxCharacters = 4000;
    private const int DefaultMaxRangeLines = 400;
    private const int DefaultMaxLineHitsPerFile = 20;
    private const int DefaultMaxRemainingLinesToCount = 500;
    private const int DefaultMaxLineContextChars = 100;
    private const int DefaultMaxQueryDisplayLength = 10;
    private string? _primaryWorkspacePath;
    private string? _secondaryWorkspacePath;
    private readonly Dictionary<string, FileSnapshotInfo> _readFileSnapshots = new(GetPathComparer());

    /// <summary>
    /// 获取在目录列举和查询中需要排除的目录名称集合。
    /// </summary>
    public ISet<string> ExcludedDirectoryNames { get; } = new HashSet<string>(GetPathComparer());

    /// <summary>
    /// 获取或设置是否允许目录列举和查询工具读取工作区外的绝对路径。
    /// 此属性不影响 <see cref="ReadFileLines"/>，该工具继续支持读取任意绝对文件路径。
    /// </summary>
    public bool AllowReadingOutsideWorkspace { get; set; }

    /// <summary>
    /// 工作路径
    /// </summary>
    public string? WorkspacePath
    {
        get
        {
            if (!string.IsNullOrEmpty(_primaryWorkspacePath))
            {
                return _primaryWorkspacePath;
            }

            return _secondaryWorkspacePath;
        }
        set => _primaryWorkspacePath = NormalizeWorkspacePath(value);
    }

    internal string? PrimaryWorkspacePath => _primaryWorkspacePath;

    public string? SecondaryWorkspacePath
    {
        get => _secondaryWorkspacePath;
        set => _secondaryWorkspacePath = NormalizeWorkspacePath(value);
    }

    public IReadOnlyList<AITool> CreateDefaultTools() =>
        CreateDefaultToolRegistrations().Select(registration => registration.Tool).ToArray();

    /// <summary>
    /// 创建默认文件工具及其展示摘要规则。
    /// </summary>
    public IReadOnlyList<ToolRegistration> CreateDefaultToolRegistrations()
    {
        return
        [
            new(AIFunctionFactory.Create(ListDirectory, name: nameof(ListDirectory), description: "列出目录中的文件和子目录。"),
                arguments => ToolCallPresentationFactory.ForPath(arguments, "directoryPath", "工作区根目录")),
            new(AIFunctionFactory.Create(FindEntriesByName, name: nameof(FindEntriesByName), description: "按名称关键字递归查找文件或目录。"),
                arguments => ToolCallPresentationFactory.ForQuery(arguments, "query", "directoryPath")),
            new(AIFunctionFactory.Create(FindFilesMatchingPattern, name: nameof(FindFilesMatchingPattern), description: "在文件内容中递归搜索文本或正则表达式，返回命中位置。"),
                arguments => ToolCallPresentationFactory.ForQuery(arguments, "query", "directoryPath",
                    ToolCallPresentationFactory.GetBoolean(arguments, "useRegex") == true ? "正则" : null)),
            new(AIFunctionFactory.Create(ReadFileLines, name: nameof(ReadFileLines), description: "读取文件的指定行范围。"),
                arguments => ToolCallPresentationFactory.ForFileLineRange(arguments, "filePath", "startLine", "endLine")),
            new(AIFunctionFactory.Create(WriteFileContent, name: nameof(WriteFileContent), description: "覆写或创建文件；覆写前必须先读取文件。"),
                arguments => ToolCallPresentationFactory.ForPath(arguments, "filePath")),
            new(AIFunctionFactory.Create(ReplaceStringInFile, name: nameof(ReplaceStringInFile), description: "替换文件中唯一匹配的文本；替换前必须先读取文件。"),
                arguments => ToolCallPresentationFactory.ForPath(arguments, "filePath")),
            new(AIFunctionFactory.Create(MultiReplaceStringInFile, name: nameof(MultiReplaceStringInFile), description: "批量替换文件中唯一匹配的文本。"),
                ToolCallPresentationCollectionFactory.ForMultipleReplacements)
        ];
    }

    [Description("列出目录中的文件和子目录。")]
    public Task<string> ListDirectory(
        [Description("目录路径；留空表示工作区根目录。")] string? directoryPath = null,
        [Description("是否递归。")] bool recursive = false,
        [Description("最大结果数。")] int maxResults = DefaultMaxResults)
    {
        if (maxResults <= 0)
        {
            return Task.FromResult($"参数错误：maxResults 必须大于 0，当前值为 {maxResults}。");
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return Task.FromResult(errorMessage);
        }

        IEnumerable<FileSystemInfo> directoryEntries = recursive
            ? EnumerateEntriesRecursively(directory)
            : directory.EnumerateFileSystemInfos().Where(entry => !IsExcludedDirectory(entry));

        List<FileSystemInfo> entries = directoryEntries
            .OrderBy(static entry => entry is FileInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults + 1)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine($"工作路径: {GetWorkspaceRootDisplayText()}");
        builder.AppendLine($"目录: {GetDisplayPath(directory.FullName)}");

        if (entries.Count == 0)
        {
            builder.Append("没有找到任何子项。");
            return Task.FromResult(builder.ToString());
        }

        foreach (var entry in entries.Take(maxResults))
        {
            builder.AppendLine($"{GetEntryKind(entry)} {GetDisplayPath(entry.FullName)}");
        }

        if (entries.Count > maxResults)
        {
            builder.Append("已截断，仍有至少 1 个结果未显示。");
        }

        return Task.FromResult(builder.ToString().TrimEnd());
    }

    [Description("按名称关键字递归查找文件或目录。")]
    public async Task<string> FindEntriesByName(
        [Description("名称关键字。")] string query,
        [Description("搜索目录；留空表示工作区根目录。")] string? directoryPath = null,
        [Description("是否查找文件。")] bool includeFiles = true,
        [Description("是否查找目录。")] bool includeDirectories = true,
        [Description("最大结果数。")] int maxResults = DefaultMaxResults)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "参数错误：query 不能为空或仅包含空白字符。";
        }

        if (!includeFiles && !includeDirectories)
        {
            return "参数错误：includeFiles 和 includeDirectories 至少有一个必须为 true。";
        }

        if (maxResults <= 0)
        {
            return $"参数错误：maxResults 必须大于 0，当前值为 {maxResults}。";
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return errorMessage;
        }

        List<FileSystemInfo> entries = await Task.Run(() =>
            EnumerateEntriesRecursively(directory)
                .Where(entry => (includeDirectories && entry is DirectoryInfo || includeFiles && entry is FileInfo)
                    && entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(static entry => entry is FileInfo)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults + 1)
                .ToList()).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine($"工作路径: {GetWorkspaceRootDisplayText()}");
        builder.AppendLine($"搜索目录: {GetDisplayPath(directory.FullName)}");
        builder.AppendLine($"关键字: {query}");

        if (entries.Count == 0)
        {
            builder.Append("没有找到匹配项。");
            return builder.ToString();
        }

        foreach (var entry in entries.Take(maxResults))
        {
            builder.AppendLine($"{GetEntryKind(entry)} {GetDisplayPath(entry.FullName)}");
        }

        if (entries.Count > maxResults)
        {
            builder.Append("已截断，仍有至少 1 个匹配项未显示。");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// 在工作路径下递归查找匹配指定模式的文件，并返回命中文件路径与行号。
    /// 支持纯文本匹配和正则表达式匹配。
    /// </summary>
    /// <param name="query">要匹配的文本或正则表达式模式。</param>
    /// <param name="directoryPath">要搜索的目录路径。相对路径相对于当前工作区；绝对路径默认必须位于工作区内，宿主启用工作区外读取后可指向任意位置。留空表示从工作区根目录开始搜索。</param>
    /// <param name="useRegex">是否将 <paramref name="query"/> 作为正则表达式进行匹配。默认为 false，表示纯文本匹配。</param>
    /// <param name="maxResults">最多返回多少个命中文件。</param>
    [Description("在文件内容中递归搜索文本或正则表达式，返回命中位置。")]
    public async Task<string> FindFilesMatchingPattern(
        [Description("搜索文本或正则表达式。")] string query,
        [Description("搜索目录；留空表示工作区根目录。")] string? directoryPath = null,
        [Description("是否按正则表达式匹配。")] bool useRegex = false,
        [Description("最大命中文件数。")] int maxResults = DefaultMaxResults)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "参数错误：query 不能为空或仅包含空白字符。";
        }

        if (maxResults <= 0)
        {
            return $"参数错误：maxResults 必须大于 0，当前值为 {maxResults}。";
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return errorMessage;
        }

        Regex? regex = null;
        if (useRegex)
        {
            try
            {
                regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch (ArgumentException ex)
            {
                return $"正则表达式无效: {ex.Message}";
            }
        }

        var results = new List<(string Path, WorkspaceFileMatchResults MatchResults)>();
        var matcher = new WorkspaceFilePatternMatcher(DefaultMaxLineContextChars, DefaultMaxLineHitsPerFile, query, regex);
        foreach (FileInfo file in EnumerateEntriesRecursively(directory).OfType<FileInfo>())
        {
            WorkspaceFileMatchResults matchResults = await matcher.FindAsync(file.FullName).ConfigureAwait(false);

            if (matchResults.Matches.Count == 0)
            {
                continue;
            }

            results.Add((GetDisplayPath(file.FullName), matchResults));

            if (results.Count >= maxResults)
            {
                break;
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"工作路径: {GetWorkspaceRootDisplayText()}");
        builder.AppendLine($"搜索目录: {GetDisplayPath(directory.FullName)}");
        builder.AppendLine($"模式{(useRegex ? "（正则）" : "")}: {TruncateQueryForDisplay(query)}");

        if (results.Count == 0)
        {
            builder.Append("没有找到匹配该模式的文件。");
            return builder.ToString();
        }

        foreach (var (filePath, matchResults) in results)
        {
            builder.AppendLine(filePath + ":");
            foreach (var match in matchResults.Matches)
            {
                builder.Append(match.LineNumber);
                builder.Append(": ");
                builder.AppendLine(match.TruncatedContextText);
            }

            if (matchResults.IsTruncated)
            {
                builder.AppendLine("  ...（该文件命中行数过多，已截断）");
            }
        }

        return builder.ToString().TrimEnd();
    }

    [Description("读取文件的指定行范围。")]
    public async Task<string> ReadFileLines(
        [Description("文件路径。")] string filePath,
        [Description("起始行号，从 1 开始。")] int startLine,
        [Description("结束行号，包含该行。")] int endLine,
        [Description("是否显示行号。")] bool includeLineNumbers = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "参数错误：filePath 不能为空或仅包含空白字符。";
        }

        if (startLine <= 0)
        {
            return $"参数错误：startLine 必须大于等于 1，当前值为 {startLine}。";
        }

        if (endLine < startLine)
        {
            return $"参数错误：endLine 必须大于等于 startLine，当前范围为 {startLine}-{endLine}。";
        }

        int requestedLines = endLine - startLine + 1;
        if (requestedLines > DefaultMaxRangeLines)
        {
            return $"参数错误：单次最多读取 {DefaultMaxRangeLines} 行，当前请求读取 {requestedLines} 行。";
        }

        if (!TryResolveFile(filePath, out var file, out var errorMessage))
        {
            return errorMessage;
        }

        RecordFileSnapshot(file);

        var reader = new WorkspaceFileLineReader(DefaultMaxCharacters, DefaultMaxRemainingLinesToCount);

        var displayPath = GetDisplayPath(file.FullName);
        return await reader.ReadAsync(file, startLine, endLine, includeLineNumbers, displayPath).ConfigureAwait(false);
    }

    private void RecordFileSnapshot(FileInfo file)
    {
        file.Refresh();
        _readFileSnapshots[NormalizePath(file.FullName)] = new FileSnapshotInfo(file.Length, file.LastWriteTimeUtc);
    }

    private bool TryResolveDirectory(string? path, out DirectoryInfo directory, out string errorMessage)
    {
        if (!TryResolveDirectoryPath(path, out string fullPath, out errorMessage))
        {
            directory = null!;
            return false;
        }

        directory = new DirectoryInfo(fullPath);
        if (!directory.Exists)
        {
            errorMessage = $"目录不存在: {GetDisplayPath(fullPath)}";
            return false;
        }

        return true;
    }

    private bool TryResolveFile(string path, out FileInfo file, out string errorMessage)
    {
        if (Path.IsPathRooted(path))
        {
            string fullPath = NormalizePath(path);
            file = new FileInfo(fullPath);
            if (!file.Exists)
            {
                errorMessage = $"文件不存在: {fullPath}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        if (!TryResolveRelativeFilePath(path, out string resolvedPath, out errorMessage))
        {
            file = null!;
            return false;
        }

        file = new FileInfo(resolvedPath);
        return true;
    }

    private bool TryResolveFileForWrite(string path, out FileInfo file, out string errorMessage)
    {
        if (Path.IsPathRooted(path))
        {
            string fullPath = NormalizePath(path);

            if (!IsPathInsideAnyWorkspace(fullPath))
            {
                file = null!;
                errorMessage = $"文件不在工作区范围内: {fullPath}";
                return false;
            }

            file = new FileInfo(fullPath);
            errorMessage = string.Empty;
            return true;
        }

        if (!TryResolveRelativeFilePathForWrite(path, out string resolvedPath, out errorMessage))
        {
            file = null!;
            return false;
        }

        file = new FileInfo(resolvedPath);
        return true;
    }

    private bool TryResolveRelativeFilePathForWrite(string path, out string fullPath, out string errorMessage)
    {
        bool hasWorkspaceRoot = false;

        foreach (string workspaceRoot in EnumerateFileWorkspaceRoots())
        {
            hasWorkspaceRoot = true;
            string candidatePath = Path.GetFullPath(Path.Combine(workspaceRoot, path));
            if (!IsPathInsideWorkspace(workspaceRoot, candidatePath))
            {
                continue;
            }

            fullPath = candidatePath;
            errorMessage = string.Empty;
            return true;
        }

        fullPath = string.Empty;
        if (!hasWorkspaceRoot)
        {
            errorMessage = $"当前未设置工作路径，无法解析相对路径: {path}";
            return false;
        }

        errorMessage = $"路径超出了当前工作路径范围: {path}";
        return false;
    }

    private bool IsPathInsideAnyWorkspace(string fullPath)
    {
        foreach (string workspaceRoot in EnumerateFileWorkspaceRoots())
        {
            if (IsPathInsideWorkspace(workspaceRoot, fullPath))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveDirectoryPath(string? path, out string fullPath, out string errorMessage)
    {
        var workspacePath = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                fullPath = string.Empty;
                errorMessage = "当前未设置主工作路径，无法使用目录工具。";
                return false;
            }

            fullPath = workspacePath;
            errorMessage = string.Empty;
            return true;
        }

        if (Path.IsPathRooted(path))
        {
            fullPath = NormalizePath(path);

            if (!AllowReadingOutsideWorkspace && !IsPathInsideAnyWorkspace(fullPath))
            {
                errorMessage = $"目录不在工作区范围内: {fullPath}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            fullPath = string.Empty;
            errorMessage = $"当前未设置主工作路径，无法解析目录相对路径: {path}";
            return false;
        }

        string workspaceRoot = workspacePath;
        fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, path));

        if (!IsPathInsideWorkspace(workspaceRoot, fullPath))
        {
            errorMessage = $"路径超出了当前工作路径范围: {path}";
            return false;
        }

        errorMessage = string.Empty;
        return true;

        string? GetWorkspacePath()
        {
            if (!string.IsNullOrWhiteSpace(_primaryWorkspacePath))
            {
                return _primaryWorkspacePath;
            }
            else
            {
                return _secondaryWorkspacePath;
            }
        }
    }

    private bool TryResolveRelativeFilePath(string path, out string fullPath, out string errorMessage)
    {
        List<string> candidatePaths = [];
        bool hasWorkspaceRoot = false;

        foreach (string workspaceRoot in EnumerateFileWorkspaceRoots())
        {
            hasWorkspaceRoot = true;
            string candidatePath = Path.GetFullPath(Path.Combine(workspaceRoot, path));
            if (!IsPathInsideWorkspace(workspaceRoot, candidatePath))
            {
                continue;
            }

            if (File.Exists(candidatePath))
            {
                fullPath = candidatePath;
                errorMessage = string.Empty;
                return true;
            }

            candidatePaths.Add(candidatePath);
        }

        fullPath = string.Empty;
        if (!hasWorkspaceRoot)
        {
            errorMessage = $"当前未设置工作路径，无法解析相对路径: {path}";
            return false;
        }

        if (candidatePaths.Count == 0)
        {
            errorMessage = $"路径超出了当前工作路径范围: {path}";
            return false;
        }

        errorMessage = candidatePaths.Count == 1
            ? $"文件不存在: {GetDisplayPath(candidatePaths[0])}"
            : $"文件不存在: {GetDisplayPath(candidatePaths[0])}；副工作路径也未找到: {GetDisplayPath(candidatePaths[1])}";
        return false;
    }

    private IEnumerable<FileSystemInfo> EnumerateEntriesRecursively(DirectoryInfo rootDirectory)
    {
        var stack = new Stack<DirectoryInfo>();
        stack.Push(rootDirectory);

        while (stack.Count > 0)
        {
            DirectoryInfo currentDirectory = stack.Pop();
            FileSystemInfo[] entries;
            try
            {
                entries = currentDirectory.GetFileSystemInfos();
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (FileSystemInfo entry in entries)
            {
                if (IsExcludedDirectory(entry))
                {
                    continue;
                }

                yield return entry;
            }

            foreach (DirectoryInfo childDirectory in entries
                         .OfType<DirectoryInfo>()
                          .Where(directory => !IsExcludedDirectory(directory))
                         .OrderByDescending(directory => directory.FullName, StringComparer.OrdinalIgnoreCase))
            {
                stack.Push(childDirectory);
            }
        }
    }

    private bool IsExcludedDirectory(FileSystemInfo entry)
    {
        return entry is DirectoryInfo && ExcludedDirectoryNames.Contains(entry.Name);
    }

    private string GetDisplayPath(string fullPath)
    {
        if (!string.IsNullOrWhiteSpace(_primaryWorkspacePath)
            && IsPathInsideWorkspace(_primaryWorkspacePath, fullPath))
        {
            string relativePath = Path.GetRelativePath(_primaryWorkspacePath, fullPath);
            return relativePath == "." ? "." : relativePath;
        }

        if (!string.IsNullOrWhiteSpace(_secondaryWorkspacePath)
            && IsPathInsideWorkspace(_secondaryWorkspacePath, fullPath))
        {
            string relativePath = Path.GetRelativePath(_secondaryWorkspacePath, fullPath);
            return string.IsNullOrWhiteSpace(_primaryWorkspacePath) ? relativePath : $"[副工作区] {relativePath}";
        }

        return fullPath;
    }

    private string GetWorkspaceRootDisplayText()
    {
        string? workspacePath = _primaryWorkspacePath;
        if (string.IsNullOrEmpty(workspacePath))
        {
            workspacePath = _secondaryWorkspacePath;
        }
        return string.IsNullOrWhiteSpace(workspacePath) ? "<未设置>" : workspacePath;
    }

    private IEnumerable<string> EnumerateFileWorkspaceRoots()
    {
        if (!string.IsNullOrWhiteSpace(_primaryWorkspacePath))
        {
            yield return _primaryWorkspacePath;
        }

        if (!string.IsNullOrWhiteSpace(_secondaryWorkspacePath)
            && (string.IsNullOrWhiteSpace(_primaryWorkspacePath) || !PathsEqual(_primaryWorkspacePath, _secondaryWorkspacePath)))
        {
            yield return _secondaryWorkspacePath;
        }
    }

    private static string? NormalizeWorkspacePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : NormalizePath(path);
    }

    private static string GetEntryKind(FileSystemInfo entry)
    {
        return entry is DirectoryInfo ? "[目录]" : "[文件]";
    }

    private static bool IsPathInsideWorkspace(string workspaceRoot, string fullPath)
    {
        if (PathsEqual(workspaceRoot, fullPath))
        {
            return true;
        }

        return fullPath.StartsWith(workspaceRoot + Path.DirectorySeparatorChar, GetPathComparison());
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), GetPathComparison());
    }

    private static string NormalizePath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }

    /// <summary>
    /// 截断过长的查询字符串用于头部显示。超过 10 字符时取前后各 5 字符，中间用 … 连接。
    /// </summary>
    private static string TruncateQueryForDisplay(string query)
    {
        if (query.Length <= DefaultMaxQueryDisplayLength)
        {
            return query;
        }

        return string.Concat(query.AsSpan(0, 5), "…", query.AsSpan(query.Length - 5));
    }

    /// <summary>
    /// 将内容覆写到工作区内的文件。若文件已存在则整体覆盖原内容，若文件不存在则创建新文件。
    /// 覆写前要求先通过 ReadFileLines 读取过该文件，且文件自读取后未被外部修改。
    /// </summary>
    /// <param name="filePath">要写入的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。</param>
    /// <param name="content">要写入的内容。</param>
    /// <returns>成功时返回 "OK"，失败时返回错误信息。</returns>
    [Description("覆写或创建文件；覆写前必须先读取文件。")]
    public string WriteFileContent(
        [Description("文件路径。")] string filePath,
        [Description("文件内容。")] string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "参数错误：filePath 不能为空或仅包含空白字符。";
        }

        if (!TryResolveFileForWrite(filePath, out var file, out var errorMessage))
        {
            return errorMessage;
        }

        string normalizedPath = NormalizePath(file.FullName);

        if (file.Exists)
        {
            if (!_readFileSnapshots.TryGetValue(normalizedPath, out var snapshot))
            {
                return $"文件已存在但未被读取过: {GetDisplayPath(file.FullName)}。请先使用 ReadFileLines 读取文件内容后再写入，避免误覆盖。";
            }

            file.Refresh();
            if (file.Length != snapshot.Length || file.LastWriteTimeUtc != snapshot.LastWriteTime)
            {
                return $"文件自读取后已被外部修改: {GetDisplayPath(file.FullName)}。请重新使用 ReadFileLines 读取最新内容后再写入。";
            }
        }
        else
        {
            string? directoryPath = Path.GetDirectoryName(file.FullName);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        File.WriteAllText(file.FullName, content);
        return "OK";
    }

    /// <summary>
    /// 替换文件中的指定字符串。要求先通过 ReadFileLines 读取过该文件，
    /// 且 oldString 在文件中必须唯一匹配。
    /// </summary>
    /// <param name="filePath">要替换的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。</param>
    /// <param name="oldString">要替换的原始文本，必须在文件中唯一匹配。</param>
    /// <param name="newString">替换后的新文本。</param>
    /// <returns>成功时返回 "OK"，失败时返回错误信息。</returns>
    [Description("替换文件中唯一匹配的文本；替换前必须先读取文件。")]
    public string ReplaceStringInFile(
        [Description("文件路径。")] string filePath,
        [Description("必须唯一匹配的原始文本；必要时包含上下文。")] string oldString,
        [Description("新文本。")] string newString)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "参数错误：filePath 不能为空或仅包含空白字符。";
        }

        ArgumentNullException.ThrowIfNull(oldString);
        ArgumentNullException.ThrowIfNull(newString);

        if (oldString.Length == 0)
        {
            return "参数错误：oldString 不能为空字符串。";
        }

        var result = ReplaceStringInFileCore(filePath, oldString, newString);
        return result.Message;
    }

    /// <summary>
    /// 批量替换文件中的多个字符串。顺序执行每个替换操作，每个操作独立处理错误。
    /// </summary>
    /// <param name="replacements">替换操作列表，每个操作包含文件路径、原始文本、新文本和说明。</param>
    /// <param name="explanation">批量替换操作的总体说明。</param>
    /// <returns>替换操作的汇总结果。</returns>
    [Description("批量替换文件中唯一匹配的文本。")]
    public string MultiReplaceStringInFile(
        [Description("替换操作列表。")] IReadOnlyList<ReplaceOperation> replacements,
        [Description("修改说明。")] string explanation)
    {
        ArgumentNullException.ThrowIfNull(replacements);

        if (replacements.Count == 0)
        {
            return "替换操作列表为空，未执行任何操作。";
        }

        var results = new List<ReplaceResult>(replacements.Count);
        int successCount = 0;
        int failureCount = 0;

        foreach (var operation in replacements)
        {
            StringReplaceOutcome result;
            if (string.IsNullOrWhiteSpace(operation.FilePath))
            {
                result = new StringReplaceOutcome(false, "参数错误：filePath 不能为空或仅包含空白字符。", null);
            }
            else if (operation.OldString.Length == 0)
            {
                result = new StringReplaceOutcome(false, "参数错误：oldString 不能为空字符串。", null);
            }
            else
            {
                result = ReplaceStringInFileCore(operation.FilePath, operation.OldString, operation.NewString);
            }

            results.Add(new ReplaceResult(operation.FilePath, result.Success, result.Message));

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"批量替换完成: {successCount} 个成功, {failureCount} 个失败。");
        builder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            builder.AppendLine($"操作 {i + 1}: {result.FilePath}");
            builder.AppendLine($"  状态: {(result.Success ? "成功" : "失败")}");
            builder.AppendLine($"  消息: {result.Message}");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private StringReplaceOutcome ReplaceStringInFileCore(string filePath, string oldString, string newString)
    {
        if (!TryResolveFileForWrite(filePath, out var file, out var errorMessage))
        {
            return new StringReplaceOutcome(Success: false, Message: errorMessage, NewContent: null);
        }

        string normalizedPath = NormalizePath(file.FullName);

        if (!file.Exists)
        {
            return new StringReplaceOutcome(Success: false, Message: $"文件不存在: {GetDisplayPath(file.FullName)}", NewContent: null);
        }

        if (!_readFileSnapshots.TryGetValue(normalizedPath, out var snapshot))
        {
            return new StringReplaceOutcome(Success: false, Message: $"文件未被读取过: {GetDisplayPath(file.FullName)}。请先使用 ReadFileLines 读取文件内容后再替换，避免误修改。", NewContent: null);
        }

        file.Refresh();
        if (file.Length != snapshot.Length || file.LastWriteTimeUtc != snapshot.LastWriteTime)
        {
            return new StringReplaceOutcome(Success: false, Message: $"文件自读取后已被外部修改: {GetDisplayPath(file.FullName)}。请重新使用 ReadFileLines 读取最新内容后再替换。", NewContent: null);
        }

        string content = File.ReadAllText(file.FullName);
        string displayPath = GetDisplayPath(file.FullName);

        var replacer = new WorkspaceFileStringReplacer();
        var outcome = replacer.ReplaceInContent(content, oldString, newString, displayPath);

        if (!outcome.Success)
        {
            return outcome;
        }

        File.WriteAllText(file.FullName, outcome.NewContent);
        UpdateFileSnapshot(file);

        return outcome;
    }

    private void UpdateFileSnapshot(FileInfo file)
    {
        file.Refresh();
        _readFileSnapshots[NormalizePath(file.FullName)] = new FileSnapshotInfo(file.Length, file.LastWriteTimeUtc);
    }
}
