using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class WpfDispatcherTests
{
    [TestMethod]
    public async Task BackgroundDispatcher_InvokeAsync_UsesSharedStaThread()
    {
        var firstThreadId = 0;
        var secondThreadId = 0;
        var apartmentState = ApartmentState.Unknown;

        await Task.Run(() => WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
        {
            firstThreadId = Environment.CurrentManagedThreadId;
            apartmentState = Thread.CurrentThread.GetApartmentState();
            Assert.IsTrue(WpfDispatcher.BackgroundInstance.CheckAccess());
            return Task.CompletedTask;
        }));

        await Task.Run(() => WpfDispatcher.BackgroundInstance.InvokeAsync(() =>
        {
            secondThreadId = Environment.CurrentManagedThreadId;
            return Task.CompletedTask;
        }));

        Assert.AreNotEqual(Environment.CurrentManagedThreadId, firstThreadId);
        Assert.AreEqual(firstThreadId, secondThreadId);
        Assert.AreEqual(ApartmentState.STA, apartmentState);
    }

    [TestMethod]
    public async Task BackgroundDispatcher_CheckAccess_IsFalseOutsideDispatcherThread()
    {
        var checkAccess = await Task.Run(WpfDispatcher.BackgroundInstance.CheckAccess);

        Assert.IsFalse(checkAccess);
    }
}
