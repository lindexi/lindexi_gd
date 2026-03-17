using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

namespace AvaloniaAgentLib.View;

public partial class CopilotSlideBar : UserControl
{
    public CopilotSlideBar()
    {
        InitializeComponent();
    }

    public CopilotViewModel ViewModel => (CopilotViewModel)DataContext!;

    private async void SendButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        SendMessageAsync();
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        SendMessageAsync();
        e.Handled = true;
    }

    private async void SendMessageAsync()
    {
        try
        {
            var inputText = InputTextBox.Text?.Trim();
            await ViewModel.SendMessageAsync(inputText);
            InputTextBox.Text = null;
        }
        catch (Exception exception)
        {
            // 待处理
        }
    }

    private void ClearButton_OnClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.Clear();
    }
}