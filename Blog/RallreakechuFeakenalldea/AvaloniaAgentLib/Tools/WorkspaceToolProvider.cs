using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AvaloniaAgentLib.Tools;

/// <summary>
/// 为 Copilot 提供基于工作路径的默认文件系统工具。
/// </summary>
public sealed class WorkspaceToolProvider
{
    private const int DefaultMaxResults = 100;
    private const int DefaultMaxCharacters = 4000;
    private const int DefaultMaxRangeLines = 400;
    private const int DefaultMaxLineHitsPerFile = 20;

    public string? WorkspacePath
    {
        get => _workspacePath;
        set => _workspacePath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private string? _workspacePath;

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        return
        [
            CreateTool(nameof(ListDirectory), "列出工作路径下指定目录中的文件与子目录。"),
            CreateTool(nameof(ReadFile), "读取工作路径下指定文件的开头内容。"),
            CreateTool(nameof(FindEntriesByName), "在工作路径下递归查找名称包含指定关键字的文件或文件夹。"),
            CreateTool(nameof(FindFilesContainingText), "在工作路径下递归查找包含指定文本的文件，并返回命中文件路径与行号。"),
            CreateTool(nameof(ReadFileLines), "读取工作路径下指定文件的某一段行内容。")
        ];
    }

    [Description("列出工作路径下指定目录中的文件与子目录。")]
    public string ListDirectory(
        [Description("要访问的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示工作路径根目录。")]
        string? directoryPath = null,
        [Description("是否递归列出子目录。false 表示只列出当前目录。")]
        bool recursive = false,
        [Description("最多返回多少个结果。")]
        int maxResults = DefaultMaxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        if (!TryResolveDirectory(directoryPath, out var directory, out var errorMessage))
        {
            return errorMessage;
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
            return builder.ToString();
        }

        foreach (var entry in entries.Take(maxResults))
        {
            builder.AppendLine($"{GetEntryKind(entry)} {GetDisplayPath(entry.FullName)}");
        }

        if (entries.Count > maxResults)
        {
            builder.Append($"已截断，仍有 {entries.Count - maxResults} 个结果未显示。");
        }

        return builder.ToString().TrimEnd();
    }

