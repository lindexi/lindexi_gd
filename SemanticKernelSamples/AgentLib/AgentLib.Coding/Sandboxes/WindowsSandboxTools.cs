using System.ComponentModel;
using System.Globalization;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding.Sandboxes;

internal sealed class WindowsSandboxTools
{
    private const int DefaultTimeoutSeconds = 300;
    private const int MaximumTimeoutSeconds = 1800;
    private const string RemoteTasksRoot = @"C:\CodingAgentSandbox\Tasks";

    private readonly string _workspacePath;
    private readonly IWinRemoteShellRunner _runner;

    internal WindowsSandboxTools(string workspacePath, IWinRemoteShellRunner runner)
    {
        ThrowIfNullOrWhiteSpace(workspacePath, nameof(workspacePath));
        ArgumentNullException.ThrowIfNull(runner);
        _workspacePath = Path.GetFullPath(workspacePath);
        _runner = runner;
    }

    internal IReadOnlyList<AITool> AsAITools() =>
        AsToolRegistrations().Select(registration => registration.Tool).ToArray();

    internal IReadOnlyList<ToolRegistration> AsToolRegistrations() =>
    [
        new
        (
            AIFunctionFactory.Create(ExecuteAsync, "execute_in_windows_sandbox"),
            arguments => new ToolCallPresentation
            (
                ToolCallPresentationFactory.GetString(arguments, "executableRelativePath"),
                null
            )
        )
    ];

    [Description("将工作区内的执行器文件夹推送到 Windows 远程沙盒，在隔离任务目录中执行命令，并把指定结果或整个任务目录拉取回工作区。")]
    internal async Task<string> ExecuteAsync
    (
        [Description("要推送到沙盒的本地文件夹。可以传绝对路径；相对路径则相对于代码工作区，且必须位于工作区内。")]
        string sourceDirectory,
        [Description("要执行的文件相对于 sourceDirectory 的路径，例如 bin\\Debug\\net8.0\\TestRunner.exe。")]
        string executableRelativePath,
        [Description("传递给执行文件的命令行参数数组。留空表示不传参数。")]
        IReadOnlyList<string>? arguments = null,
        [Description("要从沙盒拉取的文件或文件夹相对路径。留空表示拉取整个远端任务目录。")]
        string? outputRelativePath = null,
        [Description("结果在本地工作区内的保存目录。留空时保存到 .coding-agent\\sandbox-results\\<任务编号>。")]
        string? localOutputDirectory = null,
        [Description("远端执行超时秒数，默认 300 秒，最大 1800 秒。")]
        int timeoutSeconds = DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await ExecuteCoreAsync
            (
                sourceDirectory,
                executableRelativePath,
                arguments,
                outputRelativePath,
                localOutputDirectory,
                timeoutSeconds,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return FormatFailure(exception);
        }
    }

    private async Task<string> ExecuteCoreAsync
    (
        string sourceDirectory,
        string executableRelativePath,
        IReadOnlyList<string>? arguments,
        string? outputRelativePath,
        string? localOutputDirectory,
        int timeoutSeconds,
        CancellationToken cancellationToken
    )
    {
        string fullSourceDirectory = ResolveSourceDirectory(sourceDirectory);
        string executablePath = NormalizeRelativePath(executableRelativePath, nameof(executableRelativePath));
        string? outputPath = string.IsNullOrWhiteSpace(outputRelativePath)
            ? null
            : NormalizeRelativePath(outputRelativePath, nameof(outputRelativePath));
        if (timeoutSeconds is < 1 or > MaximumTimeoutSeconds)
        {
            throw new ArgumentOutOfRangeException
            (
                nameof(timeoutSeconds),
                $"超时秒数必须在 1 到 {MaximumTimeoutSeconds} 之间。"
            );
        }

        string taskId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        string remoteTaskDirectory = $@"{RemoteTasksRoot}\{taskId}";
        string remoteExecutablePath = CombineRemotePath(remoteTaskDirectory, executablePath);
        string fullLocalOutputPath = ResolveLocalOutputPath(localOutputDirectory, taskId);

        await _runner.PushAsync(fullSourceDirectory, remoteTaskDirectory, cancellationToken).ConfigureAwait(false);

        string executionOutput = await _runner.ExecuteAsync
        (
            remoteExecutablePath,
            arguments ?? [],
            timeoutSeconds,
            cancellationToken
        ).ConfigureAwait(false);

        Directory.CreateDirectory(fullLocalOutputPath);
        await _runner.PullAsync(remoteTaskDirectory, fullLocalOutputPath, cancellationToken).ConfigureAwait(false);

        string resultPath = outputPath is null
            ? fullLocalOutputPath
            : Path.Combine(fullLocalOutputPath, outputPath.Replace('\\', Path.DirectorySeparatorChar));
        if (outputPath is not null && !File.Exists(resultPath) && !Directory.Exists(resultPath))
        {
            throw new FileNotFoundException($"沙箱执行完成，但拉取的任务目录中不存在指定结果：{outputPath}", resultPath);
        }

        return string.IsNullOrWhiteSpace(executionOutput)
            ? $"沙箱执行完成。结果已保存到：{resultPath}"
            : $"{executionOutput.Trim()}{Environment.NewLine}结果已保存到：{resultPath}";
    }

    private static string FormatFailure(Exception exception)
    {
        IEnumerable<Exception> exceptions = exception is AggregateException aggregateException
            ? aggregateException.Flatten().InnerExceptions.Prepend(exception)
            : EnumerateExceptionChain(exception);
        string details = string.Join
        (
            $"{Environment.NewLine}由以下错误导致：",
            exceptions.Select(current => $"{current.GetType().Name}: {current.Message}")
        );
        return $"沙箱执行失败。{Environment.NewLine}错误详情：{details}";
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    private string ResolveSourceDirectory(string sourceDirectory)
    {
        ThrowIfNullOrWhiteSpace(sourceDirectory, nameof(sourceDirectory));
        string fullPath = Path.GetFullPath(sourceDirectory, _workspacePath);
        EnsureInsideWorkspace(fullPath, nameof(sourceDirectory));
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"未找到要推送的目录：{fullPath}");
        }

        return fullPath;
    }

    private string ResolveLocalOutputPath(string? localOutputDirectory, string taskId)
    {
        string path = string.IsNullOrWhiteSpace(localOutputDirectory)
            ? Path.Combine(_workspacePath, ".coding-agent", "sandbox-results", taskId)
            : Path.GetFullPath(localOutputDirectory, _workspacePath);
        EnsureInsideWorkspace(path, nameof(localOutputDirectory));
        return path;
    }

    private void EnsureInsideWorkspace(string path, string parameterName)
    {
        string relativePath = Path.GetRelativePath(_workspacePath, path);
        if (relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("路径必须位于代码工作区内。", parameterName);
        }
    }

    private static string NormalizeRelativePath(string path, string parameterName)
    {
        ThrowIfNullOrWhiteSpace(path, parameterName);
        string normalized = path.Replace('/', '\\').Trim();
        if (Path.IsPathRooted(normalized)
            || normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            throw new ArgumentException("必须提供不包含上级目录的相对路径。", parameterName);
        }

        return normalized.TrimStart('.').TrimStart('\\');
    }

    private static string CombineRemotePath(string root, string relativePath) =>
        string.IsNullOrEmpty(relativePath) ? root : $@"{root}\{relativePath}";

    private static void ThrowIfNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}