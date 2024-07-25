using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Windows.Win32;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using System.Text.RegularExpressions;

namespace DacemcalqeleHalibarbubem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        KeyboardHookListener.KeyDown += KeyboardHookListener_KeyDown;
        KeyboardHookListener.KeyUp += KeyboardHookListener_KeyUp;
        KeyboardHookListener.HookKeyboard();

        Loaded += MainWindow_Loaded;

        AllowsTransparency = true;
        Opacity = 0.7;

        var file = @"C:\lindexi\Application\DacemcalqeleHalibarbubem\Configuration.coin";
        var fileConfigurationRepo = ConfigurationFactory.FromFile(file);
        _fileConfigurationRepo = fileConfigurationRepo;
        var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
        AppConfigurator = appConfigurator;

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _ = _fileConfigurationRepo.SaveAsync();
    }

    private readonly Stopwatch _lastCtrlKeyDown = new Stopwatch();

    private void KeyboardHookListener_KeyDown(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LWin)
        {
            ShowNotActive();
        }
    }

    private async void ShowNotActive()
    {
        Show();
        Topmost = true;
        Topmost = false;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://finance.sina.com.cn/");

        var response = await httpClient.GetAsync("https://hq.sinajs.cn/list=sz000651");
        var stream = await response.Content.ReadAsStreamAsync();
        var streamReader = new StreamReader(stream, Encoding.GetEncoding("GBK"));
        var text = await streamReader.ReadToEndAsync();

        // 数据依次是“股票名称、今日开盘价、昨日收盘价、当前价格、今日最高价、今日最低价、竞买价、竞卖价、成交股数、成交金额、买1手、买1报价、买2手、买2报价、…、买5报价、…、卖5报价、日期、时间”。
        // var hq_str_sz000651="格力电器,38.010,38.020,37.700,38.240,37.200,37.700,37.710,39027322,1466749485.590,46000,37.700,6900,37.690,29000,37.680,2900,37.670,11900,37.660,3200,37.710,12500,37.720,8300,37.730,3800,37.740,1700,37.750,2024-07-05,15:00:00,00";
        var match = Regex.Match(text, @"var hq_str_sz000651=""(\w+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),");
        var current = match.Groups[4].Value;
        var max = match.Groups[5].Value;
        var min = match.Groups[6].Value;
        MessageTextBox.Text = $"{current} {max} {min}\r\n{DateTime.Now}";

        _isHiding = true;
        await Task.Delay(TimeSpan.FromSeconds(10));
        if (_isHiding)
        {
            Hide();
        }
    }

    private void KeyboardHookListener_KeyUp(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LeftCtrl)
        {
            if (_lastCtrlKeyDown.IsRunning && _lastCtrlKeyDown.Elapsed < TimeSpan.FromMilliseconds(500))
            {
                ShowNotActive();
            }
            else
            {
                _lastCtrlKeyDown.Restart();
            }
        }
    }

    private bool _isHiding = false;
    private FileConfigurationRepo _fileConfigurationRepo;
    public IAppConfigurator AppConfigurator { get; set; }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        _isHiding = false;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        DragMove();
    }
}