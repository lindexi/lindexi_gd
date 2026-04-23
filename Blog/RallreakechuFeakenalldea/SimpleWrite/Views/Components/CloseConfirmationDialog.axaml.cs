using Avalonia.Controls;
using Avalonia.Interactivity;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

public partial class CloseConfirmationDialog : UserControl
{
    public CloseConfirmationDialog()
    {
        InitializeComponent();
    }

    private SimpleWriteMainViewModel ViewModel => (SimpleWriteMainViewModel) DataContext!;

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

    private void OpenTempFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.OpenTempDirectory();
    }
}
