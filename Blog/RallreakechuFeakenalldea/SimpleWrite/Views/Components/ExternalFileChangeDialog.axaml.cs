using Avalonia.Controls;
using Avalonia.Interactivity;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

public partial class ExternalFileChangeDialog : UserControl
{
    public ExternalFileChangeDialog()
    {
        InitializeComponent();
    }

    private SimpleWriteMainViewModel ViewModel =>
        (SimpleWriteMainViewModel) DataContext!;

    private void ReloadFromDiskButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ExternalFileChangeConfirmationViewModel.ReloadFromDisk();
    }

    private void IgnoreExternalFileChangeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ExternalFileChangeConfirmationViewModel.Ignore();
    }
}
