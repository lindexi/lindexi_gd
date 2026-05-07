namespace JawjeleceeYairlubelhearrene;

internal sealed class SlidePreviewViewModel : ObservableObject
{
    private string _generatedScript = string.Empty;

    public SlidePreviewViewModel(int slideNumber, string slideText, string imageFilePath)
    {
        SlideNumber = slideNumber;
        SlideText = slideText;
        ImageFilePath = imageFilePath;
    }

    public int SlideNumber { get; }

    public string SlideText { get; }

    public string ImageFilePath { get; }

    public string GeneratedScript
    {
        get => _generatedScript;
        set => SetProperty(ref _generatedScript, value);
    }
}
