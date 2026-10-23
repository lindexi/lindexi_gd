# MediaFoundation.Controls

.NET 10 WPF library; assembly and C# namespace: `MediaFoundation.Controls`. References only MediaFoundation.Core and its own private CsWin32 generator. Build this csproj directly (no solution changes are required).

## Entry point

`MediaFoundation.Controls.MediaPlayer` is sealed, implements `INotifyPropertyChanged` and `IDisposable`, and must be constructed, used and disposed on its owning WPF STA Dispatcher. Add its stable `VideoView` (`FrameworkElement`, internally a Grid containing the selected presenter) to a visible ContentControl. Do not cast it to HwndHost. Open before attachment is supported for HWND mode: the URI is queued until HWND creation. HWND audio-only playback also requires the attached HWND.

Methods: `Open(Uri)`, `Play()`, `Pause()`, `Stop()`, `Seek(TimeSpan)`, `Close()`, `Dispose()`. Open accepts absolute URIs. Play during Opening is deferred until MediaOpened; Pause/Stop cancel that request. Repeated Open replaces and disposes the previous session. Close releases the session but leaves the host reusable; Dispose permanently releases both, session first.

Settings: `Volume` (0..1), `IsMuted`, `PlaybackRate` (positive finite, media-dependent), `IsLooping`. Presets and Close/reopen preserve settings. Read-only observations: `Position`, `Duration`, `State` (Core.PlaybackState), `Source`, `VideoWidth`, `VideoHeight`, `HasVideo`, `HasAudio`, `CanPlay`, `CanPause`, `CanSeek`, `LastError`.

Events: `PropertyChanged`, `MediaOpened`, `PlaybackEnded`, `Failed` (`Core.PlaybackFailedEventArgs.Error`). All run on the UI Dispatcher. Position is sampled every 200 ms. Refresh errors stop polling and surface through LastError/Failed; synchronous entry-point call errors propagate. CanSeek is conservative (ready plus known duration); actual source capabilities may still reject seek or playback rate.

## Rendering modes and platform requirements

`MediaPlayer.RenderingMode` and `MediaPlayerControl.RenderingMode` select `VideoRenderingMode.Hwnd` (default, MFPlay child HWND) or `VideoRenderingMode.D3DImage` (MFPlay with the same custom EVR presenter used by the diagnostic prototype, D3D9Ex staging/display surfaces and a WPF Image). Set the mode before Open/Source. Open locks the selection immediately, including a deferred HWND Open; assigning a different mode while locked throws InvalidOperationException. Close unlocks it. VideoView remains the same object across Close, reopen and mode changes; only its internal presenter changes.

HWND retains native-window airspace restrictions. D3DImage participates in WPF composition, enabling WPF overlays, clipping and transforms. It requires D3D9Ex video support; unsupported virtual/remote adapters may fail at initialization. The EVR scheduler owns a bounded sample queue and advances the native media clock independently of UI visibility or D3DImage lock success. The UI copies the latest staging frame only under the D3DImage lock, waits on a GPU fence, then marks the display surface dirty. Closing stops the UI pump and unbinds the back buffer before shutting down MFPlay/EVR and releasing surfaces. It is not guaranteed faster than HWND and is not an absolute zero-copy claim: decoding, sharing, synchronization and WPF composition costs depend on hardware and drivers.

Front-buffer availability changes rebind the existing back buffer, but this is not full device-loss recovery. Device removal/reset does not automatically rebuild all graphics resources or fall back to HWND. Handle errors, Close, and explicitly reopen or select HWND after Close. Validate monitor changes, remote sessions, suspend/resume and device loss on target hardware.

The WPF library currently defaults to x64 and supports explicit x86 (`win-x86` RID or x86 Platform). This changes the previous AnyCPU deployment contract. CsWin32 0.3.183 rejects AnyCPU generation of Direct3DCreate9Ex/IDirect3D9Ex with PInvoke005 due to architecture-specific metadata; its available configuration has no independent target-architecture option. Merely removing PlatformTarget fails compilation; pretending x64 interop is AnyCPU is unsafe. Core remains AnyCPU. Publish applications for the matching architecture.

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

Run tests on Windows with Media Foundation installed, a visible WPF Window, an STA thread and a running Dispatcher. Construct the player/control there and close/dispose there; do not block the Dispatcher awaiting native events. Run the published Demo with `--self-test --d3dimage <media-path>` for D3DImage, or omit `--d3dimage` for HWND. Self-test relative media paths resolve against the executable directory. Publishing with `-p:IncludeSelfTestMedia=true` optionally includes the repository sample as self-test.mp4. Logs (self-test.log) and visual-*.png are written beside the executable. D3DImage Trace reports first frame and presented-frame counts; inspect actual screenshots too, not only metadata or clock advancement.

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

### Sandbox delivery validation

Published x64 Demo was executed in the Windows sandbox with both switches. HWND self-test exited 0: metadata, playback clock, paused-position stability, slider/API seek, Stop reset, volume/mute commands, 2x clock ratio (2.043), EOF/loop, repeated open/close and callback balance (13/13) passed. A foreground screenshot showed actual video content. WASAPI loopback was unavailable (0x80070490), so actual audio output and listening quality were NOT verified.

D3DImage self-test exited 1 at D3D11CreateDevice (SharedVideoSurface), HRESULT 0x887A0004 (unsupported device interface/feature). No D3DImage video frame, audio playback, pause or seek was verified in this environment; HWND results must not be attributed to D3DImage. No silent fallback was added. Compatible GPU/driver execution remains a delivery blocker for D3DImage acceptance.

Recovered evidence: `.coding-agent/d3dimage-validation-fixed/self-test.log` and `.coding-agent/hwnd-validation-fixed/self-test.log`, with HWND `visual-*.png`, relative to the repository root. x86 publish is a compilation/deployment check only, not an x86 execution claim.
