using SimpleWrite.Foundation;
using SimpleWrite.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        var entry = GetOrCreateEntry(editorModel);
        entry.DebounceCancellationTokenSource?.Cancel();
        entry.DebounceCancellationTokenSource?.Dispose();

        var cancellationTokenSource = new CancellationTokenSource();
        entry.DebounceCancellationTokenSource = cancellationTokenSource;

        _ = RunAutoSaveAsync(editorModel, entry, cancellationTokenSource.Token);
    }

    public async Task FlushAsync(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (editorModel.TextEditor is null || editorModel.IsEmptyText())
        {
            return;
        }

        var entry = GetOrCreateEntry(editorModel);
        entry.DebounceCancellationTokenSource?.Cancel();
        entry.DebounceCancellationTokenSource?.Dispose();
        entry.DebounceCancellationTokenSource = null;

        await SaveSnapshotAsync(editorModel, entry, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task RunAutoSaveAsync(EditorModel editorModel, DocumentAutoSaveEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutoSaveDelay, cancellationToken).ConfigureAwait(false);
            await SaveSnapshotAsync(editorModel, entry, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveSnapshotAsync(EditorModel editorModel, DocumentAutoSaveEntry entry, CancellationToken cancellationToken)
    {
        await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (editorModel.TextEditor is null || editorModel.IsEmptyText())
            {
                return;
            }

            var text = editorModel.TextEditor.Text;
            var contentHash = StringComparer.Ordinal.GetHashCode(text);
            var contentLength = text.Length;
            if (entry.LastSavedContentHash == contentHash && entry.LastSavedContentLength == contentLength)
            {
                return;
            }

            var documentDirectory = Directory.CreateDirectory(Path.Join(_appPathManager.TempDirectory, editorModel.DocumentId.ToString("N")));
            var snapshotFilePath = Path.Join(documentDirectory.FullName, BuildSnapshotFileName(editorModel));
            await File.WriteAllTextAsync(snapshotFilePath, text, cancellationToken).ConfigureAwait(false);

            entry.LastSavedContentHash = contentHash;
            entry.LastSavedContentLength = contentLength;

            TrimSnapshotFiles(documentDirectory);
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private static string BuildSnapshotFileName(EditorModel editorModel)
    {
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

        return $"{DateTime.Now:yyyyMMdd_HHmmss_fff}__{baseName}{extension}";
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
        public CancellationTokenSource? DebounceCancellationTokenSource { get; set; }

        public SemaphoreSlim Gate { get; } = new(1, 1);

        public int LastSavedContentHash { get; set; }

        public int LastSavedContentLength { get; set; } = -1;
    }
}
