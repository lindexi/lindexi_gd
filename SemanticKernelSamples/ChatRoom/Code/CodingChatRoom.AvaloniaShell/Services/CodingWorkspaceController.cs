using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using AgentLib;
using AgentLib.Coding;

namespace CodingChatRoom.AvaloniaShell.Services;

internal interface ICodingWorkspaceRuntime
{
    Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken);
}

internal sealed class CodingAgentWorkspaceRuntime : ICodingWorkspaceRuntime
{
    private readonly CodingAgent _codingAgent;

    public CodingAgentWorkspaceRuntime(CodingAgent codingAgent)
    {
        ArgumentNullException.ThrowIfNull(codingAgent);
        _codingAgent = codingAgent;
    }

    public Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken) =>
        _codingAgent.PrepareWorkspaceChangeAsync(workspacePath, cancellationToken);
}

internal sealed record WorkspaceChangeResult(
    string? PreviousPath,
    string? CurrentPath,
    bool Changed,
    string Message);

internal sealed class CodingWorkspaceController : INotifyPropertyChanged
{
    private readonly ICodingWorkspaceRuntime _workspaceRuntime;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly StringComparer _pathComparer;
    private readonly SemaphoreSlim _changeGate = new(1, 1);
    private string _workspaceInput = string.Empty;
    private string? _committedWorkspacePath;
    private string _statusText = "尚未设置工作路径";
    private bool _isChangingWorkspace;

    public CodingWorkspaceController(
        ICodingWorkspaceRuntime workspaceRuntime,
        IMainThreadDispatcher mainThreadDispatcher)
        : this(workspaceRuntime, mainThreadDispatcher, GetDefaultPathComparer())
    {
    }

    internal CodingWorkspaceController(
        ICodingWorkspaceRuntime workspaceRuntime,
        IMainThreadDispatcher mainThreadDispatcher,
        StringComparer pathComparer)
    {
        ArgumentNullException.ThrowIfNull(workspaceRuntime);
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);
        ArgumentNullException.ThrowIfNull(pathComparer);
        _workspaceRuntime = workspaceRuntime;
        _mainThreadDispatcher = mainThreadDispatcher;
        _pathComparer = pathComparer;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WorkspaceInput
    {
        get => _workspaceInput;
        set => SetField(ref _workspaceInput, value ?? string.Empty);
    }

    public string? CommittedWorkspacePath => _committedWorkspacePath;

    public string StatusText => _statusText;

    public bool IsChangingWorkspace => _isChangingWorkspace;

    public async Task<WorkspaceChangeResult> ChangeWorkspaceAsync(
        string? requestedPath,
        CancellationToken cancellationToken = default)
    {
        await _changeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await PublishChangingStateAsync(true).ConfigureAwait(false);
            string? normalizedPath = NormalizePath(requestedPath);
            string? previousPath = _committedWorkspacePath;
            if (_pathComparer.Equals(previousPath, normalizedPath))
            {
                string noChangeMessage = normalizedPath is null
                    ? "工作路径已清除"
                    : $"工作路径未变化：{normalizedPath}";
                await PublishStateAsync(normalizedPath, noChangeMessage).ConfigureAwait(false);
                return new WorkspaceChangeResult(previousPath, normalizedPath, false, noChangeMessage);
            }

            if (normalizedPath is not null && !Directory.Exists(normalizedPath))
            {
                throw new DirectoryNotFoundException($"指定的工作路径不存在：{normalizedPath}");
            }

            await using IWorkspaceChangeTransaction transaction = await _workspaceRuntime
                .PrepareWorkspaceChangeAsync(normalizedPath, cancellationToken)
                .ConfigureAwait(false);
            transaction.Apply();
            string successMessage = normalizedPath is null
                ? "工作路径已清除"
                : $"工作路径已设置为：{normalizedPath}";
            try
            {
                await PublishStateAsync(normalizedPath, successMessage).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                await PublishStateAsync(previousPath, "工作路径状态发布失败，已保留原工作路径").ConfigureAwait(false);
                throw;
            }

            transaction.CommitAfterPublish();
            return new WorkspaceChangeResult(previousPath, normalizedPath, true, successMessage);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await PublishErrorAsync(exception.Message).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await PublishChangingStateAsync(false).ConfigureAwait(false);
            _changeGate.Release();
        }
    }

    private static StringComparer GetDefaultPathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static string? NormalizePath(string? requestedPath) =>
        string.IsNullOrWhiteSpace(requestedPath) ? null : Path.GetFullPath(requestedPath.Trim());

    private Task PublishStateAsync(string? workspacePath, string statusText) =>
        _mainThreadDispatcher.InvokeAsync(() =>
        {
            SetField(ref _committedWorkspacePath, workspacePath, nameof(CommittedWorkspacePath));
            SetField(ref _workspaceInput, workspacePath ?? string.Empty, nameof(WorkspaceInput));
            SetField(ref _statusText, statusText, nameof(StatusText));
            return Task.CompletedTask;
        });

    private Task PublishErrorAsync(string message) =>
        _mainThreadDispatcher.InvokeAsync(() =>
        {
            SetField(ref _statusText, $"工作路径应用失败：{message}", nameof(StatusText));
            return Task.CompletedTask;
        });

    private Task PublishChangingStateAsync(bool isChangingWorkspace) =>
        _mainThreadDispatcher.InvokeAsync(() =>
        {
            SetField(ref _isChangingWorkspace, isChangingWorkspace, nameof(IsChangingWorkspace));
            return Task.CompletedTask;
        });

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
