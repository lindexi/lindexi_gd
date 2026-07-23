using SimpleWrite.Models;

using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleWrite.Business.FileHandlers;

internal sealed class ActiveDocumentFileChangeService : IDisposable
{
    public ActiveDocumentFileChangeService(Func<EditorModel, Task> handleExternalFileChangeAsync)
    {
        ArgumentNullException.ThrowIfNull(handleExternalFileChangeAsync);

        _handleExternalFileChangeAsync = handleExternalFileChangeAsync;
        _activeFileChangeMonitor.FileChanged += ActiveFileChangeMonitorOnFileChanged;
    }

    public async Task ActivateAsync(EditorModel editorModel, bool checkDiskState)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        Deactivate();
        _activeEditorModel = editorModel;
        _isSuppressed = false;

        if (checkDiskState && HasFileChangedOnDisk(editorModel))
        {
            await _handleExternalFileChangeAsync(editorModel).ConfigureAwait(true);
        }

        if (!ReferenceEquals(editorModel, _activeEditorModel)
            || _isSuppressed
            || editorModel.FileInfo is not { } fileInfo)
        {
            return;
        }

        _activeFileChangeMonitor.Start(fileInfo);
    }

    public void Deactivate()
    {
        _activeEditorModel = null;
        _isSuppressed = false;
        _activeFileChangeMonitor.Stop();
    }

    public void MarkSynchronized(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (editorModel.FileInfo is { } fileInfo
            && FileDiskState.TryRead(fileInfo, out var fileDiskState))
        {
            editorModel.LoadedFileDiskState = fileDiskState;
        }
    }

    public IDisposable BeginLocalSave(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        _localSavingEditorModel = editorModel;
        return new LocalSaveScope(this, editorModel);
    }

    public void Suppress(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (!ReferenceEquals(editorModel, _activeEditorModel))
        {
            return;
        }

        _isSuppressed = true;
        _activeFileChangeMonitor.Stop();
    }

    public void RefreshMonitoring(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (!ReferenceEquals(editorModel, _activeEditorModel)
            || _isSuppressed
            || editorModel.FileInfo is not { } fileInfo)
        {
            return;
        }

        _activeFileChangeMonitor.Start(fileInfo);
    }

    public void Dispose()
    {
        _activeFileChangeMonitor.FileChanged -= ActiveFileChangeMonitorOnFileChanged;
        _activeFileChangeMonitor.Dispose();
    }

    private void ActiveFileChangeMonitorOnFileChanged(object? sender, ActiveFileChangedEventArgs e)
    {
        var editorModel = _activeEditorModel;
        if (editorModel is null
            || _isSuppressed
            || _isHandlingChange
            || ReferenceEquals(editorModel, _localSavingEditorModel)
            || editorModel.FileInfo is not { } fileInfo
            || !PathEquals(fileInfo.FullName, e.FilePath))
        {
            return;
        }

        _ = HandleActiveFileChangeAsync(editorModel, e.FilePath);
    }

    private async Task HandleActiveFileChangeAsync(EditorModel editorModel, string changedFilePath)
    {
        if (_isHandlingChange)
        {
            return;
        }

        _isHandlingChange = true;
        try
        {
            await Task.Yield();

            if (!ReferenceEquals(editorModel, _activeEditorModel)
                || _isSuppressed
                || ReferenceEquals(editorModel, _localSavingEditorModel)
                || editorModel.FileInfo is not { } fileInfo
                || !HasFileChangedOnDisk(editorModel)
                || !PathEquals(fileInfo.FullName, changedFilePath))
            {
                return;
            }

            await _handleExternalFileChangeAsync(editorModel).ConfigureAwait(true);
        }
        finally
        {
            _isHandlingChange = false;
        }
    }

    private static bool HasFileChangedOnDisk(EditorModel editorModel)
    {
        return editorModel.FileInfo is { } fileInfo
               && editorModel.LoadedFileDiskState is { } loadedFileDiskState
               && FileDiskState.TryRead(fileInfo, out var currentFileDiskState)
               && currentFileDiskState != loadedFileDiskState;
    }

    private static bool PathEquals(string firstPath, string secondPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(firstPath), Path.GetFullPath(secondPath), comparison);
    }

    private void EndLocalSave(EditorModel editorModel)
    {
        if (ReferenceEquals(editorModel, _localSavingEditorModel))
        {
            _localSavingEditorModel = null;
        }
    }

    private readonly Func<EditorModel, Task> _handleExternalFileChangeAsync;
    private readonly ActiveFileChangeMonitor _activeFileChangeMonitor = new();
    private EditorModel? _activeEditorModel;
    private EditorModel? _localSavingEditorModel;
    private bool _isSuppressed;
    private bool _isHandlingChange;

    private sealed class LocalSaveScope(ActiveDocumentFileChangeService service, EditorModel editorModel) : IDisposable
    {
        public void Dispose()
        {
            service.EndLocalSave(editorModel);
        }
    }
}
