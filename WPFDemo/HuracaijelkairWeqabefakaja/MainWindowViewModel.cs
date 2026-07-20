using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HuracaijelkairWeqabefakaja;

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DrawingGroup _artworkDrawing;
    private readonly IconExportService _iconExportService;
    private readonly AsyncRelayCommand _saveIconsCommand;
    private bool _isBusy;
    private string _statusMessage;
    private bool _hasError;

    internal MainWindowViewModel()
    {
        _artworkDrawing = OrbitArtworkRenderer.CreateDrawing();
        _iconExportService = new IconExportService();
        _statusMessage = GetStringResource("ReadyStatusText", "准备就绪");
        _saveIconsCommand = new AsyncRelayCommand(SaveIconsAsync, () => !IsBusy);

        PreviewSource = CreatePreviewSource(_artworkDrawing);
        OutputDirectory = IconExportService.DefaultOutputDirectory;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageSource PreviewSource { get; }

    public string OutputDirectory { get; }

    public ICommand SaveIconsCommand => _saveIconsCommand;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            _saveIconsCommand.NotifyCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    private async Task SaveIconsAsync()
    {
        IsBusy = true;
        HasError = false;
        StatusMessage = GetStringResource("SavingStatusText", "正在生成图标文件…");

        try
        {
            await _iconExportService.ExportAsync(_artworkDrawing, OutputDirectory);
            StatusMessage = GetStringResource("SavedStatusText", "图标已保存到输出文件夹。");
        }
        catch (System.IO.IOException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (ArgumentException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (NotSupportedException exception)
        {
            ShowExportError(exception.Message);
        }
        catch (OverflowException exception)
        {
            ShowExportError(exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowExportError(string errorMessage)
    {
        HasError = true;
        StatusMessage = $"{GetStringResource("SaveFailedStatusPrefix", "保存失败：")}{errorMessage}";
    }

    private static DrawingImage CreatePreviewSource(Drawing drawing)
    {
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static string GetStringResource(string key, string fallback)
    {
        return Application.Current.TryFindResource(key) as string ?? fallback;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
