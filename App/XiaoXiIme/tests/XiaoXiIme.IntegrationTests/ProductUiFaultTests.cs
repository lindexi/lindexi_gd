using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(CandidateWindowScreenshotCollection))]
public class ProductUiFaultTests(CandidateWindowScreenshotFixture fixture)
{
    [Theory(Timeout = 30_000)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenSaveResponseIsLostThenReplayDoesNotOverwriteDraftOrLaterAuthority(bool commit)
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-faults", Guid.NewGuid().ToString("N"), "settings.json");
        var root = Path.GetDirectoryName(path)!;
        XiaoXiIme.Dictionary.DictionaryPackageCompiler.Compile(
            new[] { new XiaoXiIme.Dictionary.PhoneticDictionaryEntry("你好", "ni hao", 200) },
            Path.Combine(root, XiaoXiIme.Dictionary.DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath),
            new XiaoXiIme.Dictionary.DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        using var host = new ImeHostService(new("Authority_" + Guid.NewGuid().ToString("N")), hostBaseDirectory: root, productSettingsPath: path);
        var options = new XiaoXiImeIpcOptions("FaultRelay_" + Guid.NewGuid().ToString("N"));
        var received = new TaskCompletionSource<ImeSaveProductSettingsRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var relay = new XiaoXiImeIpcServer(host.ProcessKeyAsync, host.GetSnapshotAsync,
            getHostStatusAsync: host.GetHostStatusAsync, options: options,
            getProductSettingsAsync: host.GetProductSettingsAsync,
            saveProductSettingsAsync: async request =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    var response = commit ? await host.SaveProductSettingsAsync(request) : null;
                    received.SetResult(request);
                    await release.Task;
                    return response ?? await host.SaveProductSettingsAsync(request);
                }
                return await host.SaveProductSettingsAsync(request);
            });
        relay.Start();
        try
        {
            await fixture.Session.Dispatch<bool>(async () =>
            {
                Window? window = null;
                using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((w, _) =>
                {
                    if (w.Title == "小希输入法设置") window = w;
                });
                using var session = Assert.IsType<App>(Application.Current).OpenProductSettings(options);
                await Task.Delay(1600);
                var controls = window!.GetLogicalDescendants().ToArray();
                var scheme = controls.OfType<ComboBox>().Single();
                var visible = controls.OfType<CheckBox>().Single(c => c.Name == "StatusBarVisible");
                scheme.SelectedItem = scheme.Items.OfType<ComboBoxItem>().Single(item => item.Name == "SchemeDouble");
                var apply = controls.OfType<Button>().Single(b => Equals(b.Content, "应用"));
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
                // The transport withholds only the response; authority and UI remain real.
                var authority = await host.GetProductSettingsAsync();
                await host.SaveProductSettingsAsync(new(authority.HostInstanceId, Guid.NewGuid(), authority.Settings.Revision,
                    authority.Settings with { StatusBarVisible = false }));
                await Task.Delay(2400);
                Assert.Equal("SchemeDouble", Assert.IsType<ComboBoxItem>(scheme.SelectedItem).Name);
                Assert.True(visible.IsChecked);
                Assert.False(apply.IsEnabled);
                var latest = await host.GetProductSettingsAsync();
                Assert.Equal(commit ? 2 : 1, latest.Settings.Revision);
                Assert.Equal(commit ? ImeProductInputScheme.XiaoheDoublePinyin : ImeProductInputScheme.FullPinyin, latest.Settings.InputScheme);
                Assert.False(latest.Settings.StatusBarVisible);
                release.TrySetResult();
                await Task.Delay(1200);
                Assert.Equal("SchemeDouble", Assert.IsType<ComboBoxItem>(scheme.SelectedItem).Name);
                Assert.True(visible.IsChecked);
                Assert.Equal(latest.Settings, (await host.GetProductSettingsAsync()).Settings);
                controls.OfType<Button>().Single(b => Equals(b.Content, "取消")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                return true;
            }, CancellationToken.None);
        }
        finally { release.TrySetResult(); }
    }

    [Fact(Timeout = 30_000)]
    public async Task WhenLateReadReturnsAfterReopeningThenNewDraftRemainsUnchanged()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme-product-faults", Guid.NewGuid().ToString("N"), "settings.json");
        using var host = new ImeHostService(new("Authority_" + Guid.NewGuid().ToString("N")), productSettingsPath: path);
        var options = new XiaoXiImeIpcOptions("ReadRelay_" + Guid.NewGuid().ToString("N"), RequestTimeout: TimeSpan.FromSeconds(5));
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var delay = 0;
        using var relay = new XiaoXiImeIpcServer(host.ProcessKeyAsync, host.GetSnapshotAsync,
            getHostStatusAsync: host.GetHostStatusAsync, options: options,
            saveProductSettingsAsync: host.SaveProductSettingsAsync,
            getProductSettingsAsync: async () =>
            {
                var state = await host.GetProductSettingsAsync();
                if (Interlocked.Exchange(ref delay, 0) == 1)
                {
                    blocked.TrySetResult();
                    await release.Task;
                }
                return state;
            });
        relay.Start();
        try
        {
            await fixture.Session.Dispatch<bool>(async () =>
            {
                Window? window = null;
                using var opened = Window.WindowOpenedEvent.AddClassHandler<Window>((w, _) =>
                {
                    if (w.Title == "小希输入法设置") window = w;
                });
                var app = Assert.IsType<App>(Application.Current);
                using var session = app.OpenProductSettings(options);
                await Task.Delay(1600);
                Interlocked.Exchange(ref delay, 1);
                await blocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
                window!.Close();
                using var reopened = app.OpenProductSettings(options);
                await Task.Delay(1600);
                var scheme = window!.GetLogicalDescendants().OfType<ComboBox>().Single();
                scheme.SelectedItem = scheme.Items.OfType<ComboBoxItem>().Single(item => item.Name == "SchemeDouble");
                release.TrySetResult();
                await Task.Delay(1200);
                Assert.Equal("SchemeDouble", Assert.IsType<ComboBoxItem>(scheme.SelectedItem).Name);
                Assert.Equal(new ImeProductSettings(), (await host.GetProductSettingsAsync()).Settings);
                return true;
            }, CancellationToken.None);
        }
        finally { release.TrySetResult(); }
    }
}
