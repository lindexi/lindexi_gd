using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace CakawuhealehaneJairijelanefer;

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly DrawingGroup _iconDrawing;
    private readonly RelayCommand _exportCommand;
    private bool _isExporting;
    private string _status;

    internal MainWindowViewModel()
    {
        _iconDrawing = IconDrawingFactory.CreateDrawing();

        DrawingImage iconPreview = new(_iconDrawing);
        iconPreview.Freeze();
        IconPreview = iconPreview;

        OutputDirectory = Path.Join(AppContext.BaseDirectory, "Output");
        _status = UiStrings.ReadyStatus;
        _exportCommand = new RelayCommand(Export, () => !_isExporting);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageSource IconPreview { get; }

    public string OutputDirectory { get; }

    public string WindowTitle => UiStrings.WindowTitle;

    public string Heading => UiStrings.Heading;

    public string Description => UiStrings.Description;

    public string OutputDirectoryLabel => UiStrings.OutputDirectoryLabel;

    public string OutputFolderHint => UiStrings.OutputFolderHint;

    public string FormatsLabel => UiStrings.FormatsLabel;

    public string FormatsValue => UiStrings.FormatsValue;

    public string ExportButtonText => UiStrings.ExportButtonText;

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public ICommand ExportCommand => _exportCommand;

    private void Export()
    {
        _isExporting = true;
        _exportCommand.NotifyCanExecuteChanged();
        Status = UiStrings.ExportingStatus;

        try
        {
            IconExportResult result = IconExportService.Export(OutputDirectory, _iconDrawing);
            Status = UiStrings.FormatExportSucceeded(result.PngPaths.Count);
        }
        catch (IOException exception)
        {
            Status = UiStrings.FormatExportFailed(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Status = UiStrings.FormatExportFailed(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            Status = UiStrings.FormatExportFailed(exception.Message);
        }
        catch (NotSupportedException exception)
        {
            Status = UiStrings.FormatExportFailed(exception.Message);
        }
        finally
        {
            _isExporting = false;
            _exportCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
