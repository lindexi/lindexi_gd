using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaFoundation.Controls;

namespace MediaFoundation.PlayerDemo;

internal static class VisualTest
{
    internal static async Task RunProgressAsync(MediaPlayerControl control, Action<string> log, CancellationToken ct)
    {
        var slider = (Slider)control.Template.FindName("PART_Progress", control);
        var track = (System.Windows.Controls.Primitives.Track)slider.Template.FindName("PART_Track", slider);
        void Move(double fraction)
        {
            var point = track.PointToScreen(new Point(7 + (track.ActualWidth - 14) * fraction, track.ActualHeight / 2));
            if (!SetCursorPos((int)point.X, (int)point.Y)) throw new InvalidOperationException("SetCursorPos failed");
        }
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            log("PASS INPUT " + name);
        }
        double target = slider.Maximum * 0.3;
        Move(0.3);
        mouse_event(2, 0, 0, 0, 0);
        try
        {
            await Task.Delay(350, ct);
            Check(track.Thumb.IsDragging, "track click delegates capture to real Thumb");
            Check(Math.Abs(control.Player.Position.TotalSeconds - target) < 0.6, "track mouse-down immediately seeks public position");
            Move(0.6);
            await Task.Delay(150, ct);
            double held = slider.Value;
            await Task.Delay(500, ct);
            Check(Math.Abs(slider.Value - held) < 0.01, "polling does not overwrite held drag");
            target = held;
        }
        finally { mouse_event(4, 0, 0, 0, 0); }
        await Task.Delay(650, ct);
        Check(!track.Thumb.IsDragging && Math.Abs(control.Player.Position.TotalSeconds - target) < 0.6, "release commits dragged public position");
        var thumbPoint = track.Thumb.PointToScreen(new Point(track.Thumb.ActualWidth / 2, track.Thumb.ActualHeight / 2));
        SetCursorPos((int)thumbPoint.X, (int)thumbPoint.Y);
        mouse_event(2, 0, 0, 0, 0);
        try
        {
            await Task.Delay(100, ct);
            Check(track.Thumb.IsDragging, "direct template Thumb hit starts drag");
            Move(0.4);
            await Task.Delay(150, ct);
            target = slider.Value;
        }
        finally { mouse_event(4, 0, 0, 0, 0); }
        await Task.Delay(650, ct);
        Check(Math.Abs(control.Player.Position.TotalSeconds - target) < 0.6, "direct Thumb release seeks");
        double beforeCancel = control.Player.Position.TotalSeconds;
        thumbPoint = track.Thumb.PointToScreen(new Point(track.Thumb.ActualWidth / 2, track.Thumb.ActualHeight / 2));
        SetCursorPos((int)thumbPoint.X, (int)thumbPoint.Y);
        mouse_event(2, 0, 0, 0, 0);
        try
        {
            await Task.Delay(100, ct);
            Move(0.7);
            await Task.Delay(100, ct);
            keybd_event(0x1B, 0, 0, 0);
            keybd_event(0x1B, 0, 2, 0);
            await Task.Delay(200, ct);
            Check(!track.Thumb.IsDragging, "Escape cancels native Thumb capture");
        }
        finally { mouse_event(4, 0, 0, 0, 0); }
        await Task.Delay(350, ct);
        Check(Math.Abs(control.Player.Position.TotalSeconds - beforeCancel) < 0.6, "canceled drag does not seek");
        slider.Focus();
        double beforeKeys = slider.Value;
        for (int i = 0; i < 3; i++)
        {
            keybd_event(0x27, 0, 0, 0);
            keybd_event(0x27, 0, 2, 0);
            await Task.Delay(80, ct);
        }
        target = Math.Min(slider.Maximum, beforeKeys + 3 * slider.SmallChange);
        await Task.Delay(650, ct);
        Check(Math.Abs(control.Player.Position.TotalSeconds - target) < 0.6, "focused repeated Right keys seek final slider value");
    }

    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nuint extraInfo);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extraInfo);

    internal static async Task RunAsync(Window window, MediaPlayerControl control, Action<string> log, CancellationToken ct)
    {
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            log("PASS VISUAL " + name);
        }
        T Part<T>(string name) where T : FrameworkElement => (T)control.Template.FindName(name, control);
        async Task SettleAsync()
        {
            nint hwnd = new WindowInteropHelper(window).Handle;
            ShowWindow(hwnd, 9);
            window.Left = 0; window.Top = 0;
            window.Topmost = true;
            window.Activate();
            SetForegroundWindow(hwnd);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(1800, ct);
            if (GetForegroundWindow() != hwnd) throw new InvalidOperationException("Capture window is not foreground");
        }
        void Validate()
        {
            foreach (string name in new[] { "Open", "Play", "Stop", "Mute", "Loop", "Fullscreen" })
            {
                var button = Part<Button>(name);
                var icon = (System.Windows.Shapes.Path)button.Content;
                log($"ICON {name} data={icon.Data} size={icon.ActualWidth}x{icon.ActualHeight} fill={icon.Fill} style={icon.Style} button={button.ActualWidth}x{button.ActualHeight}");
                Check(icon.Data != null && !icon.Data.Bounds.IsEmpty && icon.ActualWidth >= 10 && icon.ActualHeight >= 10 && icon.Fill != null, name + " actual vector nonempty");
                Check(button.ActualWidth >= 40 && button.ActualHeight >= 40, name + " hit bounds");
                Check(!string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)) && button.ToolTip is string { Length: > 0 }, name + " localized label");
                var bounds = button.TransformToAncestor(control).TransformBounds(new Rect(button.RenderSize));
                Check(bounds.Left >= 0 && bounds.Right <= control.ActualWidth && bounds.Bottom <= control.ActualHeight, name + " inside viewport");
            }
            var leftBounds = Part<Button>("Stop").TransformToAncestor(control).TransformBounds(new Rect(Part<Button>("Stop").RenderSize));
            var rightBounds = Part<Button>("Mute").TransformToAncestor(control).TransformBounds(new Rect(Part<Button>("Mute").RenderSize));
            Check(rightBounds.Left - leftBounds.Right >= 24 && Math.Abs(leftBounds.Top - rightBounds.Top) < 1, "separate left and right groups on same row");
            var fullBounds = Part<Button>("Fullscreen").TransformToAncestor(control).TransformBounds(new Rect(Part<Button>("Fullscreen").RenderSize));
            Check(control.ActualWidth - fullBounds.Right <= 20, "fullscreen at far right");
            var progressBounds = Part<Slider>("PART_Progress").TransformToAncestor(control).TransformBounds(new Rect(Part<Slider>("PART_Progress").RenderSize));
            Check(control.ActualHeight - progressBounds.Top <= 112, "compact toolbar at most 112 DIP");
            var time = Part<TextBlock>("Time");
            Check(time.IsVisible && time.ActualWidth > 80 && time.Text.Contains('/'), "time text visible");
            var rate = Part<ComboBox>("Rate");
            Check(rate.SelectedItem != null && rate.ActualWidth >= 70 && rate.Template != null, "rate selected text and bounds");
            var volume = Part<Slider>("Volume");
            var stop = Part<Button>("Stop");
            var mute = Part<Button>("Mute");
            var loop = Part<Button>("Loop");
            Check(stop.TransformToAncestor(control).Transform(new Point(stop.ActualWidth, 0)).X <= mute.TransformToAncestor(control).Transform(new Point()).X, "left and center do not overlap");
            Check(volume.TransformToAncestor(control).Transform(new Point(volume.ActualWidth, 0)).X <= rate.TransformToAncestor(control).Transform(new Point()).X, "center and right do not overlap");
        }
        window.WindowState = WindowState.Normal;
        window.Width = 1100; window.Height = 720;
        control.Player.Play();
        while (control.Player.State != MediaFoundation.Core.PlaybackState.Playing || control.Player.Position < TimeSpan.FromSeconds(4.5))
            await Task.Delay(30, ct);
        log($"VISUAL sampling actual playing position={control.Player.Position} mode={control.RenderingMode}");
        await SettleAsync();
        control.Player.Pause();
        await SettleAsync(); Validate();
        if (control.RenderingMode == VideoRenderingMode.D3DImage)
        {
            var view = control.Player.VideoView;
            int width = (int)view.ActualWidth, height = (int)view.ActualHeight;
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(System.IO.Path.Combine(AppContext.BaseDirectory, "visual-d3dimage-wpf.png"))) encoder.Save(stream);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            var colors = new HashSet<int>();
            for (int y = height / 4; y < height / 2; y += 3)
                for (int x = width / 4; x < width * 3 / 4; x += 3)
                {
                    int i = (y * width + x) * 4;
                    colors.Add(pixels[i] | (pixels[i + 1] << 8) | (pixels[i + 2] << 16));
                }
            log($"D3DImage WPF capture colors={colors.Count} tier={RenderCapability.Tier >> 16} mode={RenderOptions.ProcessRenderMode}");
            Check(colors.Count >= 32, "D3DImage WPF video pixels");
        }
        Capture(window, "visual-normal-printwindow.png", true); Capture(window, "visual-normal.png");
        var play = Part<System.Windows.Shapes.Path>("PlayIcon");
        string before = play.Data.ToString();
        control.Player.Play();
        await SettleAsync();
        log($"PLAY STATE {control.Player.State} CanPause={control.Player.CanPause} before={before} after={play.Data}");
        Check(control.Player.CanPause && play.Data.ToString() != before, "playing shows pause icon");
        control.Player.Pause(); await SettleAsync();
        Check(play.Data.ToString() == before, "paused shows play icon");
        var muteIcon = Part<System.Windows.Shapes.Path>("MuteIcon");
        string originalMute = muteIcon.Data.ToString();
        control.Player.IsMuted = true; control.Player.IsLooping = true;
        await SettleAsync();
        Check(muteIcon.Data.ToString() != originalMute && AutomationProperties.GetName(Part<Button>("Mute")).Contains("取消"), "mute icon and unmute label");
        Check(Part<Button>("Loop").BorderBrush == control.FindResource("PlayerAccentBrush"), "loop selection border");
        Capture(window, "visual-selected.png");
        control.Player.IsMuted = false; control.Player.IsLooping = false;
        MediaPlayerControl.FullscreenCommand.Execute(null, control); await SettleAsync();
        Check(control.IsFullscreen && AutomationProperties.GetName(Part<Button>("Fullscreen")).Contains("退出"), "fullscreen state and label");
        MediaPlayerControl.FullscreenCommand.Execute(null, control);
        window.Width = 640; window.Height = 520;
        await SettleAsync(); Validate(); Capture(window, "visual-narrow-printwindow.png", true); Capture(window, "visual-narrow.png");
        var more = control.ToolbarContent as Button ?? throw new InvalidOperationException("Missing Demo toolbar content");
        var moreBounds = more.TransformToAncestor(control).TransformBounds(new Rect(more.RenderSize));
        Check(moreBounds.Left >= 0 && moreBounds.Right <= control.ActualWidth && moreBounds.Bottom <= control.ActualHeight, "bottom More inside narrow viewport");
        Check(ReferenceEquals(more.Style, control.FindResource("PlayerButtonStyle")) && !string.IsNullOrWhiteSpace(AutomationProperties.GetName(more)), "More shares dynamic style and accessible label");
        control.IsEnabled = false;
        Check(!more.IsEnabled, "More inherits disabled state during mode switch");
        control.IsEnabled = true;
        more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Task.Delay(500, ct);
        Check(more.ContextMenu is { IsOpen: true } && ReferenceEquals(more.ContextMenu.PlacementTarget, more), "More popup anchored to bottom button");
        var menuContent = (FrameworkElement)VisualTreeHelper.GetChild(more.ContextMenu!, 0);
        var menuBounds = new Rect(menuContent.PointToScreen(new Point()), menuContent.PointToScreen(new Point(menuContent.ActualWidth, menuContent.ActualHeight)));
        var menuWorkArea = Rect.Transform(SystemParameters.WorkArea, PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice);
        Check(menuWorkArea.Contains(menuBounds), "More menu fully inside screen work area");
        Capture(window, "visual-more-narrow.png", extraBottom: 180, extraRight: 384);
        more.ContextMenu!.IsOpen = false;
        Part<ComboBox>("Rate").IsDropDownOpen = true;
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(1800, ct);
        Check(Part<ComboBox>("Rate").IsDropDownOpen, "rate popup open");
        var popup = (System.Windows.Controls.Primitives.Popup)Part<ComboBox>("Rate").Template.FindName("PART_Popup", Part<ComboBox>("Rate"));
        var popupContent = (FrameworkElement)popup.Child;
        var popupBounds = new Rect(popupContent.PointToScreen(new Point()), popupContent.PointToScreen(new Point(popupContent.ActualWidth, popupContent.ActualHeight)));
        var workArea = Rect.Transform(SystemParameters.WorkArea, PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice);
        log($"RATE POPUP bounds={popupBounds} screenWorkArea={workArea}");
        Check(workArea.Contains(popupBounds), "rate popup fully inside screen work area");
        Capture(window, "visual-rate.png", extraBottom: 220);
        Part<ComboBox>("Rate").IsDropDownOpen = false;
        window.Topmost = false;
        window.Width = 1100; window.Height = 720;
        log("PASS VISUAL actual foreground screen GetDC/BitBlt screenshots (native HWND included), PrintWindow comparisons saved: " + AppContext.BaseDirectory);
    }

    internal static async Task RunEmptyAsync(Window window, MediaPlayerControl control, Action<string> log, CancellationToken ct)
    {
        window.Left = 0; window.Top = 0;
        window.Topmost = true;
        window.Activate();
        SetForegroundWindow(new WindowInteropHelper(window).Handle);
        await Task.Delay(1200, ct);
        var hint = (TextBlock)control.Template.FindName("EmptyHint", control);
        var video = (ContentControl)control.Template.FindName("Video", control);
        if (!hint.IsVisible || video.IsVisible || control.Source != null)
            throw new InvalidOperationException("Empty state must show WPF hint without native video host");
        Capture(window, "visual-empty.png", expectVideo: false);
        log("PASS VISUAL empty hint visible, native host collapsed");
    }

    internal static void Capture(Window window, string name, bool printWindow = false, int extraBottom = 0, bool expectVideo = true, int extraRight = 0)
    {
        nint hwnd = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(hwnd, out var rect)) throw new System.ComponentModel.Win32Exception();
        int width = rect.Right - rect.Left + extraRight, height = rect.Bottom - rect.Top + extraBottom;
        nint screen = GetDC(0), memory = CreateCompatibleDC(screen), bitmap = CreateCompatibleBitmap(screen, width, height);
        nint old = SelectObject(memory, bitmap);
        try
        {
            if (printWindow)
            {
                if (!PrintWindow(hwnd, memory, 2)) throw new System.ComponentModel.Win32Exception();
            }
            else if (!BitBlt(memory, 0, 0, width, height, screen, rect.Left, rect.Top, 0x40CC0020))
                throw new System.ComponentModel.Win32Exception();
            SelectObject(memory, old);
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32, SizeImage = (uint)(width * height * 4) };
            byte[] pixels = new byte[width * height * 4];
            if (GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref info, 0) == 0) throw new System.ComponentModel.Win32Exception();
            if (!printWindow && expectVideo)
            {
                var colors = new HashSet<int>();
                for (int y = height / 4; y < height / 2; y += 3)
                    for (int x = width / 4; x < width * 3 / 4; x += 3)
                    {
                        int i = (y * width + x) * 4;
                        colors.Add(pixels[i] | (pixels[i + 1] << 8) | (pixels[i + 2] << 16));
                    }
                if (colors.Count < 32) throw new InvalidOperationException($"Blank video screenshot: {name}, colors={colors.Count}");
            }
            var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, width * 4);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(System.IO.Path.Combine(AppContext.BaseDirectory, name)); encoder.Save(stream);
        }
        finally { SelectObject(memory, old); DeleteObject(bitmap); DeleteDC(memory); ReleaseDC(0, screen); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, SizeImage; public int XPels, YPels; public uint Used, Important; }
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint hwnd, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool PrintWindow(nint hwnd, nint dc, uint flags);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out RectNative rect);
    [DllImport("user32.dll")] private static extern nint GetDC(nint hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint hwnd, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint target, int x, int y, int width, int height, nint source, int sx, int sy, uint operation);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(nint dc, nint bitmap, uint start, uint lines, byte[] pixels, ref BitmapInfo info, uint usage);
}
