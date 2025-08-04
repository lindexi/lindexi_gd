namespace SimpleWrite.ViewModels;

public class SimpleWriteMainViewModel
{
    public StatusViewModel StatusViewModel { get; } = new StatusViewModel();
    public EditorViewModel EditorViewModel { get; } = new EditorViewModel();
}