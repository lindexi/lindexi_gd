using Avalonia;
using Avalonia.Threading;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class CandidateWindowPresenter : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(50);
    private readonly XiaoXiImeIpcClient? _client;
    private readonly XiaoXiIme.ImeHost.ImeHostService? _host;
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

    public CandidateWindowPresenter(CandidateWindow window, XiaoXiIme.ImeHost.ImeHostService host, string? statusFilePath)
    {
        _window = window;
        _host = host;
        _statusFilePath = string.IsNullOrWhiteSpace(statusFilePath) ? null : Path.GetFullPath(statusFilePath);
    }

    public void Start()
    {
        if (_host is null)
        {
            _refreshTask ??= RefreshAsync(_shutdown.Token);
            return;
        }

        _window.CandidatePressed += SelectCandidate;
        _host.CandidateStateChanged += ScheduleRefresh;
        ScheduleRefresh();
    }

    public void Dispose()
    {
        if (_host is not null)
        {
            _window.CandidatePressed -= SelectCandidate;
            _host.CandidateStateChanged -= ScheduleRefresh;
        }
        _shutdown.Cancel();
        _client?.Dispose();
        _shutdown.Dispose();
    }

    private void SelectCandidate(CandidateWindowViewState state, int index)
    {
        var focus = GetFocusRequest();
        if (state.Anchor is { } anchor && anchor.WindowHandle == focus.FocusWindowHandle && anchor.ProcessId == focus.FocusProcessId)
        {
            _host?.SelectDisplayedCandidate(state.SessionId, state.CandidateRevision, index);
        }
    }

    private int _refreshScheduled;

    private void ScheduleRefresh()
    {
        if (Interlocked.Exchange(ref _refreshScheduled, 1) != 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            Interlocked.Exchange(ref _refreshScheduled, 0);
            if (_shutdown.IsCancellationRequested || _host is not { } host)
            {
                return;
            }

            var state = await host.GetUiStateAsync(GetFocusRequest());
            ApplyState(_controller.Update(state));
        });
    }

    private static ImeUiStateRequest GetFocusRequest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ImeUiStateRequest();
        }

        var gui = new GuiThreadInfo { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<GuiThreadInfo>() };
        var focus = InputTargetMethods.GetGUIThreadInfo(0, ref gui) ? gui.Focus : 0;
        InputTargetMethods.GetWindowThreadProcessId(focus, out var process);
        return new ImeUiStateRequest(FocusWindowHandle: (long)focus, FocusProcessId: process);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var uiState = await _client!.GetUiStateAsync(GetFocusRequest()).ConfigureAwait(false);
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
            WriteStatus("no-visible-candidates", state);
            return;
        }

        _window.DataContext = state;
        if (!PositionWindow(state, out var usedFallback))
        {
            HideWindow();
            return;
        }

        if (!_window.IsVisible)
        {
            _window.Show();
        }

        if (_statusFilePath is not null)
        {
            var visibility = usedFallback ? "visible-fallback-no-valid-anchor" : "visible";
            Dispatcher.UIThread.Post(() =>
            {
                if (_window.IsVisible && ReferenceEquals(_window.DataContext, state))
                {
                    _window.UpdateLayout();
                    WriteStatus(visibility, state);
                }
            }, DispatcherPriority.Background);
        }
    }

    private bool PositionWindow(CandidateWindowViewState state, out bool usedFallback)
    {
        usedFallback = false;
        var point = new PixelPoint(state.AnchorX, state.AnchorY);
        var top = point.Y;
        PixelRect? exclusion = null;
        if (state.Anchor is { } anchor)
        {
            if (anchor.SessionId != state.SessionId || !OperatingSystem.IsWindows())
            {
                return false;
            }

            var window = (nint)anchor.WindowHandle;
            var thread = InputTargetMethods.GetWindowThreadProcessId(window, out var process);
            if (!InputTargetMethods.IsWindow(window) || process != anchor.ProcessId || thread != anchor.ThreadId)
            {
                WriteStatus($"anchor-target-mismatch process={process} thread={thread}", state);
                return false;
            }
            if (anchor.WindowIdentityProperty is { } property)
            {
                var actual = (long)InputTargetMethods.GetWindowProperty(window, property);
                if (anchor.WindowIdentity == 0 || actual != anchor.WindowIdentity)
                {
                    WriteStatus($"anchor-window-identity-mismatch actual={actual}", state);
                    return false;
                }
            }
            var gui = new GuiThreadInfo { Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<GuiThreadInfo>() };
            // Attached input queues can report an active top-level window owned by a different thread.
            if (!InputTargetMethods.GetGUIThreadInfo(0, ref gui) || gui.Focus != window || gui.Active == 0 ||
                GetAncestor(gui.Active, 2) != GetAncestor(window, 2))
            {
                WriteStatus($"anchor-focus-mismatch focus=0x{gui.Focus:X} active=0x{gui.Active:X}", state);
                return false;
            }

            if (anchor.IsValid && InputTargetMethods.TryGetPhysicalPoint(window, anchor.ClientX, anchor.ClientY, out var current))
            {
                point = new PixelPoint(current.X, current.Y);
                top = anchor.ClientExcludeTop is { } clientTop &&
                      InputTargetMethods.TryGetPhysicalPoint(window, anchor.ClientX, clientTop, out var upper)
                    ? upper.Y : current.Y + anchor.ExcludeTop - anchor.ScreenY;

                if (anchor.ClientExclusion is { } area && area.Left <= area.Right && area.Top <= area.Bottom &&
                    InputTargetMethods.TryGetPhysicalPoint(window, area.Left, area.Top, out var start) &&
                    InputTargetMethods.TryGetPhysicalPoint(window, area.Right, area.Bottom, out var end) &&
                    end.X > start.X && end.Y > start.Y)
                {
                    exclusion = new PixelRect(start.X, start.Y, end.X - start.X, end.Y - start.Y);
                }
            }
            else
            {
                usedFallback = true;
                if (InputTargetMethods.TryGetPhysicalPoint(window, 8, 24, out var origin))
                {
                    point = new PixelPoint(origin.X, origin.Y);
                }
                else if (!anchor.IsValid)
                {
                    point = _window.Screens.Primary?.WorkingArea.Position ?? default;
                }
                top = point.Y;
            }
        }
        else if (state.AnchorX == 0 && state.AnchorY == 0)
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

    [System.Runtime.InteropServices.DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint GetAncestor(nint window, uint flags);

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
                $"{visibility}|{state.CompositionText}|{state.Candidates.Count}|{state.Selection}|{DateTimeOffset.UtcNow:O}|session={state.SessionId}|source={state.Anchor?.Source}|failure={state.Anchor?.Failure}|anchor={state.Anchor}|position={_window.Position}|scaling={_window.RenderScaling}|firstCandidate={_window.GetFirstCandidateCenter()}"
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