using Avalonia.Threading;

using SimpleWrite.Foundation;
using SimpleWrite.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleWrite.Business.TempFiles;

internal sealed class TempDocumentAutoSaveService
{
    private static TimeSpan AutoSaveDelay => TimeSpan.FromSeconds(3);
    private const int MaxVersionCount = 10;

    private readonly AppPathManager _appPathManager;
    private readonly Dictionary<Guid, DocumentAutoSaveEntry> _entryMap = [];

    public TempDocumentAutoSaveService(AppPathManager appPathManager)
    {
        _appPathManager = appPathManager;
    }

    public void ScheduleAutoSave(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (editorModel.TextEditor is null)
        {
            return;
        }

        _ = QueueSaveAsync(editorModel, forceImmediateSave: false);
    }

    public async Task FlushAsync(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (editorModel.TextEditor is null)
        {
            return;
        }

        await QueueSaveAsync(editorModel, forceImmediateSave: true).ConfigureAwait(false);
    }

    public void Release(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        _entryMap.Remove(editorModel.DocumentId);

        if (editorModel.FileInfo is null)
        {
            return;
        }

        var documentDirectoryPath = GetDocumentDirectoryPath(editorModel);
        if (Directory.Exists(documentDirectoryPath))
        {
            Directory.Delete(documentDirectoryPath, recursive: true);
        }
    }

    private Task QueueSaveAsync(EditorModel editorModel, bool forceImmediateSave)
    {
        var entry = GetOrCreateEntry(editorModel);
        Task processingTask;

        lock (entry.SyncRoot)
        {
            entry.HasPendingChange = true;
            entry.LastChangedAtUtc = forceImmediateSave ? DateTime.UtcNow - AutoSaveDelay : DateTime.UtcNow;
            entry.PendingSignal.TrySetResult(true);

            if (!entry.IsProcessing)
            {
                entry.IsProcessing = true;
                entry.ProcessingTask = Task.Run(() => RunAutoSaveLoopAsync(editorModel, entry));
            }

            processingTask = entry.ProcessingTask;
        }

        return processingTask;
    }

    private async Task RunAutoSaveLoopAsync(EditorModel editorModel, DocumentAutoSaveEntry entry)
    {
        try
        {
            while (true)
            {
                Task? waitTask = null;

                lock (entry.SyncRoot)
                {
                    if (!entry.HasPendingChange)
                    {
                        return;
                    }

                    var dueTime = entry.LastChangedAtUtc + AutoSaveDelay;
                    var delay = dueTime - DateTime.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        waitTask = Task.WhenAny(Task.Delay(delay), entry.PendingSignal.Task);
                    }
                    else
                    {
                        entry.HasPendingChange = false;
                    }
                }

                if (waitTask is not null)
                {
                    await waitTask.ConfigureAwait(false);

                    lock (entry.SyncRoot)
                    {
                        if (entry.PendingSignal.Task.IsCompleted)
                        {
                            entry.PendingSignal = CreatePendingSignal();
                        }
                    }

                    continue;
                }

                await SaveSnapshotAsync(editorModel).ConfigureAwait(false);
            }
        }
        finally
        {
            lock (entry.SyncRoot)
            {
                entry.IsProcessing = false;
                entry.ProcessingTask = Task.CompletedTask;
            }
        }
    }

    private async Task SaveSnapshotAsync(EditorModel editorModel)
    {
        var snapshot = await CreateSnapshotAsync(editorModel).ConfigureAwait(false);
        if (snapshot is null)
        {
            return;
        }

        var documentDirectory = Directory.CreateDirectory(GetDocumentDirectoryPath(editorModel));
        var snapshotFilePath = Path.Join(documentDirectory.FullName, BuildSnapshotFileName(snapshot));
        await File.WriteAllTextAsync(snapshotFilePath, snapshot.Text).ConfigureAwait(false);

        TrimSnapshotFiles(documentDirectory);
    }

    private static async Task<SnapshotContent?> CreateSnapshotAsync(EditorModel editorModel)
    {
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (editorModel.TextEditor is null || editorModel.IsEmptyText())
            {
                return null;
            }

            var extension = editorModel.FileInfo?.Extension;
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".txt";
            }

            var baseName = editorModel.FileInfo is { } fileInfo
                ? Path.GetFileNameWithoutExtension(fileInfo.Name)
                : editorModel.Title;
            baseName = SanitizeFileName(baseName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Document";
            }

            return new SnapshotContent(editorModel.TextEditor.Text, baseName, extension);
        });
    }

    private static string BuildSnapshotFileName(SnapshotContent snapshot)
    {
        return $"{DateTime.Now:yyyyMMdd_HHmmss_fff}__{snapshot.BaseName}{snapshot.Extension}";
    }

    private static void TrimSnapshotFiles(DirectoryInfo documentDirectory)
    {
        var snapshotFileList = documentDirectory.EnumerateFiles()
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var snapshotFile in snapshotFileList.Take(Math.Max(0, snapshotFileList.Count - MaxVersionCount)))
        {
            snapshotFile.Delete();
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidFileNameCharSet = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalidFileNameCharSet.Contains(c) ? '_' : c).ToArray());
    }

    private static TaskCompletionSource<bool> CreatePendingSignal()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private string GetDocumentDirectoryPath(EditorModel editorModel)
    {
        return Path.Join(_appPathManager.TempDirectory, editorModel.DocumentId.ToString("N"));
    }

    private DocumentAutoSaveEntry GetOrCreateEntry(EditorModel editorModel)
    {
        if (_entryMap.TryGetValue(editorModel.DocumentId, out var entry))
        {
            return entry;
        }

        entry = new DocumentAutoSaveEntry();
        _entryMap[editorModel.DocumentId] = entry;
        return entry;
    }

    private sealed class DocumentAutoSaveEntry
    {
        public object SyncRoot { get; } = new();

        public bool HasPendingChange { get; set; }

        public bool IsProcessing { get; set; }

        public DateTime LastChangedAtUtc { get; set; }

        public TaskCompletionSource<bool> PendingSignal { get; set; } = CreatePendingSignal();

        public Task ProcessingTask { get; set; } = Task.CompletedTask;
    }

    private sealed record SnapshotContent(string Text, string BaseName, string Extension);
}
