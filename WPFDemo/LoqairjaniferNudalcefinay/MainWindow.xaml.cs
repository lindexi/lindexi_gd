using System.Text;
using System.Threading.Channels;
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

namespace LoqairjaniferNudalcefinay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.StylusPlugIns.Add(new MainWindowStylusPlugIn(this));

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            // 一定要求调度线程，因为一边是触摸、一边是UI线程，两个都不能卡
            AllowSynchronousContinuations = false,
            // 单写读性能更好
            SingleWriter = true,
            SingleReader = true,
        });
        _channel = channel;

        _ = ReadAllAsync();
    }

    private async Task ReadAllAsync()
    {
        while (true)
        {
            var canRead = await _channel.Reader.WaitToReadAsync();
            if(!canRead)
            {
                break;
            }

            var lastMessage = string.Empty;
            while (_channel.Reader.TryRead(out var message))
            {
                lastMessage = message;
            }

            if (!string.IsNullOrEmpty(lastMessage))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    LogTextBlock.Text = lastMessage;
                }, DispatcherPriority.Background);
            }
        }
    }

    private readonly Channel<string> _channel;

    public void LogMessage(string message)
    {
        _channel.Writer.TryWrite(message);
    }
}

record TouchInfo
{

}