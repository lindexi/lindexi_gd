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
using dotnetCampus.Threading;

namespace NedairkaweeBiheefallbahejay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ExecuteOnceAwaiter<FooInfo> ExecuteOnceAwaiter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            var executeOnceAwaiter = new ExecuteOnceAwaiter<FooInfo>(AsyncAction);
            ExecuteOnceAwaiter = executeOnceAwaiter;
        }

        private async Task<FooInfo> AsyncAction()
        {
            for (int i = 0; i < 10; i++)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    LogTextBlock.Text += $"执行任务 {i+1}/10\r\n";
                });

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
         
            return new FooInfo();
        }

        private void StartTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            LogTextBlock.Text += $"点击启动任务按钮\r\n";

            ExecuteOnceAwaiter.ExecuteAsync();
        }

        private void ResetTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            LogTextBlock.Text += $"点击重置任务按钮\r\n";

            ExecuteOnceAwaiter.ResetWhileCompleted();
        }
    }

    public class FooInfo
    {

    }
}
