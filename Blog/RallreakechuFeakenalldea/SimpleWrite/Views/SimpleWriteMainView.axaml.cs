using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views;

public partial class SimpleWriteMainView : UserControl
{
    public SimpleWriteMainView()
    {
        InitializeComponent();

        TextEditorInfo.SetTextEditorInfo(this, new TextEditorInfo(MainEditorView));
    }

    public SimpleWriteMainViewModel ViewModel => (SimpleWriteMainViewModel) DataContext!;

    public async Task OpenFileAsync(FileInfo file)
    {
        await ViewModel.OpenFileAsync(file);
    }

    private void SaveAndCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ConfirmCloseBySaving();
    }

    private void DiscardCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ConfirmCloseWithoutSaving();
    }

    private void ContinueEditingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.CancelCloseConfirmation();
    }
}