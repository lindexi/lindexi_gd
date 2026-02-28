using Avalonia.Controls;
using Avalonia.Interactivity;
using SimpleWrite.Models;

namespace RallreakechuFeakenalldea.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        UpdateWindowTitle();

        var mainViewModel = MainView.ViewModel;
        mainViewModel.EditorViewModel.EditorModelChanged += EditorViewModel_EditorModelChanged;
    }

    private void EditorViewModel_EditorModelChanged(object? sender, System.EventArgs e)
    {
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var editorViewModel = MainView.ViewModel.EditorViewModel;
        EditorModel editorModel = editorViewModel.CurrentEditorModel;

        var titleText = editorModel.Title;
        if (editorModel.FileInfo is { } fileInfo)
        {
            titleText = fileInfo.FullName;
        }

        var productName = "SimpleWrite";

        Title = $"{titleText} - {productName}";
    }
}
