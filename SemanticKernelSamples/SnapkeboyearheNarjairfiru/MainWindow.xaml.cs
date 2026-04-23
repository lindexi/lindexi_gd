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
    private static readonly TimeSpan MinimumCaptureInterval = TimeSpan.FromSeconds(10);
    private static readonly Uri OllamaEndpoint = new("http://172.20.113.28:11434");
    private const string ModelId = "qwen3-vl:8b";

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

            var displays = _screenSnapshotProvider.GetDisplays();
            if (displays.Count == 0)
            {
                await UpdateStatusAsync("未检测到可用屏幕，5 秒后重试。");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
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
    }

    private async Task CaptureAndAnalyzeDisplayAsync(ScreenSnapshotDisplay display, CancellationToken cancellationToken)
    {
        var capturedAt = DateTimeOffset.Now;
        var fileBaseName = SnapshotRecord.CreateBaseFileName(capturedAt, display.Key);
        var imagePath = Path.Combine(StorageFolderPath, $"{fileBaseName}.png");
        var textPath = Path.Combine(StorageFolderPath, $"{fileBaseName}.txt");

        await _screenSnapshotProvider.TakeSnapshotAsync(display, new FileInfo(imagePath), cancellationToken);

        string analysisText;

        try
        {
            analysisText = await _analysisService.AnalyzeAsync(imagePath, cancellationToken);
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
            textPath,
            analysisText);

        await File.WriteAllTextAsync(
            textPath,
            SnapshotRecord.CreateTextContent(display.Name, capturedAt, analysisText),
            cancellationToken);

        var listItem = await Task.Run(record.ToListItem, cancellationToken);
        await Dispatcher.InvokeAsync(() => AddLiveItem(record, listItem));
    }

    private async Task LoadHistoryIndexAsync()
    {
        var records = await Task.Run(() =>
            Directory.EnumerateFiles(StorageFolderPath, "*.txt", SearchOption.TopDirectoryOnly)
                .Select(path => SnapshotRecord.TryLoad(path, out var record) ? record : null)
                .OfType<SnapshotRecord>()
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

    private void AddLiveItem(SnapshotRecord record, SnapshotListItem listItem)
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

        StatusText = $"已完成 {record.DisplayName} 的截图与解读。当前显示 {_items.Count} 项。";
    }

    private void TrimVisibleItems()
    {
        while (_items.Count > MaxVisibleItems)
        {
            _items.RemoveAt(_items.Count - 1);
        }
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
}