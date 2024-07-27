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
using static System.Runtime.CompilerServices.RuntimeHelpers;

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
        Opacity = 0.65;

        var file = @"C:\lindexi\Application\ApplicationConfiguration\屏幕信息展示\Configuration.coin";
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

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://finance.sina.com.cn/");

            var response = await httpClient.GetAsync($"https://hq.sinajs.cn/list={AppConfigurator.Of<DacemcalqeleHalibarbubemConfiguratioin>().TickName}");
            var stream = await response.Content.ReadAsStreamAsync();
            var streamReader = new StreamReader(stream, Encoding.GetEncoding("GBK"));
            var text = await streamReader.ReadToEndAsync();

            var match = Regex.Match(text,
                @"var hq_str_\w+=""(\w+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),([\d\.]+),");
            var current = match.Groups[4].Value;
            var max = match.Groups[5].Value;
            var min = match.Groups[6].Value;
            MessageTextBox.Text = $"{current} {max} {min}\r\n{DateTime.Now}";
        }
        catch
        {
            // 忽略
        }
        _ = TryHide();
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

    private ulong _hideVersion;
    private readonly FileConfigurationRepo _fileConfigurationRepo;
    public IAppConfigurator AppConfigurator { get; set; }

    private async Task TryHide()
    {
        _hideVersion++;
        var version = _hideVersion;
        await Task.Delay(TimeSpan.FromSeconds(10));
        if (version == _hideVersion)
        {
            Hide();
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        _ = TryHide();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        DragMove();
    }

    class DacemcalqeleHalibarbubemConfiguratioin : Configuration
    {
        public DacemcalqeleHalibarbubemConfiguratioin() : base("")
        {
        }

        public string TickName
        {
            get => GetString() ?? "sz000651";
        }
    }
}