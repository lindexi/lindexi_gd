using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GifellichelNurcikaifallhane;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Dispatcher.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            TextBlock.Text += $"Dispatcher UnhandledException {args.Exception.Message}\r\n";
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                TextBlock.Text += $"TaskScheduler UnobservedTaskException {args.Exception.InnerException!.Message}\r\n";
            });
        };

        Task.Run(async () =>
        {
            while (true)
            {
                // 不断 GC 方便 Task 清理
                await Task.Delay(TimeSpan.FromSeconds(1));
                GC.Collect();
            }
        });
    }

    private void InvokeAsyncButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeAsync(() => throw new Exception($"在 Dispatcher.InvokeAsync 抛出异常"));
    }

    private void BeginInvokeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => throw new Exception($"在 Dispatcher.BeginInvoke 抛出异常")));
    }
}
