using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.AI;

namespace AgentLib.ChatRoom.Tools.Coding;

/// <summary>
/// 提供限定在代码工作区内的 .NET 构建与测试工具。
/// </summary>
public sealed class DotNetCliTools
{
    private const int DefaultMaxOutputCharacters = 20000;
    private const int MaxErrorPreviewCharacters = 500;
    private const int MaxLineCharacters = 2000;
    private const int MaxQueryLinesReturn = 200;
    private const int MaxSearchMatches = 200;
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(2);
    private readonly string _workspacePath;
    private LogSnapshot? _lastLogSnapshot;

    /// <summary>
    /// 使用指定代码工作区创建 .NET CLI 工具。
    /// </summary>
    /// <param name="workspacePath">代码工作区根目录。</param>
    public DotNetCliTools(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("代码工作区路径不能为空。", nameof(workspacePath));
        }

        _workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(_workspacePath))
        {
            throw new DirectoryNotFoundException("指定的代码工作区不存在。");
        }
    }

    /// <summary>
    /// 创建可按角色授权的 .NET 构建与测试工具集合。
    /// </summary>
    /// <returns>包含 <c>run_build</c> 和 <c>run_tests</c> 的工具集合。</returns>
    public IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(RunBuildAsync, "run_build"),
        AIFunctionFactory.Create(RunTestsAsync, "run_tests"),
        AIFunctionFactory.Create(ReadLastLogLines, "read_last_log_lines"),
        AIFunctionFactory.Create(SearchLastLog, "search_last_log")
    ];

    /// <summary>
    /// 使用 <c>dotnet build</c> 构建工作区或工作区内指定的解决方案或项目。
    /// </summary>
    /// <param name="targetPath">可选的解决方案或项目路径。</param>
    /// <param name="cancellationToken">用于取消构建的令牌。</param>
    /// <returns>构建输出、退出码和执行结果。</returns>
    [Description("使用 dotnet build 构建代码工作区或工作区内指定的解决方案/项目，并返回构建输出和退出码。")]
    public Task<string> RunBuildAsync(
        [Description("可选的解决方案或项目路径。可以传工作区内的绝对路径；相对路径则相对于代码工作区。留空表示构建整个工作区。")]
        string? targetPath = null,
        CancellationToken cancellationToken = default)
    {
        return RunDotNetCommandAsync("build", targetPath, cancellationToken);
    }

    /// <summary>
    /// 使用 <c>dotnet test</c> 测试工作区或工作区内指定的解决方案或项目。
    /// </summary>
    /// <param name="targetPath">可选的解决方案或项目路径。</param>
    /// <param name="cancellationToken">用于取消测试的令牌。</param>
    /// <returns>测试输出、退出码和执行结果。</returns>
    [Description("使用 dotnet test 测试代码工作区或工作区内指定的解决方案/项目，并返回测试输出和退出码。")]
    public Task<string> RunTestsAsync(
        [Description("可选的解决方案或项目路径。可以传工作区内的绝对路径；相对路径则相对于代码工作区。留空表示测试整个工作区。")]
        string? targetPath = null,
        CancellationToken cancellationToken = default)
    {
        return RunDotNetCommandAsync("test", targetPath, cancellationToken);
    }

    [Description("按行读取最后一次构建/测试日志，行号从 1 开始，闭区间 [startLine, endLine]。返回带行号的日志片段或错误信息。")]
    public string ReadLastLogLines(
        [Description("起始行号，从 1 开始。")]
        int startLine,
        [Description("结束行号，必须大于等于起始行号。")]
        int endLine)
    {
        LogSnapshot? snapshot = _lastLogSnapshot;

        if (snapshot is null)
        {
            return "没有可用的日志。";
        }

        if (startLine < 1 || endLine < startLine)
        {
            return "参数错误：请使用从 1 开始的有效行号，且 end_line >= start_line。";
        }

        int total = snapshot.Lines.Length;
        if (startLine > total)
        {
            return $"开始行号超出日志总行数：{total} 行。";
        }

        int adjustedEnd = Math.Min(endLine, total);
        int count = adjustedEnd - startLine + 1;
        bool reachedLineLimit = count > MaxQueryLinesReturn;
        if (reachedLineLimit)
        {
            adjustedEnd = startLine + MaxQueryLinesReturn - 1;
            count = MaxQueryLinesReturn;
        }

        var builder = new StringBuilder();
        builder.AppendLine("<MetaData>");
        builder.AppendLine($"日志总行数: {total}");
        builder.AppendLine($"返回行范围: {startLine}-{adjustedEnd}");
        if (reachedLineLimit)
        {
            int requestedEnd = Math.Min(endLine, total);
            builder.AppendLine($"截断: 单次最多返回 {MaxQueryLinesReturn} 行，仍有 {requestedEnd - adjustedEnd} 行请求内容未显示");
        }
        builder.AppendLine("</MetaData>");

        for (int i = 0; i < count; i++)
        {
            int lineNumber = startLine + i;
            string content = snapshot.Lines[lineNumber - 1];
            builder.AppendLine($"{lineNumber}: {TruncateLine(content)}");
        }

        return builder.ToString().TrimEnd();
    }

    [Description("使用正则表达式在最后一次构建/测试日志中逐行搜索，返回匹配行的行号与内容。支持超时保护。")]
    public string SearchLastLog(
        [Description("正则表达式模式。")]
        string pattern)
    {
        LogSnapshot? snapshot = _lastLogSnapshot;

        if (snapshot is null)
        {
            return "没有可用的日志。";
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexMatchTimeout);
        }
        catch (ArgumentException ex)
        {
            return $"无效的正则表达式：{ex.Message}";
        }

        var matches = new List<string>(MaxSearchMatches);
        int total = snapshot.Lines.Length;
        bool isTruncated = false;
        for (int i = 0; i < total; i++)
        {
            string line = snapshot.Lines[i];
            try
            {
                if (regex.IsMatch(line))
                {
                    if (matches.Count >= MaxSearchMatches)
                    {
                        isTruncated = true;
                        break;
                    }

                    matches.Add($"{i + 1}: {TruncateLine(line)}");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return "正则匹配超时，请简化表达式或缩小范围。";
            }
        }

        if (matches.Count == 0)
        {
            return "未找到匹配项。";
        }

        var builder = new StringBuilder();
        builder.AppendLine("<MetaData>");
        builder.AppendLine($"日志总行数: {total}");
        builder.AppendLine($"返回匹配数: {matches.Count}");
        if (isTruncated)
        {
            builder.AppendLine($"截断: 最多返回 {MaxSearchMatches} 个匹配，仍有匹配项未显示");
        }
        builder.AppendLine("</MetaData>");

        foreach (string match in matches)
        {
            builder.AppendLine(match);
        }

        return builder.ToString().TrimEnd();
    }

    private async Task<string> RunDotNetCommandAsync(string command, string? targetPath, CancellationToken cancellationToken)
    {
        if (!TryResolveTarget(targetPath, out string? resolvedTargetPath, out string errorMessage))
        {
            return errorMessage;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _workspacePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add(command);
        if (resolvedTargetPath is not null)
        {
            startInfo.ArgumentList.Add(resolvedTargetPath);
        }

        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return $"无法启动 dotnet {command}。";
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            string standardOutput = await standardOutputTask.ConfigureAwait(false);
            string standardError = await standardErrorTask.ConfigureAwait(false);

            // 将完整日志保存到实例内存
            string full = FormatResult(command, resolvedTargetPath, process.ExitCode, standardOutput, standardError);
            string[] lines = full.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var snapshot = new LogSnapshot(command, resolvedTargetPath, process.ExitCode, standardOutput, standardError, lines);
            _lastLogSnapshot = snapshot;

            // 返回简短摘要
            int totalLines = lines.Length;
            if (process.ExitCode == 0)
            {
                return $"执行成功。完整日志共 {totalLines} 行，可使用 read_last_log_lines 按行读取。";
            }

            // 查找首个包含 error 的行（不区分大小写）
            string? firstErrorLine = lines.FirstOrDefault(l => l.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrEmpty(firstErrorLine))
            {
                string preview = firstErrorLine.Length <= MaxErrorPreviewCharacters
                    ? firstErrorLine
                    : $"{firstErrorLine[..MaxErrorPreviewCharacters]}…【该错误行已截断】";
                return $"执行失败。完整日志共 {totalLines} 行。首个包含 error 的行：{preview}";
            }

            return $"执行失败。完整日志共 {totalLines} 行，可使用 read_last_log_lines 按行读取。";
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }
        catch (Win32Exception ex)
        {
            return $"无法启动 dotnet {command}: {ex.Message}";
        }
    }

    private static string TruncateLine(string line)
    {
        if (line.Length <= MaxLineCharacters)
        {
            return line;
        }

        int omittedCharacters = line.Length - MaxLineCharacters;
        return $"{line[..MaxLineCharacters]}…【该行过长，后续 {omittedCharacters} 个字符未显示】";
    }

    private bool TryResolveTarget(string? targetPath, out string? resolvedTargetPath, out string errorMessage)
    {
        resolvedTargetPath = null;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            errorMessage = string.Empty;
            return true;
        }

        string fullPath = Path.IsPathRooted(targetPath)
            ? Path.GetFullPath(targetPath)
            : Path.GetFullPath(Path.Join(_workspacePath, targetPath));
        if (!IsPathInsideWorkspace(fullPath))
        {
            errorMessage = $"目标不在代码工作区范围内: {targetPath}";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            errorMessage = $"解决方案或项目不存在: {ToDisplayPath(fullPath)}";
            return false;
        }

        string extension = Path.GetExtension(fullPath);
        if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = $"仅支持 .sln、.slnx、.csproj、.vbproj 或 .fsproj 文件: {ToDisplayPath(fullPath)}";
            return false;
        }

        resolvedTargetPath = fullPath;
        errorMessage = string.Empty;
        return true;
    }

    private string FormatResult(string command, string? targetPath, int exitCode, string standardOutput, string standardError)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"命令: dotnet {command}");
        builder.AppendLine($"目标: {(targetPath is null ? "." : ToDisplayPath(targetPath))}");
        builder.AppendLine($"退出码: {exitCode}");
        builder.AppendLine($"结果: {(exitCode == 0 ? "成功" : "失败")}");

        AppendOutput(builder, "标准输出", standardOutput);
        AppendOutput(builder, "标准错误", standardError);
        return builder.ToString().TrimEnd();
    }

    private static void AppendOutput(StringBuilder builder, string title, string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        string trimmedOutput = output.TrimEnd();
        builder.AppendLine();
        builder.AppendLine($"<{title}>");
        if (trimmedOutput.Length <= DefaultMaxOutputCharacters)
        {
            builder.AppendLine(trimmedOutput);
            builder.AppendLine($"</{title}>");
            return;
        }

        int omittedCharacters = trimmedOutput.Length - DefaultMaxOutputCharacters;
        builder.AppendLine($"【输出共 {trimmedOutput.Length} 个字符，已达到 {DefaultMaxOutputCharacters} 个字符的显示限制。前 {omittedCharacters} 个字符未显示，以下保留最后 {DefaultMaxOutputCharacters} 个字符。】");
        builder.AppendLine(trimmedOutput[^DefaultMaxOutputCharacters..]);
        builder.AppendLine($"</{title}>");
    }

    private bool IsPathInsideWorkspace(string fullPath)
    {
        if (string.Equals(_workspacePath, fullPath, GetPathComparison()))
        {
            return true;
        }

        return fullPath.StartsWith(_workspacePath + Path.DirectorySeparatorChar, GetPathComparison());
    }

    private string ToDisplayPath(string fullPath)
    {
        return Path.GetRelativePath(_workspacePath, fullPath);
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    private sealed class LogSnapshot
    {
        public LogSnapshot(string command, string? targetPath, int exitCode, string standardOutput, string standardError, string[] lines)
        {
            Command = command;
            TargetPath = targetPath;
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            Lines = lines;
            CreatedAt = DateTime.UtcNow;
        }

        public string Command { get; }
        public string? TargetPath { get; }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
        public string[] Lines { get; }
        public DateTime CreatedAt { get; }
    }
}
