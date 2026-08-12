using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 提供 .NET 构建与测试工具。
/// </summary>
public sealed class DotNetCliTools
{
    private const int DefaultMaxOutputCharacters = 20000;
    private static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromMinutes(5);
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
    /// <returns>包含 <c>run_build</c>、<c>run_msbuild</c> 和 <c>run_tests</c> 等功能的工具集合。</returns>
    public IReadOnlyList<AITool> AsAITools() => AsToolRegistrations().Select(registration => registration.Tool).ToArray();

    /// <summary>
    /// 创建 .NET 工具及其展示摘要规则。
    /// </summary>
    public IReadOnlyList<ToolRegistration> AsToolRegistrations() =>
    [
        new(AIFunctionFactory.Create(RunBuildAsync, "run_build"), ToolCallPresentationFactory.ForBuild),
        new(AIFunctionFactory.Create(RunMSBuildAsync, "run_msbuild"), ToolCallPresentationFactory.ForBuild),
        new(AIFunctionFactory.Create(RunDotNetPublishAsync, "RunDotNetPublish"), arguments => ToolCallPresentationFactory.ForQuery(arguments, "commandLine")),
        new(AIFunctionFactory.Create(RunTestsAsync, "run_tests"), arguments => ToolCallPresentationFactory.ForTestRun(arguments, "targetPath", "filter")),
        new(AIFunctionFactory.Create(ReadLastLogLines, "read_last_log_lines"), arguments => new ToolCallPresentation(null,
                FormatLineRange(arguments, "startLine", "endLine"))),
        new(AIFunctionFactory.Create(SearchLastLog, "search_last_log"), arguments => ToolCallPresentationFactory.ForQuery(arguments, "pattern"))
    ];

    private static string? FormatLineRange(IDictionary<string, object?> arguments, string startName, string endName)
    {
        int? start = ToolCallPresentationFactory.GetInt32(arguments, startName);
        int? end = ToolCallPresentationFactory.GetInt32(arguments, endName);
        return start is null ? null : end == start ? $"第 {start} 行" : end is null ? $"从第 {start} 行开始" : $"第 {start}–{end} 行";
    }

    /// <summary>
    /// 使用 <c>dotnet build</c> 构建工作区或指定目标。
    /// </summary>
    /// <param name="targetPath">可选的解决方案或项目路径。</param>
    /// <param name="configuration">可选的构建配置，例如 <c>Debug</c> 或 <c>Release</c>。</param>
    /// <param name="runtimeIdentifier">可选的目标运行时标识符，例如 <c>linux-x64</c> 或 <c>win-x64</c>。</param>
    /// <param name="targetFramework">可选的目标框架，例如 <c>net8.0</c> 或 <c>net9.0</c>。</param>
    /// <param name="cancellationToken">用于取消构建的令牌。</param>
    /// <returns>构建输出、退出码和执行结果。</returns>
    [Description("使用 dotnet build 构建代码工作区或指定目标，可设置构建配置、运行时标识符和目标框架，并返回构建输出和退出码。")]
    public Task<string> RunBuildAsync(
        [Description("可选的构建目标路径。可以传绝对路径；相对路径则相对于代码工作区。留空表示构建整个工作区。")]
        string? targetPath = null,
        [Description("可选的构建配置，例如 Debug 或 Release。留空时使用项目默认值。")]
        string? configuration = null,
        [Description("可选的目标运行时标识符，例如 linux-x64 或 win-x64。留空时不指定运行时。")]
        string? runtimeIdentifier = null,
        [Description("可选的目标框架，例如 net8.0 或 net9.0。留空时不指定目标框架。")]
        string? targetFramework = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>(6);
        AddOption(arguments, "--configuration", configuration);
        AddOption(arguments, "--runtime", runtimeIdentifier);
        AddOption(arguments, "--framework", targetFramework);
        return RunDotNetCommandAsync("build", targetPath, cancellationToken, arguments);
    }

