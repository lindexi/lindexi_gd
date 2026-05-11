using JawjeleceeYairlubelhearrene.Infrastructure;

namespace JawjeleceeYairlubelhearrene.ViewModels;

internal sealed class SlidePreviewViewModel : ObservableObject
{
    public SlidePreviewViewModel(int slideNumber, string slideText, string originalImageFilePath, string imageFilePath)
    {
        SlideNumber = slideNumber;
        SlideText = slideText;
        OriginalImageFilePath = originalImageFilePath;
        ImageFilePath = imageFilePath;
    }


    public int SlideNumber { get; }

    public string SlideText { get; }

    public string OriginalImageFilePath { get; }

    public string ImageFilePath
    {
        get => _imageFilePath;
        set => SetProperty(ref _imageFilePath, value);
    }

    public string GeneratedScript
    {
        get => _generatedScript;
        set => SetProperty(ref _generatedScript, value);
    }

    private string _generatedScript = string.Empty;

    private string _imageFilePath;
}
