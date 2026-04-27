using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaApp;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        TextBlock1.Text += DateTime.Now.ToUniversalTime().Add(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss");
    }
}