using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed record WorkspaceChangeResult
(
    string? PreviousPath,
    string? CurrentPath,
    bool Changed,
    string Message
);

internal sealed class CodingWorkspaceController : INotifyPropertyChanged
{
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly StringComparer _pathComparer;
    private readonly SemaphoreSlim _changeGate = new(1, 1);
    private string _workspaceInput = string.Empty;
    private string? _nextRunWorkspacePath;
    private string _statusText = "工作路径：未设置";
    private bool _isChangingWorkspace;

    public CodingWorkspaceController(IMainThreadDispatcher mainThreadDispatcher)
        : this(mainThreadDispatcher, GetDefaultPathComparer())
    {
    }

    internal CodingWorkspaceController
    (
        IMainThreadDispatcher mainThreadDispatcher,
        StringComparer pathComparer
    )
    {
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);
        ArgumentNullException.ThrowIfNull(pathComparer);
        _mainThreadDispatcher = mainThreadDispatcher;
        _pathComparer = pathComparer;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WorkspaceInput
    {
        get => _workspaceInput;
        set => SetField(ref _workspaceInput, value ?? string.Empty);
    }

    public string? NextRunWorkspacePath => _nextRunWorkspacePath;

    public string StatusText => _statusText;

    public bool IsChangingWorkspace => _isChangingWorkspace;

    public async Task<WorkspaceChangeResult> ChangeWorkspaceAsync
    (
        string? requestedPath,
        CancellationToken cancellationToken = default
    )
    {
        await _changeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await PublishChangingStateAsync(true).ConfigureAwait(false);
            string? normalizedPath = NormalizePath(requestedPath);
            string? previousPath = _nextRunWorkspacePath;
            if (_pathComparer.Equals(previousPath, normalizedPath))
            {
                string noChangeMessage = FormatWorkspaceStatus(normalizedPath);
                await PublishStateAsync(normalizedPath, noChangeMessage).ConfigureAwait(false);
                return new WorkspaceChangeResult(previousPath, normalizedPath, false, noChangeMessage);
            }

            if (normalizedPath is not null && !Directory.Exists(normalizedPath))
            {
                throw new DirectoryNotFoundException($"指定的工作路径不存在：{normalizedPath}");
            }

            string successMessage = FormatWorkspaceStatus(normalizedPath);
            await PublishStateAsync(normalizedPath, successMessage).ConfigureAwait(false);
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

    private static string FormatWorkspaceStatus(string? workspacePath) =>
        workspacePath is null ? "工作路径：未设置" : $"工作路径：{workspacePath}";

    private Task PublishStateAsync(string? workspacePath, string statusText) =>
        _mainThreadDispatcher.InvokeAsync
        (() =>
            {
                SetField(ref _nextRunWorkspacePath, workspacePath, nameof(NextRunWorkspacePath));
                SetField(ref _workspaceInput, workspacePath ?? string.Empty, nameof(WorkspaceInput));
                SetField(ref _statusText, statusText, nameof(StatusText));
                return Task.CompletedTask;
            }
        );

    private Task PublishErrorAsync(string message) =>
        _mainThreadDispatcher.InvokeAsync
        (() =>
            {
                SetField(ref _statusText, $"工作路径设置失败：{message}", nameof(StatusText));
                return Task.CompletedTask;
            }
        );

    private Task PublishChangingStateAsync(bool isChangingWorkspace) =>
        _mainThreadDispatcher.InvokeAsync
        (() =>
            {
                SetField(ref _isChangingWorkspace, isChangingWorkspace, nameof(IsChangingWorkspace));
                return Task.CompletedTask;
            }
        );

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