    /// <summary>
    /// 使用本机安装的 <c>MSBuild.exe</c> 构建工作区或指定目标。
    /// </summary>
    /// <param name="targetPath">可选的解决方案或项目路径。</param>
    /// <param name="configuration">可选的构建配置，例如 <c>Debug</c> 或 <c>Release</c>。</param>
    /// <param name="runtimeIdentifier">可选的目标运行时标识符，例如 <c>linux-x64</c> 或 <c>win-x64</c>。</param>
    /// <param name="targetFramework">可选的目标框架，例如 <c>net8.0</c> 或 <c>net9.0</c>。</param>
    /// <param name="cancellationToken">用于取消构建的令牌。</param>
    /// <returns>构建输出、退出码和执行结果；未找到 MSBuild 时返回错误信息。</returns>
    [Description("使用本机安装的最新 MSBuild.exe 构建代码工作区或指定目标，可设置构建配置、运行时标识符和目标框架，并返回构建输出和退出码。")]
    public Task<string> RunMSBuildAsync(
        [Description("可选的构建目标路径。可以传绝对路径；相对路径则相对于代码工作区。留空时由 MSBuild 在工作区中查找项目或解决方案。")]
        string? targetPath = null,
        [Description("可选的构建配置，例如 Debug 或 Release。留空时使用项目默认值。")]
        string? configuration = null,
        [Description("可选的目标运行时标识符，例如 linux-x64 或 win-x64。留空时不指定运行时。")]
        string? runtimeIdentifier = null,
        [Description("可选的目标框架，例如 net8.0 或 net9.0。留空时不指定目标框架。")]
        string? targetFramework = null,
        CancellationToken cancellationToken = default)
    {
        string? msBuildPath = FindInstalledMSBuildFilePath();
        if (msBuildPath is null)
        {
            return Task.FromResult("未找到已安装的 MSBuild.exe。支持查找 Visual Studio 2026、2022 和 2019，且同版本优先使用企业版、专业版、个人版。");
        }

        var arguments = new List<string>(3);
        AddMSBuildProperty(arguments, "Configuration", configuration);
        AddMSBuildProperty(arguments, "RuntimeIdentifier", runtimeIdentifier);
        AddMSBuildProperty(arguments, "TargetFramework", targetFramework);
        return RunProcessCommandAsync(msBuildPath, "MSBuild", targetPath, cancellationToken, arguments);

        static void AddMSBuildProperty(List<string> arguments, string propertyName, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            arguments.Add($"/property:{propertyName}={value}");
        }
    }

    /// <summary>
    /// 执行以 <c>dotnet publish</c> 开头的完整发布命令行。
    /// </summary>
    /// <param name="commandLine">必须严格以 <c>dotnet publish</c> 开头的完整命令行。</param>
    /// <param name="cancellationToken">用于取消发布的令牌。</param>
    /// <returns>发布输出、退出码和执行结果。</returns>
    [Description("执行以 dotnet publish 开头的完整命令行发布 .NET 项目。命令必须严格从 dotnet publish 开始，不允许前导空白、换行或空字符；命令不会通过 Shell 执行。")]
    public Task<string> RunDotNetPublishAsync(
        [Description("完整发布命令行，必须严格以 dotnet publish 开头，例如 dotnet publish MyApp.csproj -c Release -r win-x64。")]
        string commandLine,
        CancellationToken cancellationToken = default)
    {
        const string requiredPrefix = "dotnet publish";
        if (string.IsNullOrEmpty(commandLine)
            || !commandLine.StartsWith(requiredPrefix, StringComparison.Ordinal)
            || commandLine.Length > requiredPrefix.Length && !char.IsWhiteSpace(commandLine[requiredPrefix.Length])
            || commandLine.IndexOfAny(['\r', '\n', '\0']) >= 0)
        {
            return Task.FromResult("命令行必须严格以 dotnet publish 开头，且不能包含前导空白、换行或空字符。");
        }

        string arguments = commandLine["dotnet ".Length..];
        return RunProcessCommandAsync(
            "dotnet",
            commandLine,
            targetPath: null,
            cancellationToken,
            rawArguments: arguments);
    }

