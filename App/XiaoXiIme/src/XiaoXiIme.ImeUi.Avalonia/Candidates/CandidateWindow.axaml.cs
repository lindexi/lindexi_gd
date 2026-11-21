using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed partial class CandidateWindow : Window
{
    private delegate nint WindowProcedure(nint window, uint message, nint wParam, nint lParam);
    private WindowProcedure? _procedure;
    private nint _previousProcedure;

    internal event Action<CandidateWindowViewState, int>? CandidatePressed;

    public CandidateWindow()
    {
        InitializeComponent();
        Opened += (_, _) => InstallNonActivation();
        Closing += (_, _) => RestoreProcedure();
    }

    private void OnCandidatePressed(object? sender, PointerPressedEventArgs args)
    {
        if (sender is Control { DataContext: CandidateWindowCandidateViewModel candidate } row &&
            DataContext is CandidateWindowViewState state && args.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
        {
            CandidatePressed?.Invoke(state, candidate.CandidateIndex);
            args.Handled = true;
        }
    }

    internal global::Avalonia.PixelPoint? GetFirstCandidateCenter()
    {
        var row = this.GetVisualDescendants().OfType<Border>()
            .FirstOrDefault(border => border.DataContext is CandidateWindowCandidateViewModel && border.Bounds.Width > 0);
        return row?.PointToScreen(new global::Avalonia.Point(row.Bounds.Width / 2, row.Bounds.Height / 2));
    }

    private void InstallNonActivation()
    {
        if (!OperatingSystem.IsWindows() || _procedure is not null || TryGetPlatformHandle() is not { } handle)
        {
            return;
        }

        var window = handle.Handle;
        SetWindowLongPtr(window, -20, GetWindowLongPtr(window, -20) | 0x08000000);
        _procedure = (hwnd, message, wParam, lParam) =>
            message == 0x21 ? 3 : CallWindowProc(_previousProcedure, hwnd, message, wParam, lParam);
        _previousProcedure = SetWindowLongPtr(window, -4, Marshal.GetFunctionPointerForDelegate(_procedure));
    }

    private void RestoreProcedure()
    {
        if (_previousProcedure != 0 && TryGetPlatformHandle() is { } handle)
        {
            SetWindowLongPtr(handle.Handle, -4, _previousProcedure);
        }
        _previousProcedure = 0;
        _procedure = null;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern nint CallWindowProc(nint previous, nint window, uint message, nint wParam, nint lParam);

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}