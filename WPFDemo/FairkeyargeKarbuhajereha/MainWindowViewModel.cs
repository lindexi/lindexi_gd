using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FairkeyargeKarbuhajereha;

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IconExportService _iconExportService = new();
    private readonly AsyncRelayCommand _exportCommand;
    private bool _isExporting;
    private string _statusText = "将生成 16×16、24×24、32×32、256×256 PNG 和多尺寸 ICO 文件。";

    public MainWindowViewModel()
    {
        _exportCommand = new AsyncRelayCommand(ExportAsync, () => !IsExporting);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ExportCommand => _exportCommand;

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (_isExporting == value)
            {
                return;
            }

            _isExporting = value;
            OnPropertyChanged();
            _exportCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    private async Task ExportAsync()
    {
        IsExporting = true;
        StatusText = "正在导出图标……";

        try
        {
            var drawingImage = (DrawingImage)Application.Current.FindResource("ApplicationIconDrawingImage");
            var outputDirectory = await _iconExportService.ExportAsync(drawingImage);
            StatusText = $"导出完成：{outputDirectory}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusText = $"导出失败：{exception.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
