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

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        return
        [
            AIFunctionFactory.Create(ListDirectory, name: nameof(ListDirectory), description: "列出工作路径下指定目录中的文件与子目录。"),
            //AIFunctionFactory.Create(ReadFile, name: nameof(ReadFile), description: "读取工作路径下指定文件的开头内容。"),
            AIFunctionFactory.Create(FindEntriesByName, name: nameof(FindEntriesByName), description: "在工作路径下递归查找名称包含指定关键字的文件或文件夹。"),
            AIFunctionFactory.Create(FindFilesMatchingPattern, name: nameof(FindFilesMatchingPattern), description: "在工作路径下递归查找匹配指定模式的文件，并返回命中文件路径与行号。支持纯文本匹配和正则表达式匹配。"),
            AIFunctionFactory.Create(ReadFileLines, name: nameof(ReadFileLines), description: "读取工作路径下指定文件的某一段行内容。"),
            AIFunctionFactory.Create(WriteFileContent, name: nameof(WriteFileContent), description: "将内容写入工作区内的文件。写入前要求先读取过该文件，防止误覆盖。"),
            AIFunctionFactory.Create(ReplaceStringInFile, name: nameof(ReplaceStringInFile), description: "替换文件中的指定字符串。要求先读取过该文件，且 oldString 在文件中必须唯一匹配。"),
            AIFunctionFactory.Create(MultiReplaceStringInFile, name: nameof(MultiReplaceStringInFile), description: "批量替换文件中的多个字符串。每个替换操作要求先读取过对应文件，且 oldString 在文件中必须唯一匹配。")
        ];
    }

    [Description("列出工作路径下指定目录中的文件与子目录。")] 
    public Task<string> ListDirectory(
        [Description("要访问的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示工作路径根目录。")] string? directoryPath = null,
        [Description("是否递归列出子目录。false 表示只列出当前目录。")] bool recursive = false,
        [Description("最多返回多少个结果。")] int maxResults = DefaultMaxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return Task.FromResult(errorMessage);
        }

        List<FileSystemInfo> entries = recursive
            ? EnumerateEntriesRecursively(directory).ToList()
            : GetDirectoryEntries(directory).ToList();

        entries = entries
            .OrderBy(static entry => entry is FileInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
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
            builder.Append($"已截断，仍有 {entries.Count - maxResults} 个结果未显示。");
        }

        return Task.FromResult(builder.ToString().TrimEnd());
    }

    [Description("在工作路径下递归查找名称包含指定关键字的文件或文件夹。")] 
    public Task<string> FindEntriesByName(
        [Description("名称中要包含的关键字。")] string query,
        [Description("要搜索的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示从工作路径根目录开始搜索。")] string? directoryPath = null,
        [Description("是否包含文件。")] bool includeFiles = true,
        [Description("是否包含文件夹。")] bool includeDirectories = true,
        [Description("最多返回多少个结果。")] int maxResults = DefaultMaxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (!includeFiles && !includeDirectories)
        {
            throw new ArgumentException("文件和文件夹至少需要包含一种。", nameof(includeFiles));
        }

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return Task.FromResult(errorMessage);
        }

        List<FileSystemInfo> entries = EnumerateEntriesRecursively(directory)
            .Where(entry => (includeDirectories && entry is DirectoryInfo || includeFiles && entry is FileInfo)
                && entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static entry => entry is FileInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults + 1)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine($"工作路径: {GetWorkspaceRootDisplayText()}");
        builder.AppendLine($"搜索目录: {GetDisplayPath(directory.FullName)}");
        builder.AppendLine($"关键字: {query}");

        if (entries.Count == 0)
        {
            builder.Append("没有找到匹配项。");
            return Task.FromResult(builder.ToString());
        }

        foreach (var entry in entries.Take(maxResults))
        {
            builder.AppendLine($"{GetEntryKind(entry)} {GetDisplayPath(entry.FullName)}");
        }

        if (entries.Count > maxResults)
        {
            builder.Append("已截断，仍有至少 1 个匹配项未显示。");
        }

        return Task.FromResult(builder.ToString().TrimEnd());
    }

    /// <summary>
    /// 在工作路径下递归查找匹配指定模式的文件，并返回命中文件路径与行号。
    /// 支持纯文本匹配和正则表达式匹配。
    /// </summary>
    /// <param name="query">要匹配的文本或正则表达式模式。</param>
    /// <param name="directoryPath">要搜索的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示从工作路径根目录开始搜索。</param>
    /// <param name="useRegex">是否将 <paramref name="query"/> 作为正则表达式进行匹配。默认为 false，表示纯文本匹配。</param>
    /// <param name="maxResults">最多返回多少个命中文件。</param>
    [Description("在工作路径下递归查找匹配指定模式的文件，并返回命中文件路径与行号。支持纯文本匹配和正则表达式匹配。")]
    public async Task<string> FindFilesMatchingPattern(
        [Description("要匹配的文本或正则表达式模式。")] string query,
        [Description("要搜索的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示从工作路径根目录开始搜索。")] string? directoryPath = null,
        [Description("是否将 query 作为正则表达式进行匹配。默认为 false，表示纯文本匹配。")] bool useRegex = false,
        [Description("最多返回多少个命中文件。")] int maxResults = DefaultMaxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
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
        foreach (var file in EnumerateFilesRecursively(directory))
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

    [Description("读取工作路径下指定文件的某一段行内容。")]
    public async Task<string> ReadFileLines(
        [Description("要读取的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。")] string filePath,
        [Description("起始行号，从 1 开始(1-based)。")] int startLine,
        [Description("结束行号，包含该行(1-based)。")] int endLine,
        [Description("是否在每一行前面包含行号。true 表示添加行号，false 表示只返回原始行内容。")] bool includeLineNumbers = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (startLine <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine));
        }

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine));
        }

        if (endLine - startLine + 1 > DefaultMaxRangeLines)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine), $"单次最多读取 {DefaultMaxRangeLines} 行。");
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

    private static IEnumerable<FileSystemInfo> GetDirectoryEntries(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFileSystemInfos().ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<FileSystemInfo> EnumerateEntriesRecursively(DirectoryInfo rootDirectory)
    {
        var stack = new Stack<DirectoryInfo>();
        stack.Push(rootDirectory);

        while (stack.Count > 0)
        {
            DirectoryInfo currentDirectory = stack.Pop();
            FileSystemInfo[] entries = GetDirectoryEntries(currentDirectory).ToArray();

            foreach (var entry in entries)
            {
                yield return entry;
            }

            foreach (var childDirectory in entries.OfType<DirectoryInfo>().OrderByDescending(directory => directory.FullName, StringComparer.OrdinalIgnoreCase))
            {
                stack.Push(childDirectory);
            }
        }
    }

    private static IEnumerable<FileInfo> EnumerateFilesRecursively(DirectoryInfo rootDirectory)
    {
        foreach (var entry in EnumerateEntriesRecursively(rootDirectory))
        {
            if (entry is FileInfo file)
            {
                yield return file;
            }
        }
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
    /// 将内容写入工作区内的文件。写入前要求先通过 ReadFileLines 读取过该文件，
    /// 且文件自读取后未被外部修改。
    /// </summary>
    /// <param name="filePath">要写入的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。</param>
    /// <param name="content">要写入的内容。</param>
    /// <returns>成功时返回 "OK"，失败时返回错误信息。</returns>
    [Description("将内容写入工作区内的文件。写入前要求先通过 ReadFileLines 读取过该文件，且文件自读取后未被外部修改。")]
    public string WriteFileContent(
        [Description("要写入的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。")] string filePath,
        [Description("要写入的内容。")] string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

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
    [Description("替换文件中的指定字符串。要求先读取过该文件，且 oldString 在文件中必须唯一匹配。")]
    public string ReplaceStringInFile(
        [Description("要替换的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。")] string filePath,
        [Description("要替换的原始文本，必须在文件中唯一匹配。包含前后各 3-5 行上下文以确保唯一性。")] string oldString,
        [Description("替换后的新文本。")] string newString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(oldString);
        ArgumentNullException.ThrowIfNull(newString);

        var result = ReplaceStringInFileCore(filePath, oldString, newString);
        return result.Message;
    }

    /// <summary>
    /// 批量替换文件中的多个字符串。顺序执行每个替换操作，每个操作独立处理错误。
    /// </summary>
    /// <param name="replacements">替换操作列表，每个操作包含文件路径、原始文本、新文本和说明。</param>
    /// <param name="explanation">批量替换操作的总体说明。</param>
    /// <returns>替换操作的汇总结果。</returns>
    [Description("批量替换文件中的多个字符串。顺序执行每个替换操作，每个操作独立处理错误。")]
    public string MultiReplaceStringInFile(
        [Description("替换操作列表，每个操作包含文件路径、原始文本、新文本和说明。")] IReadOnlyList<ReplaceOperation> replacements,
        [Description("批量替换操作的总体说明。")] string explanation)
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
            var result = ReplaceStringInFileCore(operation.FilePath, operation.OldString, operation.NewString);
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
