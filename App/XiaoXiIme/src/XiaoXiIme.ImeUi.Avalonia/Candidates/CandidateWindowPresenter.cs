using Avalonia;
using Avalonia.Threading;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeInterop;

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
                var request = new ImeUiStateRequest();
                if (OperatingSystem.IsWindows())
                {
                    var gui = new GuiThreadInfo
                        { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<GuiThreadInfo>() };
                    var focus = InputTargetMethods.GetGUIThreadInfo(0, ref gui) ? gui.Focus : 0;
                    InputTargetMethods.GetWindowThreadProcessId(focus, out var process);
                    request = new ImeUiStateRequest(FocusWindowHandle: (long)focus, FocusProcessId: process);
                }

                var uiState = await _client.GetUiStateAsync(request).ConfigureAwait(false);
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
        if (!PositionWindow(state))
        {
            HideWindow();
            WriteStatus("anchor-validation-failed", state);
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        WriteStatus
        (
            state.Anchor is { IsValid: false } || (state.Anchor is null && state.AnchorX == 0 && state.AnchorY == 0)
                ? "visible-fallback-no-valid-anchor"
                : "visible", state
        );
    }

    private bool PositionWindow(CandidateWindowViewState state)
    {
        var point = new PixelPoint(state.AnchorX, state.AnchorY);
        var top = point.Y;
        PixelRect? exclusion = null;
        if (state.Anchor is { IsValid: true } anchor)
        {
            if (anchor.SessionId != state.SessionId || !OperatingSystem.IsWindows())
            {
                return false;
            }

            var window = (nint)anchor.WindowHandle;
            var thread = InputTargetMethods.GetWindowThreadProcessId(window, out var process);
            if (!InputTargetMethods.IsWindow(window) || process != anchor.ProcessId || thread != anchor.ThreadId
                || (anchor.WindowIdentityProperty is { } property
                    && (anchor.WindowIdentity == 0 || (long)InputTargetMethods.GetWindowProperty(window, property) !=
                        anchor.WindowIdentity))
                || !InputTargetMethods.TryGetPhysicalPoint(window, anchor.ClientX, anchor.ClientY, out var current))
            {
                return false;
            }

            var gui = new GuiThreadInfo { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<GuiThreadInfo>() };
            if (!InputTargetMethods.GetGUIThreadInfo(0, ref gui) || gui.Focus != window
                                                                 || InputTargetMethods.GetWindowThreadProcessId
                                                                     (gui.Active, out var foregroundProcess) != thread
                                                                 || foregroundProcess != process)
            {
                return false;
            }

            point = new PixelPoint(current.X, current.Y);
            if (anchor.ClientExclusion is { } area)
            {
                if (area.Left > area.Right || area.Top > area.Bottom
                                           || !InputTargetMethods.TryGetPhysicalPoint
                                               (window, area.Left, area.Top, out var start)
                                           || !InputTargetMethods.TryGetPhysicalPoint
                                               (window, area.Right, area.Bottom, out var end)
                                           || end.X < start.X || end.Y < start.Y)
                {
                    return false;
                }

                exclusion = new PixelRect(start.X, start.Y, end.X - start.X, end.Y - start.Y);
            }

            if (anchor.ClientExcludeTop is { } clientTop
                && InputTargetMethods.TryGetPhysicalPoint(window, anchor.ClientX, clientTop, out var upper))
            {
                top = upper.Y;
            }
            else
            {
                top = current.Y + anchor.ExcludeTop - anchor.ScreenY;
            }
        }
        else if (state.Anchor is not null || (state.AnchorX == 0 && state.AnchorY == 0))
        {
            WriteStatus("hidden-no-valid-anchor", state);
            return false;
        }

        var screen = _window.Screens.ScreenFromPoint(point) ?? _window.Screens.Primary;
        if (screen is null)
        {
            return false;
        }

        _window.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _window.Position = CandidateWindowPlacement.Calculate
        (
            point, top, _window.DesiredSize,
            screen.Scaling, screen.WorkingArea, exclusion
        );
        return true;
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

        try
        {
            var directory = Path.GetDirectoryName(_statusFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporary = _statusFilePath + ".tmp";
            File.WriteAllText
            (
                temporary,
                $"{visibility}|{state.CompositionText}|{state.Candidates.Count}|{state.Selection}|{DateTimeOffset.UtcNow:O}|session={state.SessionId}|source={state.Anchor?.Source}|failure={state.Anchor?.Failure}|position={_window.Position}|scaling={_window.RenderScaling}"
            );
            File.Move(temporary, _statusFilePath, true);
        }
        catch (IOException exception)
        {
            System.Diagnostics.Trace.TraceError("Candidate UI status write failed: {0}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            System.Diagnostics.Trace.TraceError("Candidate UI status write failed: {0}", exception);
        }
    }
}