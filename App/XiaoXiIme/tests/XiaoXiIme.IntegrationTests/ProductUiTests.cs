using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(CandidateWindowScreenshotCollection))]
public class ProductUiTests(CandidateWindowScreenshotFixture fixture)
{
    [Theory]
    [InlineData("SettingsTitle", "小希输入法设置")]
    [InlineData("Attribution", "词库数据及词库逻辑贡献：{0}")]
    [InlineData("Hint", "保存后下次启动输入服务生效，当前输入不受影响。")]
    public void WhenChineseResourcesAreLoadedThenProductTextIsAvailable(string key, string expected)
    {
        var resources = new ResourceManager("XiaoXiIme.ImeUi.Avalonia.ProductResources", typeof(App).Assembly);
        Assert.Equal(expected, resources.GetString(key));
    }

    [Theory(Timeout = 15_000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenDraftOrDefaultsAreCancelledThenNoSettingsArePersisted(bool defaults)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var options = new XiaoXiIme.ImeIpc.XiaoXiImeIpcOptions("ProductUi_" + Guid.NewGuid().ToString("N"));
        using var host = new XiaoXiIme.ImeHost.ImeHostService(options, productSettingsPath: path);
        host.Start();
        Window? settings = null;
        await fixture.Session.Dispatch<bool>(async () =>
        {
            var app = Assert.IsType<App>(Application.Current);
            using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) =>
            {
                if (window.Title == "小希输入法设置") settings = window;
            });
            using var session = app.OpenProductSettings(options);
            await Task.Delay(1600);
            var controls = global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(settings!).ToArray();
            controls.OfType<ComboBox>().Single().SelectedIndex = 1;
            var button = controls.OfType<Button>().Single(b => Equals(b.Content, defaults ? "恢复默认" : "取消"));
            button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            if (defaults)
                controls.OfType<Button>().Single(b => Equals(b.Content, "取消")).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            return true;
        }, CancellationToken.None);
        Assert.False(File.Exists(path));
    }

    [Fact(Timeout = 15_000)]
    public async Task WhenAppliedPreferenceIsFollowedByCancelledDraftThenAppliedPreferenceRemains()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var options = new XiaoXiIme.ImeIpc.XiaoXiImeIpcOptions("ProductApply_" + Guid.NewGuid().ToString("N"));
        using var host = new XiaoXiIme.ImeHost.ImeHostService(options, productSettingsPath: path);
        host.Start();
        await fixture.Session.Dispatch<bool>(async () =>
        {
            Window? settings = null;
            using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) =>
            {
                if (window.Title == "小希输入法设置") settings = window;
            });
            using var session = Assert.IsType<App>(Application.Current).OpenProductSettings(options);
            await Task.Delay(1600);
            var controls = global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(settings!).ToArray();
            global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            await Task.Delay(600);
            var visible = controls.OfType<CheckBox>().Single();
            visible.IsChecked = false;
            controls.OfType<Button>().Single(b => Equals(b.Content, "应用")).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            using var savedTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while ((await host.GetProductSettingsAsync()).Settings.Revision == 0)
                await Task.Delay(20, savedTimeout.Token);
            while (!controls.OfType<Button>().Single(b => Equals(b.Content, "确定")).IsEnabled)
                await Task.Delay(20, savedTimeout.Token);
            visible.IsChecked = true;
            controls.OfType<Button>().Single(b => Equals(b.Content, "取消")).RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            return true;
        }, CancellationToken.None);
        Assert.Equal(new XiaoXiIme.Foundation.ImeProductSettings(Revision: 1, StatusBarVisible: false), (await host.GetProductSettingsAsync()).Settings);
    }

    [Fact(Timeout = 15_000)]
    public async Task WhenRefreshCompletesAfterDisposalThenProductWindowsStayClosed()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-tests", Guid.NewGuid().ToString("N"), "settings.json");
        var options = new XiaoXiIme.ImeIpc.XiaoXiImeIpcOptions("ProductDisposeRefresh_" + Guid.NewGuid().ToString("N"));
        using var host = new XiaoXiIme.ImeHost.ImeHostService(options, productSettingsPath: path);
        host.Start();
        await fixture.Session.Dispatch<bool>(async () =>
        {
            var windows = new List<Window>();
            using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) => windows.Add(window));
            var session = Assert.IsType<App>(Application.Current).OpenProductSettings(options);
            await Task.Delay(1000);
            session.Dispose();
            await Task.Delay(1500);
            Assert.All(windows, window => Assert.False(window.IsVisible));
            return true;
        }, CancellationToken.None);
    }

    [Fact(Timeout = 15_000)]
    public async Task WhenProductSessionIsDisposedThenAllItsWindowsClose()
    {
        await fixture.Session.Dispatch(() =>
        {
            var windows = new List<Window>();
            using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) => windows.Add(window));
            var app = Assert.IsType<App>(Application.Current);
            app.OpenProductSettings(new("ProductUiDispose_" + Guid.NewGuid().ToString("N"))).Dispose();
            Assert.All(windows, window => Assert.False(window.IsVisible));
        }, CancellationToken.None);
    }

    [Fact(Timeout = 15_000)]
    public async Task WhenSettingsEntryStartsWithoutHostThenInteractiveSettingsAreRendered()
    {
        await fixture.Session.Dispatch(() =>
        {
            var app = Assert.IsType<App>(Application.Current);
            Window? settings = null;
            using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) =>
            {
                if (window.Title == "小希输入法设置") settings = window;
            });
            using var session = app.OpenProductSettings(new("ProductUiHeadless_" + Guid.NewGuid().ToString("N")));
            Assert.NotNull(settings);
            using var frame = settings.CaptureRenderedFrame();
            Assert.NotNull(frame);
        }, CancellationToken.None);
    }
}
