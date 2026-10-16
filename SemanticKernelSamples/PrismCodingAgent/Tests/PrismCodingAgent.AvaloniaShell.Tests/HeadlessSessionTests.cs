using Avalonia.Controls;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class HeadlessSessionTests
{
    [TestMethod]
    [Timeout(15000)]
    public async Task WhenSessionDispatchesThenUiControlsCanBeCreated()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        string? text = await AvaloniaTestClassAttribute.Session.Dispatch(
            () => new TextBlock { Text = "ready" }.Text, cancellation.Token);
        Assert.AreEqual("ready", text);
    }
}
