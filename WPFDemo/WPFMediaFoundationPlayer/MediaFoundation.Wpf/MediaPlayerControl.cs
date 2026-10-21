using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MediaFoundation.Core;
using Microsoft.Win32;

namespace MediaFoundation.Controls;

/// <summary>包含原生视频区域和独立操作栏的开箱即用播放器。暂时卸载不会释放；拥有者窗口关闭或显式 Dispose 时释放。</summary>
[TemplatePart(Name = "PART_Progress", Type = typeof(Slider))]
public class MediaPlayerControl : Control, IDisposable
{
    private Slider? progress;
    private bool seeking, updatingProgress, disposed;
    private Window? owner;
    private Window? fullscreenWindow;
    private WindowStyle previousStyle;
    private WindowState previousState;
    private ResizeMode previousResize;

    static MediaPlayerControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaPlayerControl), new FrameworkPropertyMetadata(typeof(MediaPlayerControl)));

    /// <summary>创建播放器和默认命令绑定；必须在 WPF UI 线程使用。</summary>
    public MediaPlayerControl()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/MediaFoundation.Controls;component/Themes/Strings.xaml", UriKind.Relative)
        });
        Player = new MediaPlayer();
        Player.PropertyChanged += OnPlayerChanged;
        Loaded += OnLoaded;
        AllowDrop = true;
        Focusable = true;
        Bind(PlayPauseCommand, () => { if (Player.CanPause) Player.Pause(); else Player.Play(); }, () => Player.CanPlay || Player.CanPause || Player.State == PlaybackState.Opening);
        Bind(StopCommand, Player.Stop, () => Player.IsReady);
        Bind(OpenFileCommand, OpenFile, () => true);
        Bind(MuteCommand, () => Player.IsMuted = !Player.IsMuted, () => true);
        Bind(LoopCommand, () => Player.IsLooping = !Player.IsLooping, () => true);
        Bind(FullscreenCommand, ToggleFullscreen, () => Window.GetWindow(this) != null);
        UpdatePresentation();
    }

    /// <summary>唯一播放器入口；控件拥有并释放此对象。</summary>
    public MediaPlayer Player { get; }
    /// <summary>可双向绑定的绝对媒体地址；null 关闭当前媒体。</summary>
    public Uri? Source { get => (Uri?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    /// <summary>Source 依赖属性。</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(MediaPlayerControl), new FrameworkPropertyMetadata(null, OnSourceChanged));
    /// <summary>打开后自动播放，默认 true。</summary>
    public bool AutoPlay { get => (bool)GetValue(AutoPlayProperty); set => SetValue(AutoPlayProperty, value); }
    /// <summary>AutoPlay 依赖属性。</summary>
    public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(nameof(AutoPlay), typeof(bool), typeof(MediaPlayerControl), new FrameworkPropertyMetadata(true));
    /// <summary>进度时间显示。</summary>
    public string TimeText => (string)GetValue(TimeTextProperty);
    private static readonly DependencyPropertyKey TimeTextPropertyKey = DependencyProperty.RegisterReadOnly(nameof(TimeText), typeof(string), typeof(MediaPlayerControl), new PropertyMetadata(""));
    /// <summary>TimeText 只读依赖属性。</summary>
    public static readonly DependencyProperty TimeTextProperty = TimeTextPropertyKey.DependencyProperty;
    /// <summary>加载、无媒体或错误信息。</summary>
    public string StatusText => (string)GetValue(StatusTextProperty);
    private static readonly DependencyPropertyKey StatusTextPropertyKey = DependencyProperty.RegisterReadOnly(nameof(StatusText), typeof(string), typeof(MediaPlayerControl), new PropertyMetadata(""));
    /// <summary>StatusText 只读依赖属性。</summary>
    public static readonly DependencyProperty StatusTextProperty = StatusTextPropertyKey.DependencyProperty;
    /// <summary>是否由此控件进入全屏。</summary>
    public bool IsFullscreen => (bool)GetValue(IsFullscreenProperty);
    private static readonly DependencyPropertyKey IsFullscreenPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsFullscreen), typeof(bool), typeof(MediaPlayerControl), new PropertyMetadata(false));
    /// <summary>IsFullscreen 只读依赖属性。</summary>
    public static readonly DependencyProperty IsFullscreenProperty = IsFullscreenPropertyKey.DependencyProperty;
    /// <summary>播放或暂停。</summary>
    public static RoutedUICommand PlayPauseCommand { get; } = new();
    /// <summary>停止。</summary>
    public static RoutedUICommand StopCommand { get; } = new();
    /// <summary>选择文件。</summary>
    public static RoutedUICommand OpenFileCommand { get; } = new();
    /// <summary>切换静音。</summary>
    public static RoutedUICommand MuteCommand { get; } = new();
    /// <summary>切换循环。</summary>
    public static RoutedUICommand LoopCommand { get; } = new();
    /// <summary>切换当前窗口全屏，不重挂视频 HWND。</summary>
    public static RoutedUICommand FullscreenCommand { get; } = new();
    /// <summary>冒泡请求；宿主可设置 Handled 来自行实现全屏。</summary>
    public static readonly RoutedEvent FullscreenRequestedEvent = EventManager.RegisterRoutedEvent(nameof(FullscreenRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MediaPlayerControl));
    /// <summary>进入或退出全屏前通知宿主。</summary>
    public event RoutedEventHandler FullscreenRequested { add => AddHandler(FullscreenRequestedEvent, value); remove => RemoveHandler(FullscreenRequestedEvent, value); }

    /// <summary>连接默认模板的进度条；重设模板时断开旧处理器。</summary>
    public override void OnApplyTemplate()
    {
        DetachProgress(); base.OnApplyTemplate();
        progress = GetTemplateChild("PART_Progress") as Slider;
        if (progress != null)
        {
            progress.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnDragStarted));
            progress.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
            progress.ValueChanged += OnProgressChanged;
        }
        UpdatePresentation();
    }
    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MediaPlayerControl)d;
        control.VerifyAccess(); ObjectDisposedException.ThrowIf(control.disposed, control);
        control.Run(() =>
        {
            if (e.NewValue is Uri uri) { control.Player.Open(uri); if (control.AutoPlay) control.Player.Play(); }
            else control.Player.Close();
        });
    }
    private void Bind(ICommand command, Action action, Func<bool> canExecute) => CommandBindings.Add(new CommandBinding(command,
        (_, e) => { Run(action); e.Handled = true; }, (_, e) => { e.CanExecute = !disposed && canExecute(); e.Handled = true; }));
    private void Run(Action action)
    {
        try { action(); }
        catch (ExternalException error) { Player.ReportError(error); }
        catch (ArgumentException error) { Player.ReportError(error); }
        catch (InvalidOperationException error) { Player.ReportError(error); }
        catch (System.IO.IOException error) { Player.ReportError(error); }
        catch (UnauthorizedAccessException error) { Player.ReportError(error); }
    }
    private void OpenFile()
    {
        var dialog = new OpenFileDialog { Filter = Text.Get("FileFilter"), CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) SetCurrentValue(SourceProperty, new Uri(dialog.FileName));
    }
    private void OnPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdatePresentation(); CommandManager.InvalidateRequerySuggested();
    }
    private void UpdatePresentation()
    {
        SetValue(TimeTextPropertyKey, $"{Format(Player.Position)} / {Format(Player.Duration)}");
        SetValue(StatusTextPropertyKey, Player.LastError?.Message ?? (Player.State == PlaybackState.Opening ? Text.Get("Loading") : Player.Source == null ? Text.Get("NoMedia") : ""));
        if (progress != null && !seeking)
        {
            updatingProgress = true;
            try { progress.Maximum = Math.Max(1, Player.Duration.TotalSeconds); progress.Value = Player.Position.TotalSeconds; }
            finally { updatingProgress = false; }
        }
    }
    private static string Format(TimeSpan time) => $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    private void OnDragStarted(object sender, DragStartedEventArgs e) => seeking = true;
    private void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        seeking = false;
        if (!e.Canceled && progress != null && !disposed) Run(() => Player.Seek(TimeSpan.FromSeconds(progress.Value)));
        UpdatePresentation();
    }
    private void OnProgressChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updatingProgress && !seeking && !disposed) Run(() => Player.Seek(TimeSpan.FromSeconds(e.NewValue)));
    }
    private void DetachProgress()
    {
        if (progress == null) return;
        progress.RemoveHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnDragStarted));
        progress.RemoveHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
        progress.ValueChanged -= OnProgressChanged; progress = null; seeking = false;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (disposed) return;
        var window = Window.GetWindow(this);
        if (ReferenceEquals(owner, window)) return;
        ExitFullscreen();
        if (owner != null) owner.Closed -= OnOwnerClosed;
        owner = window;
        if (owner != null) owner.Closed += OnOwnerClosed;
    }
    private void OnOwnerClosed(object? sender, EventArgs e) => Dispose();
    private void ToggleFullscreen()
    {
        var request = new RoutedEventArgs(FullscreenRequestedEvent, this); RaiseEvent(request);
        if (request.Handled) return;
        if (IsFullscreen) { ExitFullscreen(); return; }
        var window = Window.GetWindow(this);
        if (window == null) return;
        fullscreenWindow = window; previousStyle = window.WindowStyle; previousState = window.WindowState; previousResize = window.ResizeMode;
        window.WindowState = WindowState.Normal; window.WindowStyle = WindowStyle.None; window.ResizeMode = ResizeMode.NoResize; window.WindowState = WindowState.Maximized;
        SetValue(IsFullscreenPropertyKey, true);
    }
    private void ExitFullscreen()
    {
        if (fullscreenWindow is not { } window) return;
        fullscreenWindow = null;
        window.WindowState = WindowState.Normal; window.WindowStyle = previousStyle; window.ResizeMode = previousResize; window.WindowState = previousState;
        SetValue(IsFullscreenPropertyKey, false);
    }
    /// <summary>Space 播放暂停，左右定位五秒，上下调节音量，M 静音，F 全屏，Escape 退出全屏；输入控件保留自身按键。</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (disposed || e.Handled || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is TextBoxBase or ComboBox or ComboBoxItem or Slider) return;
        Action? action = e.Key switch
        {
            Key.Space => () => { if (PlayPauseCommand.CanExecute(null, this)) PlayPauseCommand.Execute(null, this); },
            Key.Left => () => Player.Seek(Player.Position - TimeSpan.FromSeconds(5)),
            Key.Right => () => Player.Seek(Player.Position + TimeSpan.FromSeconds(5)),
            Key.Up => () => Player.Volume = Math.Min(1, Player.Volume + 0.05),
            Key.Down => () => Player.Volume = Math.Max(0, Player.Volume - 0.05),
            Key.M => () => Player.IsMuted = !Player.IsMuted,
            Key.F => ToggleFullscreen,
            Key.Escape when IsFullscreen => ExitFullscreen,
            _ => null
        };
        if (action != null) { Run(action); e.Handled = true; }
    }
    /// <summary>接受单个文件拖放。</summary>
    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e); e.Effects = !disposed && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true;
    }
    /// <summary>打开拖入的第一个文件。</summary>
    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        if (!disposed && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
            Run(() => SetCurrentValue(SourceProperty, new Uri(paths[0])));
        e.Handled = true;
    }
    /// <summary>永久释放控件及播放器；暂时 Unloaded 不会调用此方法。重复调用安全。</summary>
    public void Dispose()
    {
        VerifyAccess(); if (disposed) return;
        ExitFullscreen(); disposed = true;
        Loaded -= OnLoaded; if (owner != null) owner.Closed -= OnOwnerClosed; owner = null;
        DetachProgress(); Player.PropertyChanged -= OnPlayerChanged; Player.Dispose();
        CommandManager.InvalidateRequerySuggested();
    }
}
