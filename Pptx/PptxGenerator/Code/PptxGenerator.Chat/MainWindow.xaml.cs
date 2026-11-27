using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PptxGenerator.Chat.Services;
using PptxGenerator.Chat.ViewModels;

namespace PptxGenerator.Chat;

public partial class MainWindow : Window
{
    private bool _followTail = true;
    private ChatViewModel ViewModel => (ChatViewModel)DataContext;

    /// <summary>Initializes the independent chat window.</summary>
    public MainWindow() => InitializeComponent();

    private void FocusDraft(object sender, RoutedEventArgs e) => DraftBox.Focus();
    private void StopOnClose(object? sender, CancelEventArgs e)
    {
        if (!ViewModel.IsBusy) return;
        e.Cancel = true;
        ViewModel.PropertyChanged -= CloseAfterSave;
        ViewModel.PropertyChanged += CloseAfterSave;
        ViewModel.Stop();
    }

    private void CloseAfterSave(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChatViewModel.IsBusy) || ViewModel.IsBusy) return;
        ViewModel.PropertyChanged -= CloseAfterSave;
        Dispatcher.BeginInvoke(new Action(Close));
    }

    private async void ShowHistory(object sender, RoutedEventArgs e)
    {
        await ViewModel.ShowHistoryAsync();
        if (ViewModel.IsHistoryOpen) HistorySearchBox.Focus();
    }

    private async void OpenSession(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: SessionSummary summary }) return;
        await ViewModel.OpenSessionAsync(summary);
        if (ViewModel.IsChatOpen)
        {
            Transcript.ScrollToEnd();
            DraftBox.Focus();
        }
    }

    private void CloseHistory(object sender, RoutedEventArgs e)
    {
        ViewModel.CloseHistory();
        if (ViewModel.IsChatOpen) DraftBox.Focus();
    }

    private void NewSession(object sender, RoutedEventArgs e)
    {
        ViewModel.NewSession();
        DraftBox.Focus();
    }

    private void DraftKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.Control) return;
        e.Handled = true;
        if (ViewModel.SendCommand.CanExecute(null)) ViewModel.SendCommand.Execute(null);
    }

    private void FollowTranscript(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange == 0)
            _followTail = Transcript.ScrollableHeight - Transcript.VerticalOffset < 24;
        else if (_followTail)
            Transcript.ScrollToEnd();
    }

    private async void AttachImages(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = UiText.Get("Attach"), Filter = UiText.Get("ImageFilter"), Multiselect = true };
        try
        {
            if (dialog.ShowDialog(this) == true)
            {
                ViewModel.ComposerNotice = string.Empty;
                await ViewModel.AddImagesAsync(dialog.FileNames);
                DraftBox.Focus();
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or ExternalException)
        {
            ChatDiagnostics.Failure("PickImages", exception, "Dialog", nameof(OpenFileDialog));
            ViewModel.ComposerNotice = UiText.Get("ImagePickerFailed");
        }
    }

    private void RemoveImage(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ChatImage image }) ViewModel.RemoveImage(image);
    }

    private async void OpenToolImage(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: BitmapSource image }) return;

        string? path = null;
        try
        {
            path = Path.Combine(Path.GetTempPath(), $"PptxGenerator.Chat-{Guid.NewGuid():N}.png");
            await Task.Run(() =>
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                encoder.Save(stream);
            });
            using var viewer = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            ViewModel.ComposerNotice = string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            Win32Exception or InvalidOperationException or NotSupportedException or COMException)
        {
            ChatDiagnostics.Failure("OpenToolImage", exception, "ImagePath", path ?? "<temp>");
            ViewModel.ComposerNotice = UiText.Get("OpenToolImageFailed");
        }
    }

    private void CopyMessage(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: string { Length: > 0 } text }) return;
        try
        {
            Clipboard.SetText(text);
            ViewModel.ComposerNotice = UiText.Get("Copied");
        }
        catch (ExternalException exception)
        {
            ChatDiagnostics.Failure("Copy", exception, "Target", "Clipboard");
            ViewModel.ComposerNotice = UiText.Get("CopyFailed");
        }
    }

    private void ApplyMica(object? sender, EventArgs e)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemParameters.HighContrast) return;
        var source = (HwndSource)PresentationSource.FromVisual(this);
        const int systemBackdrop = 38;
        int mainWindow = 2;
        if (DwmSetWindowAttribute(source.Handle, systemBackdrop, ref mainWindow, sizeof(int)) == 0 &&
            DwmExtendFrameIntoClientArea(source.Handle, new Margins { Left = -1 }) == 0)
        {
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            Background = Brushes.Transparent;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr window, in Margins margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        internal int Left;
        internal int Right;
        internal int Top;
        internal int Bottom;
    }
}
