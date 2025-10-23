using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using LightTextEditorPlus.AvaloniaDemo.Views.Controls;

using System;
using System.IO;
using System.Threading.Tasks;

namespace LightTextEditorPlus.AvaloniaDemo;

public partial class ConsoleTextEditorTestView : UserControl
{
    public ConsoleTextEditorTestView()
    {
        InitializeComponent();

        ConsoleTextEditorScrollViewer.Content = new ConsoleTextEditor()
        {
            SizeToContent = SizeToContent.Height
        };
    }

    private void AddConsoleTextEditorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ConsoleTextEditorBorder.IsVisible = true;
    }

    private async void AddTextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ConsoleTextEditorScrollViewer.Content is ConsoleTextEditor textEditor)
        {
            for (int i = 0; i < 500; i++)
            {
                textEditor.AppendText($"[{textEditor.ParagraphList.Count}] 123123123 {Random.Shared.Next(10000)}\n");
            }

            await Task.Delay(100);
            ConsoleTextEditorScrollViewer.ScrollToEnd();
        }
    }

    private void RemoveConsoleTextEditorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ConsoleTextEditorScrollViewer.Content = null;
    }

    private void RaiseGarbageCollectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    private void SaveTextToFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var text = (ConsoleTextEditorScrollViewer.Content as TextEditor)?.Text;
        if (!string.IsNullOrEmpty(text))
        {
            File.WriteAllText("Text.txt", text);
        }
    }
}