using Avalonia;
using Avalonia.Headless;
using Ava = Avalonia.Controls;
using Wpf = System.Windows.Controls;

[assembly: DoNotParallelize]

namespace ToolTemplateDictionaryDemo.Tests;

[TestClass]
public sealed class ToolTemplateDictionaryTests
{
    [TestMethod]
    public async Task AvaloniaCompiledXamlRegistersAndBuildsKeyedTemplateAsync()
    {
        await using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessApp));
        var text = await session.Dispatch(() =>
        {
            var probe = new AvaloniaProbe();
            var selector = (AvaloniaSelector)probe.DataTemplates.Single();
            var item = new ToolItem("ReadFileLines", "Avalonia tool output");
            var content = (Ava.TextBlock)selector.Build(item)!;
            content.DataContext = item;
            return content.Text;
        }, CancellationToken.None);

        Assert.AreEqual("Avalonia tool output", text);
    }

    [STATestMethod]
    public void WpfCompiledXamlRegistersAndBuildsKeyedTemplate()
    {
        var probe = new WpfProbe();
        var selector = (WpfSelector)probe.Resources["MessageParts"];
        var item = new ToolItem("ReadFileLines", "WPF tool output");

        var presenter = new Wpf.ContentPresenter { Content = item, ContentTemplateSelector = selector };
        probe.Content = presenter;
        probe.Measure(new System.Windows.Size(400, 200));
        probe.Arrange(new System.Windows.Rect(0, 0, 400, 200));
        var content = (Wpf.TextBlock)System.Windows.Media.VisualTreeHelper.GetChild(presenter, 0);
        content.GetBindingExpression(Wpf.TextBlock.TextProperty)!.UpdateTarget();

        Assert.AreEqual("WPF tool output", content.Text);
    }

    public sealed class HeadlessApp : Application
    {
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
