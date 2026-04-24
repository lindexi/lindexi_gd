using JalfijefallKelweehelhelwellu;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SnapkeboyearheNarjairfiru;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const int MaxVisibleItems = 100;
    private const int HistoryPageSize = 20;
    private const int RecentContextCount = 10;
    private const long StorageLimitBytes = 1024L * 1024L * 1024L;
    private static readonly TimeSpan MinimumCaptureInterval = TimeSpan.FromSeconds(10);
    private static readonly SearchOption FileSearchOption = SearchOption.TopDirectoryOnly;
    private static readonly Uri OllamaEndpoint = new("http://172.20.113.28:11434");
    private const string ModelId = "qwen3-vl:8b";
    private static readonly HashSet<string> SnapshotImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".gif",
        ".bmp"
    };

    private readonly ObservableCollection<SnapshotListItem> _items = [];
    private readonly List<SnapshotRecord> _historyRecords = [];
    private readonly object _historySyncRoot = new();
    private readonly SemaphoreSlim _historyLoadLock = new(1, 1);
    private readonly ScreenSnapshotProvider _screenSnapshotProvider = new();
    private readonly ScreenshotAnalysisService _analysisService = new(OllamaEndpoint, ModelId);
    private CancellationTokenSource? _captureLoopCancellationTokenSource;
    private int _nextHistoryIndex;
    private bool _autoTrimEnabled = true;
    private bool _canLoadMoreHistory;
    private bool _isCapturePaused;
    private string _statusText = "正在初始化。";

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;
        StorageFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SnapkeboyearheNarjairfiru",
            "Snapshots");

        Directory.CreateDirectory(StorageFolderPath);

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SnapshotListItem> Items => _items;

    public string StorageFolderPath { get; }

    public bool AutoTrimEnabled
    {
        get => _autoTrimEnabled;
        set
        {
            if (!SetProperty(ref _autoTrimEnabled, value))
            {
                return;
            }

            if (_autoTrimEnabled)
            {
                TrimVisibleItems();
            }
        }
    }

    public bool CanLoadMoreHistory
    {
        get => _canLoadMoreHistory;
        private set => SetProperty(ref _canLoadMoreHistory, value);
    }

    public bool IsCapturePaused
    {
        get => _isCapturePaused;
        private set => SetProperty(ref _isCapturePaused, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadHistoryIndexAsync();
            await LoadMoreHistoryAsync();
            StartCaptureLoop();
        }
        catch (Exception exception)
        {
            StatusText = $"启动失败：{exception.Message}";
            System.Windows.MessageBox.Show(this, exception.Message, "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _captureLoopCancellationTokenSource?.Cancel();
        _captureLoopCancellationTokenSource?.Dispose();
        _historyLoadLock.Dispose();
        _screenSnapshotProvider.Dispose();
    }

    private void StartCaptureLoop()
    {
        _captureLoopCancellationTokenSource?.Cancel();
        _captureLoopCancellationTokenSource?.Dispose();

        _captureLoopCancellationTokenSource = new CancellationTokenSource();
        _ = RunCaptureLoopSafelyAsync(_captureLoopCancellationTokenSource.Token);
    }

    private async Task RunCaptureLoopSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UpdateStatusAsync("截图和解读循环已启动。");
            await RunCaptureLoopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await UpdateStatusAsync("截图和解读循环已停止。");
        }
        catch (Exception exception)
        {
            await UpdateStatusAsync($"后台循环失败：{exception.Message}");
        }
    }

    private async Task RunCaptureLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await WaitWhilePausedAsync(cancellationToken);

            try
            {
                await CaptureAsync(cancellationToken);
            }
            catch (Exception e)
            {
                await UpdateStatusAsync("截图和解读异常。" + e.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task CaptureAsync(CancellationToken cancellationToken)
    {
        var displays = _screenSnapshotProvider.GetDisplays();
        if (displays.Count == 0)
        {
            await UpdateStatusAsync("未检测到可用屏幕，5 秒后重试。");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return;
        }

        foreach (var display in displays)
        {
            await WaitWhilePausedAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();
            await UpdateStatusAsync($"正在截图并解读 {display.Name}。");
            await CaptureAndAnalyzeDisplayAsync(display, cancellationToken);

            var remainingDelay = MinimumCaptureInterval - stopwatch.Elapsed;
            if (remainingDelay > TimeSpan.Zero)
            {
                await UpdateStatusAsync($"{display.Name} 已完成，等待 {remainingDelay.TotalSeconds:F0} 秒后继续。");
                await Task.Delay(remainingDelay, cancellationToken);
            }
        }
    }

    private async Task CaptureAndAnalyzeDisplayAsync(ScreenSnapshotDisplay display, CancellationToken cancellationToken)
    {
        var capturedAt = DateTimeOffset.Now;
        var fileBaseName = SnapshotRecord.CreateBaseFileName(capturedAt, display.Key);
        var imagePath = Path.Combine(StorageFolderPath, $"{fileBaseName}.png");
        var analysisPath = Path.Combine(StorageFolderPath, $"{fileBaseName}.xml");

        await _screenSnapshotProvider.TakeSnapshotAsync(display, new FileInfo(imagePath), cancellationToken);

        string analysisText;
        var recentContexts = GetRecentAnalysisContexts();

        try
        {
            analysisText = await _analysisService.AnalyzeAsync(imagePath, capturedAt, recentContexts, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            analysisText = $"解读失败：{exception.Message}";
        }

        var record = new SnapshotRecord(
            fileBaseName,
            display.Key,
            display.Name,
            capturedAt,
            imagePath,
            analysisPath,
            analysisText);

        await File.WriteAllTextAsync(
            analysisPath,
            record.CreateXmlContent(),
            cancellationToken);

        var listItem = await Task.Run(record.ToListItem, cancellationToken);
        var cleanupResult = await CleanupStorageIfNeededAsync(cancellationToken);
        await Dispatcher.InvokeAsync(() => AddLiveItem(record, listItem, cleanupResult));
    }

    private async Task LoadHistoryIndexAsync()
    {
        var records = await Task.Run(() =>
            Directory.EnumerateFiles(StorageFolderPath, "*.*", FileSearchOption)
                .Where(path => IsAnalysisFile(path))
                .Select(path => SnapshotRecord.TryLoad(path, out var record) ? record : null)
                .OfType<SnapshotRecord>()
                .GroupBy(record => record.Key, StringComparer.Ordinal)
                .Select(group => group.OrderBy(record => GetAnalysisFilePriority(record.AnalysisPath)).First())
                .OrderByDescending(record => record.CapturedAt)
                .ToList());

        lock (_historySyncRoot)
        {
            _historyRecords.Clear();
            _historyRecords.AddRange(records);
            _nextHistoryIndex = 0;
            CanLoadMoreHistory = _historyRecords.Count > 0;
        }

        await UpdateStatusAsync(records.Count == 0
            ? "当前没有历史记录，等待新的截图。"
            : $"已索引 {records.Count} 条历史记录。");
    }

    private async Task LoadMoreHistoryAsync()
    {
        await _historyLoadLock.WaitAsync();

        try
        {
            var canLoadMoreHistory = false;
            List<SnapshotListItem> batch = await Task.Run(() =>
            {
                List<SnapshotRecord> recordsToLoad = [];

                lock (_historySyncRoot)
                {
                    var remaining = _historyRecords.Count - _nextHistoryIndex;
                    var count = Math.Min(HistoryPageSize, Math.Max(remaining, 0));

                    for (var i = 0; i < count; i++)
                    {
                        recordsToLoad.Add(_historyRecords[_nextHistoryIndex + i]);
                    }

                    _nextHistoryIndex += count;
                    canLoadMoreHistory = _nextHistoryIndex < _historyRecords.Count;
                }

                return recordsToLoad.Select(record => record.ToListItem()).ToList();
            });

            CanLoadMoreHistory = canLoadMoreHistory;

            foreach (var item in batch)
            {
                if (_items.Any(existingItem => existingItem.Key == item.Key))
                {
                    continue;
                }

                _items.Add(item);
            }

            if (AutoTrimEnabled)
            {
                TrimVisibleItems();
            }

            if (batch.Count > 0)
            {
                StatusText = $"已加载 {batch.Count} 条历史记录。当前显示 {_items.Count} 项。";
            }
        }
        finally
        {
            _historyLoadLock.Release();
        }
    }

    private void AddLiveItem(SnapshotRecord record, SnapshotListItem listItem, StorageCleanupResult cleanupResult)
    {
        lock (_historySyncRoot)
        {
            _historyRecords.Insert(0, record);

            if (_nextHistoryIndex > 0)
            {
                _nextHistoryIndex++;
            }

            CanLoadMoreHistory = _nextHistoryIndex < _historyRecords.Count;
        }

        var existingIndex = -1;

        for (var i = 0; i < _items.Count; i++)
        {
            if (_items[i].Key == listItem.Key)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0 && existingIndex < _items.Count)
        {
            _items.RemoveAt(existingIndex);
        }

        _items.Insert(0, listItem);

        if (AutoTrimEnabled)
        {
            TrimVisibleItems();
        }

        StatusText = cleanupResult.DeletedFileCount == 0
            ? $"已完成 {record.DisplayName} 的截图与解读。当前显示 {_items.Count} 项。"
            : $"已完成 {record.DisplayName} 的截图与解读。当前显示 {_items.Count} 项，并清理了 {cleanupResult.DeletedFileCount} 张旧截图，释放 {FormatFileSize(cleanupResult.ReleasedBytes)}。";
    }

    private void TrimVisibleItems()
    {
        while (_items.Count > MaxVisibleItems)
        {
            _items.RemoveAt(_items.Count - 1);
        }
    }

    private List<SnapshotAnalysisContext> GetRecentAnalysisContexts()
    {
        lock (_historySyncRoot)
        {
            return _historyRecords
                .Where(record => !string.IsNullOrWhiteSpace(record.AnalysisText))
                .Where(record => !record.AnalysisText.StartsWith("解读失败：", StringComparison.Ordinal))
                .Take(RecentContextCount)
                .Select(record => record.ToAnalysisContext())
                .ToList();
        }
    }

    private async Task<StorageCleanupResult> CleanupStorageIfNeededAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            DirectoryInfo directoryInfo = new(StorageFolderPath);
            if (!directoryInfo.Exists)
            {
                return StorageCleanupResult.Empty;
            }

            var files = directoryInfo.EnumerateFiles("*", FileSearchOption).ToList();
            var totalSize = files.Sum(file => file.Length);
            if (totalSize <= StorageLimitBytes)
            {
                return StorageCleanupResult.Empty;
            }

            var filesToDelete = files
                .Where(file => SnapshotImageExtensions.Contains(file.Extension))
                .OrderBy(file => file.CreationTimeUtc)
                .ThenBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var deletedFileCount = 0;
            long releasedBytes = 0;

            foreach (var file in filesToDelete)
            {
                if (totalSize <= StorageLimitBytes)
                {
                    break;
                }

                var fileLength = file.Length;
                file.Delete();
                totalSize -= fileLength;
                releasedBytes += fileLength;
                deletedFileCount++;
            }

            return new StorageCleanupResult(deletedFileCount, releasedBytes);
        }, cancellationToken);
    }

    private async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
    {
        while (IsCapturePaused)
        {
            await UpdateStatusAsync("截图和解读已暂停。");
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }
    }

    private async Task UpdateStatusAsync(string status)
    {
        await Dispatcher.InvokeAsync(() => StatusText = status);
    }

    private void PauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsCapturePaused = true;
        StatusText = "截图和解读已暂停。";
    }

    private void ResumeButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsCapturePaused = false;
        StatusText = "截图和解读已恢复。";
    }

    private async void LoadHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        await LoadMoreHistoryAsync();
    }

    private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = StorageFolderPath,
            UseShellExecute = true
        });
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private static int GetAnalysisFilePriority(string analysisPath)
    {
        return string.Equals(Path.GetExtension(analysisPath), ".xml", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static bool IsAnalysisFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".xml" or ".txt";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
        {
            return $"{bytes / 1024d / 1024d / 1024d:F2} GB";
        }

        if (bytes >= 1024L * 1024L)
        {
            return $"{bytes / 1024d / 1024d:F2} MB";
        }

        if (bytes >= 1024L)
        {
            return $"{bytes / 1024d:F2} KB";
        }

        return $"{bytes} B";
    }

    private sealed record StorageCleanupResult(int DeletedFileCount, long ReleasedBytes)
    {
        public static StorageCleanupResult Empty { get; } = new(0, 0);
    }
}