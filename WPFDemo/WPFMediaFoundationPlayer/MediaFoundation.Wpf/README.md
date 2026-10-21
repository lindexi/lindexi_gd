# MediaFoundation.Controls

.NET 10 WPF library; assembly and C# namespace: `MediaFoundation.Controls`. References only MediaFoundation.Core and its own private CsWin32 generator. Build this csproj directly (no solution changes are required).

## Entry point

`MediaFoundation.Controls.MediaPlayer` is sealed, implements `INotifyPropertyChanged` and `IDisposable`, and must be constructed, used and disposed on its owning WPF STA Dispatcher. Add its `VideoView` (`FrameworkElement`, internally an HWND host) to a visible ContentControl. Open before attachment is supported: the URI is queued until HWND creation. Audio-only playback also requires the attached HWND.

Methods: `Open(Uri)`, `Play()`, `Pause()`, `Stop()`, `Seek(TimeSpan)`, `Close()`, `Dispose()`. Open accepts absolute URIs. Play during Opening is deferred until MediaOpened; Pause/Stop cancel that request. Repeated Open replaces and disposes the previous session. Close releases the session but leaves the host reusable; Dispose permanently releases both, session first.

Settings: `Volume` (0..1), `IsMuted`, `PlaybackRate` (positive finite, media-dependent), `IsLooping`. Presets and Close/reopen preserve settings. Read-only observations: `Position`, `Duration`, `State` (Core.PlaybackState), `Source`, `VideoWidth`, `VideoHeight`, `HasVideo`, `HasAudio`, `CanPlay`, `CanPause`, `CanSeek`, `LastError`.

Events: `PropertyChanged`, `MediaOpened`, `PlaybackEnded`, `Failed` (`Core.PlaybackFailedEventArgs.Error`). All run on the UI Dispatcher. Position is sampled every 200 ms. Refresh errors stop polling and surface through LastError/Failed; synchronous entry-point call errors propagate. CanSeek is conservative (ready plus known duration); actual source capabilities may still reject seek or playback rate.

## Ready-to-use control

`MediaFoundation.Controls.MediaPlayerControl : Control, IDisposable` owns its read-only `Player`. Use its bindable `Source` URI DP and `AutoPlay` DP (default true). DefaultStyleKey and Themes/Generic.xaml provide the complete default UI. The default template does not change DataContext.

Commands: `PlayPauseCommand`, `StopCommand`, `OpenFileCommand`, `MuteCommand`, `LoopCommand`, `FullscreenCommand`. They are routed and may target the control explicitly. `FullscreenRequested` is a bubbling routed event; mark Handled to implement a custom host policy. The default policy changes the existing owner Window's style, resize mode and maximized state, and restores them on exit. It never reparents the video HWND. `IsFullscreen` describes only fullscreen owned by this default policy.

The progress slider is named `PART_Progress`; thumb dragging suspends position-following and seeks on release. Keyboard and track changes seek immediately. Live/unknown duration disables seeking. The independent bottom row contains status/error, time, playback, volume/mute, rate, looping and fullscreen controls. No UI is overlaid on the HWND. Space toggles play/pause; left/right seek 5 seconds; up/down adjust volume; M mutes; F toggles fullscreen; Escape exits. Input controls keep their own keyboard navigation. File drop opens the first file.

Temporary Unloaded does not dispose. Explicit Dispose or the tracked owner Window.Closed disposes permanently. If moving between windows, Loaded updates ownership; disposing a permanently removed control is the caller's responsibility. Do not reuse a disposed control. Do not dispose its Player independently while continuing to use the control.

## Resources/localization

Override keys in control, parent or Application resources (DynamicResource lookup):

- `PlayerBackgroundBrush`, `PlayerForegroundBrush`, `PlayerAccentBrush`, `PlayerControlBackgroundBrush`, `PlayerErrorBrush`.
- `PlayerButtonStyle` (Button), `PlayerSliderStyle` (Slider), `PlayerComboBoxStyle` (ComboBox).
- All `Player*Text` string keys in Themes/Strings.xaml.

Operational strings use Strings.resx and standard culture satellite resource lookup. Consumers can replace the control template and retain PART_Progress; TimeText, StatusText and IsFullscreen are read-only DPs available for template bindings.

## Integration test handoff

Run tests on Windows with Media Foundation installed, a visible WPF Window, an STA thread and a running Dispatcher. Construct the player/control there and close/dispose there; do not block the Dispatcher awaiting native events. Real media tests are delegated to the integration/demo agent, not claimed by this library implementation.

Suggested checks using only public APIs:

1. Set volume/mute/rate/loop before attaching or opening; attach VideoView, Open local AV, wait for MediaOpened, verify metadata and presets.
2. Open then immediately Play: verify Playing occurs only after MediaOpened. Pause/Stop during Opening cancels deferred play.
3. Open A then immediately B; verify no stale A events update B. Close during Opening, Close twice, reopen, Dispose twice, then check mutating calls throw ObjectDisposedException.
4. Play audio-only; position advances, HasAudio is true, HasVideo false. Stop returns to zero; seek clamps; end and loop produce PlaybackEnded.
5. Invalid/missing URI targets surface Failed/LastError. Parameter mistakes through the entry point throw synchronously. Observe both event errors and binding validation errors for unsupported rate settings.
6. Exercise actual template thumb dragging: value must not jump to polled position, seek occurs at release. Verify disabled seek for live/unknown duration.
7. Override brush/style/string resources at Application and control scopes. Inspect automation names, tooltips and loading/error/no-media status.
8. Enter/exit fullscreen from normal and maximized owner windows; verify style/state/resize restoration, HWND continuity and playback continuity. Mark FullscreenRequested handled and verify default policy is suppressed.
9. Temporarily remove/reinsert control or replace template while playing; verify it remains usable. Close owner window; verify native session is disposed before child HWND destruction.

Build validation targets: Release win-x64 and Release win-x86. RID compilation is not a substitute for native execution on each architecture.
