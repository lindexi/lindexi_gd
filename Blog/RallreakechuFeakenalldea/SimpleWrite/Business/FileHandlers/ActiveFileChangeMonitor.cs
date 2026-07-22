using System;
using System.IO;

using Avalonia.Threading;

namespace SimpleWrite.Business.FileHandlers;

internal sealed class ActiveFileChangeMonitor : IDisposable
{
    public event EventHandler<ActiveFileChangedEventArgs>? FileChanged;

    public void Start(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        Stop();

        var filePath = Path.GetFullPath(fileInfo.FullName);
        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException($"无法确定文件“{filePath}”所在的目录。");
        }

        var watcher = new FileSystemWatcher(directoryPath, Path.GetFileName(filePath))
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.CreationTime
                | NotifyFilters.LastWrite
                | NotifyFilters.Size,
        };

        watcher.Changed += (_, e) => NotifyIfTargetFile(filePath, e.FullPath);
        watcher.Created += (_, e) => NotifyIfTargetFile(filePath, e.FullPath);
        watcher.Deleted += (_, e) => NotifyIfTargetFile(filePath, e.FullPath);
        watcher.Renamed += (_, e) => NotifyIfTargetFile(filePath, e.FullPath, e.OldFullPath);
        watcher.Error += (_, _) => NotifyChanged(filePath);

        _watcher = watcher;
        watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        var watcher = _watcher;
        _watcher = null;
        if (watcher is null)
        {
            return;
        }

        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
    }

    public void Dispose()
    {
        Stop();
    }

    private void NotifyIfTargetFile(string targetFilePath, params string[] changedFilePaths)
    {
        foreach (var changedFilePath in changedFilePaths)
        {
            if (PathEquals(targetFilePath, changedFilePath))
            {
                NotifyChanged(targetFilePath);
                return;
            }
        }
    }

    private void NotifyChanged(string filePath)
    {
        Dispatcher.UIThread.Post(() => FileChanged?.Invoke(this, new ActiveFileChangedEventArgs(filePath)));
    }

    private static bool PathEquals(string firstPath, string secondPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(firstPath), Path.GetFullPath(secondPath), comparison);
    }

    private FileSystemWatcher? _watcher;
}

internal sealed class ActiveFileChangedEventArgs(string filePath) : EventArgs
{
    public string FilePath { get; } = filePath;
}
