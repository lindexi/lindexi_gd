using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MSTest.Extensions.Contracts;

namespace dotnetCampus.UITest.WPF.Demo
{
    [TestClass]
    public class FooTest
    {
        [AssemblyInitialize]
        public static void InitializeApplication(TestContext testContext)
        {
            UITestManager.InitializeApplication(() => new App());
        }

        [UIContractTestCase]
        public void TestAsyncLoad()
        {
            "等待窗口显示出来，可以成功进行异步等待，不会锁主线程".Test(async () =>
            {
                var mainWindow = new MainWindow();
                var taskCompletionSource = new TaskCompletionSource();
                mainWindow.Loaded += (sender, args) => taskCompletionSource.SetResult();
                await mainWindow.Dispatcher.InvokeAsync(mainWindow.Show);
                await taskCompletionSource.Task;
            });
        }

        [UIContractTestCase]
        public void TestMainWindow()
        {
            "打开 MainWindow 窗口，可以成功打开窗口".Test(() =>
            {
                Assert.AreEqual(Application.Current.Dispatcher, Dispatcher.CurrentDispatcher);
                var mainWindow = new MainWindow();
                bool isMainWindowLoaded = false;
                mainWindow.Loaded += (sender, args) => isMainWindowLoaded = true;
                mainWindow.Show();
                Assert.AreEqual(true, isMainWindowLoaded);
            });

            "关闭 MainWindow 窗口，可以成功关闭窗口和收到窗口关闭事件".Test(() =>
            {
                var window = Application.Current.MainWindow;
                Assert.AreEqual(true, window is MainWindow);
                bool isMainWindowClosed = false;
                Assert.IsNotNull(window);
                window.Closed += (sender, args) => isMainWindowClosed = true;
                window.Close();
                Assert.AreEqual(true, isMainWindowClosed);
            });
        }
    }
}