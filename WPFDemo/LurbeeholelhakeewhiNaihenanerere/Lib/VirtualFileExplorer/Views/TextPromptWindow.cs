using System.Windows;

namespace VirtualFileExplorer.Views;

public partial class TextPromptWindow : Window
{
    public string PromptMessage { get; }

    public string InputText { get; set; }

    private TextPromptWindow(string title, string message, string defaultValue)
    {
        InitializeComponent();

        PromptMessage = message;
        InputText = defaultValue;
        Title = title;
        DataContext = this;

        Loaded += (_, _) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    public static string? ShowDialog(Window? owner, string title, string message, string defaultValue = "")
    {
        var dialog = new TextPromptWindow(title, message, defaultValue)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
