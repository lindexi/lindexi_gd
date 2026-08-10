using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Text;

using System.Text.RegularExpressions;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Coding.Sandboxes;
using System.Text.RegularExpressions;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Sandboxes;

internal sealed class WindowsSandboxTools
{
    private const int DefaultTimeoutSeconds = 300;
    private const int MaximumTimeoutSeconds = 1800;
    private const int MaximumOutputCharacters = 20000;
    private const string RemoteTasksRoot = @"C:\CodingAgentSandbox\Tasks";
    private static readonly Regex ExitCodeRegex = new(
        @"^__CODING_AGENT_EXIT_CODE_([0-9a-f]{32})=(-?\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private readonly string _workspacePath;
    private readonly IWinRemoteShellRunner _runner;

    internal WindowsSandboxTools(
        string workspacePath,
        IWinRemoteShellRunner runner)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("代码工作区路径不能为空。", nameof(workspacePath));
        }

        ArgumentNullException.ThrowIfNull(runner);
        _workspacePath = Path.GetFullPath(workspacePath);
        _runner = runner;
    }

    internal IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(ExecuteAsync, "execute_in_windows_sandbox")
    ];

    [Description("将工作区内的执行器文件夹推送到 Windows 远程沙盒，在隔离任务目录中执行命令，并把指定结果或整个任务目录拉取回工作区。")]
    internal async Task<string> ExecuteAsync(
        [Description("要推送到沙盒的本地文件夹。可以传绝对路径；相对路径则相对于代码工作区，且必须位于工作区内。")]
        string sourceDirectory,
        [Description("要执行的文件相对于 sourceDirectory 的路径，例如 bin\\Debug\\net8.0\\TestRunner.exe。")]
        string executableRelativePath,
        [Description("执行工作目录相对于 sourceDirectory 的路径。留空表示 sourceDirectory 根目录。")]
        string? workingDirectoryRelativePath = null,
        [Description("传递给执行文件的命令行参数数组。留空表示不传参数。")]
        IReadOnlyList<string>? arguments = null,
        [Description("要从沙盒拉取的文件或文件夹相对路径。留空表示拉取整个远端任务目录。")]
        string? outputRelativePath = null,
        [Description("结果在本地工作区内的保存目录。留空时保存到 .coding-agent\\sandbox-results\\<任务编号>。")]
        string? localOutputDirectory = null,
        [Description("远端执行超时秒数，默认 300 秒，最大 1800 秒。")]
        int timeoutSeconds = DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveSourceDirectory(sourceDirectory, out string fullSourceDirectory, out string validationError)
            || !TryResolveRelativePath(executableRelativePath, "执行文件", out string normalizedExecutablePath, out validationError)
            || !TryResolveOptionalRelativePath(workingDirectoryRelativePath, "执行工作目录", out string normalizedWorkingDirectory, out validationError)
            || !TryResolveOptionalRelativePath(outputRelativePath, "结果路径", out string? normalizedOutputPath, out validationError)
            || !TryResolveLocalOutputDirectory(localOutputDirectory, out string fullLocalOutputDirectory, out validationError))
        {
            return validationError;
        }
        if (timeoutSeconds is < 1 or > MaximumTimeoutSeconds)
        {
            return $"沙盒执行参数无效：超时秒数必须在 1 到 {MaximumTimeoutSeconds} 之间。";
        }
        if (!TryValidateCommandValue(normalizedExecutablePath, "执行文件", out validationError)
            || (normalizedWorkingDirectory is not null
                && !TryValidateCommandValue(normalizedWorkingDirectory, "执行工作目录", out validationError))
            || !TryValidateArguments(arguments, out validationError))
        {
            return validationError;
        }

        string taskId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        string remoteTaskDirectory = $@"{RemoteTasksRoot}\{taskId}";
        string remoteWorkingDirectory = CombineRemotePath(remoteTaskDirectory, normalizedWorkingDirectory);
        string remoteExecutablePath = CombineRemotePath(remoteTaskDirectory, normalizedExecutablePath);
        string remoteOutputPath = normalizedOutputPath is null
            ? remoteTaskDirectory
            : CombineRemotePath(remoteTaskDirectory, normalizedOutputPath);
        fullLocalOutputDirectory = fullLocalOutputDirectory.Replace("{taskId}", taskId, StringComparison.Ordinal);

        try
        {
            await _runner.PushAsync(fullSourceDirectory, remoteTaskDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            return $"沙盒执行失败：推送执行目录失败。原因：{GetFailureMessage(ex)}";
        }

        string marker = $"__CODING_AGENT_EXIT_CODE_{taskId}=";
        string remoteCommand = BuildRemoteCommand(remoteWorkingDirectory, remoteExecutablePath, arguments, marker);
        string executionOutput;
        try
        {
            executionOutput = await _runner.ExecuteAsync(remoteCommand, timeoutSeconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            return $"沙盒执行失败：远端命令执行失败。远端任务目录：{remoteTaskDirectory}。原因：{GetFailureMessage(ex)}";
        }

        Match exitCodeMatch = ExitCodeRegex.Matches(executionOutput)
            .Cast<Match>()
            .LastOrDefault(match => string.Equals(match.Groups[1].Value, taskId, StringComparison.OrdinalIgnoreCase))
            ?? Match.Empty;
        if (!exitCodeMatch.Success
            || !int.TryParse(exitCodeMatch.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int exitCode))
        {
            return FormatResult(
                succeeded: false,
                exitCode: null,
                remoteTaskDirectory,
                localOutputDirectory: null,
                executionOutput,
                "未能从远端输出中读取执行文件退出码，结果文件未拉取。");
        }

        string visibleOutput = ExitCodeRegex.Replace(executionOutput, string.Empty).TrimEnd();
        try
        {
            Directory.CreateDirectory(fullLocalOutputDirectory);
            await _runner.PullAsync(remoteOutputPath, fullLocalOutputDirectory, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsExpectedFailure(ex))
        {
            return FormatResult(
                succeeded: false,
                exitCode,
                remoteTaskDirectory,
                fullLocalOutputDirectory,
                visibleOutput,
                $"远端命令已完成，但拉取结果失败。原因：{GetFailureMessage(ex)}");
        }

        return FormatResult(
            succeeded: exitCode == 0,
            exitCode,
            remoteTaskDirectory,
            fullLocalOutputDirectory,
            visibleOutput,
            exitCode == 0 ? null : "远端执行文件返回非零退出码。");
    }

    private bool TryResolveSourceDirectory(string sourceDirectory, out string fullPath, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            fullPath = string.Empty;
            errorMessage = "沙盒执行参数无效：执行目录不能为空。";
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(Path.IsPathRooted(sourceDirectory)
                ? sourceDirectory
                : Path.Combine(_workspacePath, sourceDirectory));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            fullPath = string.Empty;
            errorMessage = $"沙盒执行参数无效：执行目录路径无效。原因：{ex.Message}";
            return false;
        }

        if (!IsWithinWorkspace(fullPath))
        {
            errorMessage = $"沙盒执行参数无效：执行目录必须位于代码工作区内。路径：{fullPath}";
            return false;
        }
        if (!Directory.Exists(fullPath))
        {
            errorMessage = $"沙盒执行参数无效：执行目录不存在。路径：{fullPath}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private bool TryResolveLocalOutputDirectory(string? path, out string fullPath, out string errorMessage)
    {
        string candidate = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(_workspacePath, ".coding-agent", "sandbox-results", "{taskId}")
            : Path.IsPathRooted(path) ? path : Path.Combine(_workspacePath, path);
        try
        {
            fullPath = Path.GetFullPath(candidate);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            fullPath = string.Empty;
            errorMessage = $"沙盒执行参数无效：本地结果目录路径无效。原因：{ex.Message}";
            return false;
        }

        if (!IsWithinWorkspace(fullPath))
        {
            errorMessage = $"沙盒执行参数无效：本地结果目录必须位于代码工作区内。路径：{fullPath}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryResolveOptionalRelativePath(
        string? path,
        string parameterName,
        out string? normalizedPath,
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            normalizedPath = null;
            errorMessage = string.Empty;
            return true;
        }

        return TryResolveRelativePath(path, parameterName, out normalizedPath, out errorMessage);
    }

    private static bool TryResolveRelativePath(
        string path,
        string parameterName,
        out string normalizedPath,
        out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            normalizedPath = string.Empty;
            errorMessage = $"沙盒执行参数无效：{parameterName}必须是非空相对路径。";
            return false;
        }
        if (path.IndexOfAny(['\r', '\n']) >= 0)
        {
            normalizedPath = string.Empty;
            errorMessage = $"沙盒执行参数无效：{parameterName}不能包含换行符。";
            return false;
        }

        string replacedPath = path.Replace('/', '\\');
        string[] segments = replacedPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            normalizedPath = string.Empty;
            errorMessage = $"沙盒执行参数无效：{parameterName}不能包含当前目录或上级目录片段。";
            return false;
        }

        normalizedPath = string.Join('\\', segments);
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryValidateArguments(IReadOnlyList<string>? arguments, out string errorMessage)
    {
        if (arguments is not null)
        {
            foreach (string? argument in arguments)
            {
                if (!TryValidateCommandValue(argument, "命令行参数", out errorMessage))
                {
                    return false;
                }
            }
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryValidateCommandValue(string? value, string parameterName, out string errorMessage)
    {
        if (value is null || value.IndexOfAny(['\r', '\n', '%', '!', '"']) >= 0)
        {
            errorMessage = $"沙盒执行参数无效：{parameterName}不能为 null，且不能包含换行符、百分号、感叹号或双引号。";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private bool IsWithinWorkspace(string path)
    {
        string relativePath = Path.GetRelativePath(_workspacePath, path);
        return !Path.IsPathRooted(relativePath)
            && relativePath != ".."
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static string BuildRemoteCommand(
        string workingDirectory,
        string executablePath,
        IReadOnlyList<string>? arguments,
        string marker)
    {
        var builder = new StringBuilder("cmd.exe /D /V:ON /S /C \"");
        builder.Append("cd /d ").Append(QuoteForCmd(workingDirectory));
        builder.Append(" && ").Append(QuoteForCmd(executablePath));
        if (arguments is not null)
        {
            foreach (string argument in arguments)
            {
                builder.Append(' ').Append(QuoteForCmd(argument));
            }
        }

        builder.Append(" & echo ").Append(marker).Append("!errorlevel!\"");
        return builder.ToString();
    }

    private static string QuoteForCmd(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        int backslashCount = 0;
        foreach (char character in value)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            builder.Append('\\', backslashCount);
            backslashCount = 0;
            builder.Append(character);
        }

        builder.Append('\\', backslashCount * 2);
        builder.Append('"');
        return builder.ToString();
    }

    private static string CombineRemotePath(string root, string? relativePath) =>
        string.IsNullOrEmpty(relativePath) ? root : $@"{root}\{relativePath}";

    private static bool IsExpectedFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or Win32Exception
            or HttpRequestException
            or TimeoutException;

    private static string GetFailureMessage(Exception exception) =>
        string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message;

    private static string FormatResult(
        bool succeeded,
        int? exitCode,
        string remoteTaskDirectory,
        string? localOutputDirectory,
        string output,
        string? errorMessage)
    {
        string limitedOutput = output.Length <= MaximumOutputCharacters
            ? output
            : $"{output[..MaximumOutputCharacters]}\n【远端输出已截断，共 {output.Length} 个字符】";
        var builder = new StringBuilder();
        builder.AppendLine($"状态：{(succeeded ? "执行成功" : "执行失败")}");
        builder.AppendLine($"退出码：{exitCode?.ToString(CultureInfo.InvariantCulture) ?? "未知"}");
        builder.AppendLine($"远端任务目录：{remoteTaskDirectory}");
        if (!string.IsNullOrWhiteSpace(localOutputDirectory))
        {
            builder.AppendLine($"本地结果目录：{localOutputDirectory}");
        }
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            builder.AppendLine($"错误：{errorMessage}");
        }
        builder.AppendLine("远端输出：");
        builder.Append(limitedOutput);
        return builder.ToString().TrimEnd();
    }
}