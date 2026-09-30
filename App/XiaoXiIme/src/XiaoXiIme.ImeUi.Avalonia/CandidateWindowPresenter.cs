using Avalonia;
using Avalonia.Threading;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class CandidateWindowPresenter : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(50);
    private readonly XiaoXiImeIpcClient _client;
    private readonly CandidateWindowController _controller = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly string? _statusFilePath;
    private readonly CandidateWindow _window;
    private Task? _refreshTask;

    public CandidateWindowPresenter(CandidateWindow window, XiaoXiImeIpcClient client, string? statusFilePath = null)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _statusFilePath = string.IsNullOrWhiteSpace(statusFilePath) ? null : Path.GetFullPath(statusFilePath);
    }

    public void Start()
    {
        _refreshTask ??= RefreshAsync(_shutdown.Token);
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        _client.Dispose();
        _shutdown.Dispose();
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var uiState = await _client.GetUiStateAsync().ConfigureAwait(false);
                var viewState = _controller.Update(uiState);
                await Dispatcher.UIThread.InvokeAsync(() => ApplyState(viewState));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (TimeoutException)
            {
                await Dispatcher.UIThread.InvokeAsync(HideWindow);
            }
            catch (InvalidOperationException)
            {
                await Dispatcher.UIThread.InvokeAsync(HideWindow);
            }

            await Task.Delay(RefreshInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private void ApplyState(CandidateWindowViewState state)
    {
        if (!state.IsVisible)
        {
            HideWindow();
            return;
        }

        _window.DataContext = state;
        PositionWindow(state);
        if (!_window.IsVisible)
        {
            _window.Show();
        }

        WriteStatus("visible", state);
    }

    private void PositionWindow(CandidateWindowViewState state)
    {
        if (state.AnchorX != 0 || state.AnchorY != 0)
        {
            _window.Position = new PixelPoint(state.AnchorX, state.AnchorY);
            return;
        }

        var screen = _window.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        _window.Position = new PixelPoint(workArea.X + 120, workArea.Bottom - 320);
    }

    private void HideWindow()
    {
        if (_window.IsVisible)
        {
            _window.Hide();
            WriteStatus("hidden", CandidateWindowViewState.Hidden);
        }
    }

    private void WriteStatus(string visibility, CandidateWindowViewState state)
    {
        if (_statusFilePath is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_statusFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            _statusFilePath,
            $"{visibility}|{state.CompositionText}|{state.Candidates.Count}|{state.Selection}|{DateTimeOffset.UtcNow:O}");
    }
}
