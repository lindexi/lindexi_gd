using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using LightTextEditorPlus.AvaloniaDemo.Views.Controls;

using System;

namespace LightTextEditorPlus.AvaloniaDemo;

public partial class ConsoleTextEditorTestView : UserControl
{
    public ConsoleTextEditorTestView()
    {
        InitializeComponent();

        Loaded += ConsoleTextEditorTestView_Loaded;
        DataContextChanged += ConsoleTextEditorTestView_DataContextChanged;
    }

    private void ConsoleTextEditorTestView_Loaded(object? sender, RoutedEventArgs e)
    {
        object? dataContext = DataContext;
        if (dataContext is not null)
        {
            DataContext = null;
        }
    }

    private void ConsoleTextEditorTestView_DataContextChanged(object? sender, EventArgs e)
    {
        DataContextChanged -= ConsoleTextEditorTestView_DataContextChanged;

        ConsoleTextEditorScrollViewer.Content = new ConsoleTextEditor()
        {
            SizeToContent = SizeToContent.Height
        };
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
}

public class ConsoleTextEditorTestViewModel
{
}