    /// <summary>
    /// 使用 <c>dotnet test</c> 测试工作区或指定目标。
    /// </summary>
    /// <param name="targetPath">可选的解决方案或项目路径。</param>
    /// <param name="filter">可选的测试筛选表达式，语法与 <c>dotnet test --filter</c> 一致。</param>
    /// <param name="cancellationToken">用于取消测试的令牌。</param>
    /// <returns>测试输出、退出码和执行结果。</returns>
    [Description("使用 dotnet test 测试代码工作区或指定目标，可通过筛选表达式只运行部分测试，并返回测试输出和退出码。")]
    public async Task<string> RunTestsAsync(
        [Description("可选的测试目标路径。可以传绝对路径；相对路径则相对于代码工作区。留空表示测试整个工作区。")]
        string? targetPath = null,
        [Description("可选的测试筛选表达式，语法与 dotnet test --filter 一致。留空表示运行全部测试。")]
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellationTokenSource.CancelAfter(DefaultTestTimeout);

        try
        {
            var arguments = new List<string>(2);
            AddOption(arguments, "--filter", filter);
            return await RunDotNetCommandAsync("test", targetPath, timeoutCancellationTokenSource.Token, arguments).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCancellationTokenSource.IsCancellationRequested)
        {
            return "测试执行已超过默认的 5 分钟超时时间，测试进程已终止。";
        }
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

    private async Task<string> RunDotNetCommandAsync(
        string command,
        string? targetPath,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? arguments = null)
    {
        return await RunProcessCommandAsync("dotnet", $"dotnet {command}", targetPath, cancellationToken, arguments, [command]).ConfigureAwait(false);
    }

    private async Task<string> RunProcessCommandAsync(
        string fileName,
        string commandDisplayName,
        string? targetPath,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? arguments = null,
        IReadOnlyList<string>? argumentsBeforeTarget = null,
        string? rawArguments = null)
    {
        if (!TryResolveTarget(targetPath, out string? resolvedTargetPath, out string errorMessage))
        {
            return errorMessage;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = _workspacePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        if (rawArguments is not null)
        {
            startInfo.Arguments = rawArguments;
        }
        else
        {
            if (argumentsBeforeTarget is not null)
            {
                foreach (string argument in argumentsBeforeTarget)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }
            if (resolvedTargetPath is not null)
            {
                startInfo.ArgumentList.Add(resolvedTargetPath);
            }
            if (arguments is not null)
            {
                foreach (string argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }
        }

        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return $"无法启动 {commandDisplayName}。";
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            string standardOutput = await standardOutputTask.ConfigureAwait(false);
            string standardError = await standardErrorTask.ConfigureAwait(false);

            // 将完整日志保存到实例内存
            string full = FormatResult(commandDisplayName, resolvedTargetPath, arguments, process.ExitCode, standardOutput, standardError);
            string[] lines = full.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var snapshot = new LogSnapshot(commandDisplayName, resolvedTargetPath, process.ExitCode, standardOutput, standardError, lines);
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
            return $"无法启动 {commandDisplayName}: {ex.Message}";
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

    private static void AddOption(List<string> arguments, string option, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add(option);
        arguments.Add(value);
    }

    private static string? FindInstalledMSBuildFilePath()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string[] candidatePaths =
        [
            Path.Join(programFiles, "Microsoft Visual Studio", "18", "Enterprise", "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe"),
            Path.Join(programFiles, "Microsoft Visual Studio", "18", "Professional", "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe"),
            Path.Join(programFiles, "Microsoft Visual Studio", "18", "Community", "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe"),
            Path.Join(programFiles, "Microsoft Visual Studio", "2022", "Enterprise", "MSBuild", "Current", "Bin", "MSBuild.exe"),
            Path.Join(programFiles, "Microsoft Visual Studio", "2022", "Professional", "MSBuild", "Current", "Bin", "MSBuild.exe"),
            Path.Join(programFiles, "Microsoft Visual Studio", "2022", "Community", "MSBuild", "Current", "Bin", "MSBuild.exe"),
            Path.Join(programFilesX86, "Microsoft Visual Studio", "2019", "Enterprise", "MSBuild", "Current", "Bin", "MSBuild.exe"),
            Path.Join(programFilesX86, "Microsoft Visual Studio", "2019", "Professional", "MSBuild", "Current", "Bin", "MSBuild.exe"),
            Path.Join(programFilesX86, "Microsoft Visual Studio", "2019", "Community", "MSBuild", "Current", "Bin", "MSBuild.exe")
        ];

        return candidatePaths.FirstOrDefault(File.Exists);
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
        if (!File.Exists(fullPath))
        {
            errorMessage = $"目标文件不存在: {ToDisplayPath(fullPath)}";
            return false;
        }

        resolvedTargetPath = fullPath;
        errorMessage = string.Empty;
        return true;
    }

    private string FormatResult(
        string command,
        string? targetPath,
        IReadOnlyList<string>? arguments,
        int exitCode,
        string standardOutput,
        string standardError)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"命令: {command}");
        builder.AppendLine($"目标: {(targetPath is null ? "." : ToDisplayPath(targetPath))}");
        if (arguments is { Count: > 0 })
        {
            builder.AppendLine($"附加参数: {string.Join(' ', arguments)}");
        }
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

    private string ToDisplayPath(string fullPath)
    {
        return Path.GetRelativePath(_workspacePath, fullPath);
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
