using System.Windows;
using System.Windows.Controls;

namespace VirtualFileExplorer.Views;

internal sealed class TextPromptWindow : Window
{
    private readonly TextBox _textBox;

    private TextPromptWindow(string title, string message, string defaultValue)
    {
        Title = title;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        MinWidth = 360;

        var panel = new Grid
        {
            Margin = new Thickness(16)
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(messageBlock, 0);
        panel.Children.Add(messageBlock);

        _textBox = new TextBox
        {
            MinWidth = 320,
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(_textBox, 1);
        panel.Children.Add(_textBox);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "确定",
            IsDefault = true,
            MinWidth = 80,
            Margin = new Thickness(0, 0, 8, 0)
        };
        okButton.Click += (_, _) => DialogResult = true;
        buttonsPanel.Children.Add(okButton);

        var cancelButton = new Button
        {
            Content = "取消",
            IsCancel = true,
            MinWidth = 80
        };
        buttonsPanel.Children.Add(cancelButton);

        Grid.SetRow(buttonsPanel, 2);
        panel.Children.Add(buttonsPanel);

        Content = panel;

        Loaded += (_, _) =>
        {
            _textBox.Focus();
            _textBox.SelectAll();
        };
    }

    public static string? ShowDialog(Window? owner, string title, string message, string defaultValue = "")
    {
        var dialog = new TextPromptWindow(title, message, defaultValue)
        {
            Owner = owner
        };

        return dialog.ShowDialog() == true ? dialog._textBox.Text : null;
    }
}
