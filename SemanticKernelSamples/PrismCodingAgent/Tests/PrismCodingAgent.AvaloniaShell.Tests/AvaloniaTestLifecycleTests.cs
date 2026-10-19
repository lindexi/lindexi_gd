using Avalonia.Controls;
using Avalonia.Threading;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class AvaloniaTestLifecycleTests
{
    private int _initializationThread;

    [TestInitialize]
    public void RememberUiThread()
    {
        Dispatcher.UIThread.VerifyAccess();
        _initializationThread = Environment.CurrentManagedThreadId;
    }

    [TestCleanup]
    public Task VerifyCleanupThreadAsync() => Dispatcher.UIThread.InvokeAsync(async () =>
    {
        await Task.Yield();
        Dispatcher.UIThread.VerifyAccess();
        Assert.AreEqual(_initializationThread, Environment.CurrentManagedThreadId);
    });

    [TestMethod]
    public void WhenTestIsSynchronousThenItRunsOnInitializationThread()
    {
        Dispatcher.UIThread.VerifyAccess();
        Assert.AreEqual(_initializationThread, Environment.CurrentManagedThreadId);
    }

    [TestMethod]
    public async Task WhenTestAwaitsThenUiAccessIsPreserved()
    {
        await Task.Yield();
        Dispatcher.UIThread.VerifyAccess();
        var window = new Window { Content = new TextBlock { Text = "ready" } };
        window.Show();
        try
        {
            Assert.AreEqual(_initializationThread, Environment.CurrentManagedThreadId);
        }
        finally
        {
            window.Close();
        }
    }
}
