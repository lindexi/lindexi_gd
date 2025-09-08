using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
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

        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
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
                    StylusPlugInLogTextBlock.Text = lastMessage;
                }, DispatcherPriority.Background);
            }
        }
    }

    private readonly Channel<string> _channel;

    public void LogStylusPlugInMessage(string message)
    {
        _channel.Writer.TryWrite(message);
    }

    private string? _currentWarnMessage;

    protected override void OnStylusDown(StylusDownEventArgs e)
    {
        var id = e.StylusDevice.Id;
        if (_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"重复 Down Id={id}";
        }
        _dictionary[id] = new TouchInfo(e);

        LogMessage();
        base.OnStylusDown(e);
    }

    protected override void OnStylusMove(StylusEventArgs e)
    {
        var id = e.StylusDevice.Id;
        var currentTouchInfo = new TouchInfo(e);
        if (!_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"未找到 Move Id={id}";

            _dictionary[id] = currentTouchInfo;
        }
        else
        {
            _dictionary[id] = new TouchInfo(e);
        }
        LogMessage();

        base.OnStylusMove(e);
    }

    protected override void OnStylusUp(StylusEventArgs e)
    {
        var id = e.StylusDevice.Id;
        if (!_dictionary.TryGetValue(id, out var info))
        {
            _currentWarnMessage = $"未找到 Up Id={id}";
        }
        else
        {
            _dictionary.Remove(id);
        }
        LogMessage();

        base.OnStylusUp(e);
    }

    private readonly Dictionary<int, TouchInfo> _dictionary = [];

    private void LogMessage()
    {
        var message = "主线程触摸信息" + Environment.NewLine;
        if (!string.IsNullOrEmpty(_currentWarnMessage))
        {
            message += $"Warning: {_currentWarnMessage}" + Environment.NewLine;
        }
        message += $"Touch Count: {_dictionary.Count}" + Environment.NewLine;
        foreach (var item in _dictionary.Values)
        {
            message += $"Id={item.Id}, X={item.X:0.000}, Y={item.Y:0.000}" + Environment.NewLine;
        }

        MainLogTextBlock.Text = message;
    }
}