    [Description("读取工作路径下指定文件的开头内容。")]
    public string ReadFile(
        [Description("要读取的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。")]
        string filePath,
        [Description("最多返回多少个字符。")]
        int maxCharacters = DefaultMaxCharacters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (maxCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharacters));
        }

        if (!TryResolveFile(filePath, out var file, out var errorMessage))
        {
            return errorMessage;
        }

        var (content, isTruncated) = ReadFileSnippet(file.FullName, maxCharacters);
        var builder = new StringBuilder();
        builder.AppendLine($"文件: {GetDisplayPath(file.FullName)}");
        builder.AppendLine();
        builder.Append(content);

        if (isTruncated)
        {
            builder.AppendLine();
            builder.Append($"已截断，文件后续内容未显示。可使用 {nameof(ReadFileLines)} 继续读取指定行范围。");
        }

        return builder.ToString().TrimEnd();
    }

    [Description("在工作路径下递归查找名称包含指定关键字的文件或文件夹。")]
    public string FindEntriesByName(
        [Description("名称中要包含的关键字。")]
        string query,
        [Description("要搜索的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示从工作路径根目录开始搜索。")]
        string? directoryPath = null,
        [Description("是否包含文件。")]
        bool includeFiles = true,
        [Description("是否包含文件夹。")]
        bool includeDirectories = true,
        [Description("最多返回多少个结果。")]
        int maxResults = DefaultMaxResults)
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
            return errorMessage;
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
            return builder.ToString();
        }

        foreach (var entry in entries.Take(maxResults))
        {
            builder.AppendLine($"{GetEntryKind(entry)} {GetDisplayPath(entry.FullName)}");
        }

        if (entries.Count > maxResults)
        {
            builder.Append($"已截断，仍有至少 1 个匹配项未显示。");
        }

        return builder.ToString().TrimEnd();
    }

    [Description("在工作路径下递归查找包含指定文本的文件，并返回命中文件路径与行号。")]
    public string FindFilesContainingText(
        [Description("要搜索的文本。")]
        string query,
        [Description("要搜索的目录路径。可以传绝对路径；相对路径则相对于当前工作路径。留空表示从工作路径根目录开始搜索。")]
        string? directoryPath = null,
        [Description("最多返回多少个命中文件。")]
        int maxResults = DefaultMaxResults)
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

        var results = new List<(string Path, List<int> LineNumbers, bool IsTruncated)>();
        foreach (var file in EnumerateFilesRecursively(directory))
        {
            List<int> lineNumbers = [];
            int lineNumber = 0;

            try
            {
                using var reader = new StreamReader(file.FullName, detectEncodingFromByteOrderMarks: true);
                while (reader.ReadLine() is { } line)
                {
                    lineNumber++;
                    if (!line.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (lineNumbers.Count < DefaultMaxLineHitsPerFile)
                    {
                        lineNumbers.Add(lineNumber);
                    }
                    else
                    {
                        results.Add((GetDisplayPath(file.FullName), lineNumbers, IsTruncated: true));
                        goto AddResultCompleted;
                    }
                }
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            if (lineNumbers.Count == 0)
            {
                continue;
            }

            results.Add((GetDisplayPath(file.FullName), lineNumbers, IsTruncated: false));

        AddResultCompleted:
            if (results.Count >= maxResults)
            {
                break;
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"工作路径: {GetWorkspaceRootDisplayText()}");
        builder.AppendLine($"搜索目录: {GetDisplayPath(directory.FullName)}");
        builder.AppendLine($"关键字: {query}");

        if (results.Count == 0)
        {
            builder.Append("没有找到包含该文本的文件。");
            return builder.ToString();
        }

        foreach (var result in results)
        {
            builder.Append(result.Path);
            builder.Append(": ");
            builder.Append(string.Join(", ", result.LineNumbers));
            if (result.IsTruncated)
            {
                builder.Append(" ...");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    [Description("读取工作路径下指定文件的某一段行内容。")]
    public string ReadFileLines(
        [Description("要读取的文件路径。可以传绝对路径；相对路径则相对于当前工作路径。")]
        string filePath,
        [Description("起始行号，从 1 开始。")]
        int startLine,
        [Description("结束行号，包含该行。")]
        int endLine)
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

        var builder = new StringBuilder();
        builder.AppendLine($"文件: {GetDisplayPath(file.FullName)}");
        builder.AppendLine($"行范围: {startLine}-{endLine}");
        builder.AppendLine();

        int currentLine = 0;
        bool hasContent = false;

        using var reader = new StreamReader(file.FullName, detectEncodingFromByteOrderMarks: true);
        while (reader.ReadLine() is { } line)
        {
            currentLine++;
            if (currentLine < startLine)
            {
                continue;
            }

            if (currentLine > endLine)
            {
                break;
            }

            builder.AppendLine($"{currentLine}: {line}");
            hasContent = true;
        }

        if (!hasContent)
        {
            builder.Append("指定范围内没有可读取的内容。");
        }

        return builder.ToString().TrimEnd();
    }

    private AITool CreateTool(string methodName, string description)
    {
        MethodInfo methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
                                ?? throw new InvalidOperationException($"未找到 {methodName} 方法。");
        return AIFunctionFactory.Create(methodInfo, this, methodName, description, serializerOptions: null);
    }

    private bool TryResolveDirectory(string? path, out DirectoryInfo directory, out string errorMessage)
    {
        if (!TryResolvePath(path, out string fullPath, out errorMessage))
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
        if (!TryResolvePath(path, out string fullPath, out errorMessage))
        {
            file = null!;
            return false;
        }

        file = new FileInfo(fullPath);
        if (!file.Exists)
        {
            errorMessage = $"文件不存在: {GetDisplayPath(fullPath)}";
            return false;
        }

        return true;
    }

    private bool TryResolvePath(string? path, out string fullPath, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (string.IsNullOrWhiteSpace(WorkspacePath))
            {
                fullPath = string.Empty;
                errorMessage = "当前未设置工作路径，无法使用默认文件工具。";
                return false;
            }

            fullPath = NormalizePath(WorkspacePath);
            errorMessage = string.Empty;
            return true;
        }

        if (Path.IsPathRooted(path))
        {
            fullPath = NormalizePath(path);
            errorMessage = string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(WorkspacePath))
        {
            fullPath = string.Empty;
            errorMessage = $"当前未设置工作路径，无法解析相对路径: {path}";
            return false;
        }

        string workspaceRoot = NormalizePath(WorkspacePath);
        fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, path));

        if (!IsPathInsideWorkspace(workspaceRoot, fullPath))
        {
            errorMessage = $"路径超出了当前工作路径范围: {path}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
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

    private static (string Content, bool IsTruncated) ReadFileSnippet(string filePath, int maxCharacters)
    {
        using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
        char[] buffer = new char[Math.Min(1024, maxCharacters)];
        var builder = new StringBuilder(Math.Min(maxCharacters, 4096));
        int remainingCharacters = maxCharacters;

        while (remainingCharacters > 0)
        {
            int currentReadLength = Math.Min(buffer.Length, remainingCharacters);
            int readLength = reader.Read(buffer, 0, currentReadLength);
            if (readLength == 0)
            {
                return (builder.ToString(), isTruncated: false);
            }

            builder.Append(buffer, 0, readLength);
            remainingCharacters -= readLength;
        }

        return (builder.ToString(), isTruncated: reader.Peek() >= 0);
    }

    private string GetDisplayPath(string fullPath)
    {
        string? workspacePath = WorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return fullPath;
        }

        string relativePath = Path.GetRelativePath(NormalizePath(workspacePath), fullPath);
        return relativePath == "." ? "." : relativePath;
    }

    private string GetWorkspaceRootDisplayText()
    {
        string? workspacePath = WorkspacePath;
        return string.IsNullOrWhiteSpace(workspacePath) ? "<未设置>" : NormalizePath(workspacePath);
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
}
