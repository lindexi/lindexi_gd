using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class StatusBarWindow : Window
{
    private readonly ProductUiController _owner;
    private PixelPoint? _dragStart;
    private PixelPoint _windowStart;
    private ImeStatusPositionKey? _positionKey;
    private delegate nint WindowProcedure(nint hwnd, uint message, nint wparam, nint lparam);
    private WindowProcedure? _procedure;
    private nint _oldProcedure;
    internal TextBlock Status { get; } = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 270 };
    private sealed record ImeStatusPositionKey(int? X, int? Y);

    internal StatusBarWindow(ProductUiController owner)
    {
        _owner = owner;
        Title = ProductResources.Text("Title");
        Width = 360; Height = 92;
        CanResize = false; ShowActivated = false; ShowInTaskbar = false; Topmost = true;
        SystemDecorations = SystemDecorations.None;
        Background = Brushes.White;
        var panel = new StackPanel { Margin = new Thickness(8), Spacing = 4 };
        var title = new TextBlock { Text = ProductResources.Text("Title"), FontSize = 14, Foreground = Brushes.RoyalBlue };
        title.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            _dragStart = this.PointToScreen(e.GetPosition(this));
            _windowStart = Position;
            e.Pointer.Capture(title);
        };
        title.PointerMoved += (_, e) =>
        {
            if (_dragStart is not { } start) return;
            var point = this.PointToScreen(e.GetPosition(this));
            Position = Clamp(new PixelPoint(_windowStart.X + point.X - start.X, _windowStart.Y + point.Y - start.Y));
        };
        title.PointerReleased += async (_, e) =>
        {
            if (_dragStart is null) return;
            _dragStart = null;
            e.Pointer.Capture(null);
            await _owner.SavePositionAsync();
        };
        panel.Children.Add(title);
        panel.Children.Add(Status);
        var buttons = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
        Add(buttons, "Settings", () => { _owner.OpenSettings(); return Task.CompletedTask; });
        Add(buttons, "About", () => { _owner.OpenAbout(); return Task.CompletedTask; });
        Add(buttons, "Hide", () => _owner.ChangeVisibilityAsync(false));
        Add(buttons, "Exit", () => { _owner.Exit(); return Task.CompletedTask; });
        panel.Children.Add(buttons);
        Content = panel;
        Opened += (_, _) => InstallNonActivation();
        Closed += (_, _) => RestoreProcedure();
    }

    private static void Add(StackPanel panel, string key, Func<Task> action)
    {
        var button = new Button { Content = ProductResources.Text(key), Focusable = false, MinHeight = 32, Padding = new Thickness(6, 2) };
        global::Avalonia.Automation.AutomationProperties.SetName(button, ProductResources.Text(key));
        button.Click += async (_, _) => await action();
        panel.Children.Add(button);
    }

    internal void ApplyPosition(XiaoXiIme.Foundation.ImeStatusBarPosition? position, bool force = false)
    {
        var key = new ImeStatusPositionKey(position?.X, position?.Y);
        if (!force && _positionKey == key) return;
        _positionKey = key;
        var area = Screens.Primary?.WorkingArea ?? new PixelRect(0, 0, 1024, 768);
        var scale = Screens.Primary?.Scaling ?? 1;
        Position = Clamp(position is null ? new PixelPoint(area.Right - (int)((Width + 16) * scale), area.Bottom - (int)((Height + 16) * scale)) : new PixelPoint(position.X, position.Y));
    }

    private PixelPoint Clamp(PixelPoint position)
    {
        var screen = Screens.ScreenFromPoint(position) ?? Screens.Primary;
        var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1024, 768);
        var scale = screen?.Scaling ?? 1;
        return new PixelPoint(Math.Clamp(position.X, area.X, Math.Max(area.X, area.Right - (int)(Width * scale))),
            Math.Clamp(position.Y, area.Y, Math.Max(area.Y, area.Bottom - (int)(Height * scale))));
    }

    private void InstallNonActivation()
    {
        if (!OperatingSystem.IsWindows() || _procedure is not null || TryGetPlatformHandle() is not { } handle) return;
        var style = GetWindowLongPtr(handle.Handle, -20);
        SetWindowLongPtr(handle.Handle, -20, style | 0x08000000);
        _procedure = (hwnd, message, wparam, lparam) => message == 0x21 ? 3 : CallWindowProc(_oldProcedure, hwnd, message, wparam, lparam);
        _oldProcedure = SetWindowLongPtr(handle.Handle, -4, Marshal.GetFunctionPointerForDelegate(_procedure));
    }
    private void RestoreProcedure()
    {
        if (OperatingSystem.IsWindows() && _oldProcedure != 0 && TryGetPlatformHandle() is { } handle)
            SetWindowLongPtr(handle.Handle, -4, _oldProcedure);
        _procedure = null; _oldProcedure = 0;
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] private static extern nint CallWindowProc(nint previous, nint hwnd, uint message, nint wparam, nint lparam);
}
