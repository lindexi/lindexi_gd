using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;

using LightTextEditorPlus.AvaloniaDemo;
using LightTextEditorPlus.AvaloniaDemo.Desktop;
using LightTextEditorPlus.AvaloniaDemo.Views;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace LightTextEditorPlus.Avalonia.Tests;

[TestClass]
internal class TestFramework
{
    public static DirectoryInfo OutputDirectory { get; private set; } = null!;

    [AssemblyInitialize]
    public static void InitializeApplication(TestContext testContext)
    {
        OutputDirectory = Directory.CreateDirectory(Path.Join(testContext.TestRunDirectory, "Output"));

        ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        Thread thread = new Thread(() =>
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime([], lifetime =>
            {
                lifetime.Startup += (sender, args) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    manualResetEvent.Set();
                };
            });
        })
        {
            IsBackground = true,
            Name = "Avalonia UI Thread"
        };
        thread.Start();

        _thread = thread;

        manualResetEvent.WaitOne();
        manualResetEvent.Dispose();
    }

    public static TextEditTestContext CreateTextEditorInNewWindow()
    {
        return Dispatcher.UIThread.Invoke(() =>
        {
            TextEditorDebugView textEditorDebugView = new TextEditorDebugView(runDebug: false);
            var mainWindow = new Window()
            {
                Title = "文本库 UI 单元测试",
                Width = 1000,
                Height = 700,
                Content = textEditorDebugView
            };
            mainWindow.Show();

            mainWindow.AttachDevTools();

            return new TextEditTestContext(mainWindow, textEditorDebugView.TextEditor);
        });
    }

    private static Thread? _thread;

    /// <summary>
    /// 用于调试下辅助了解到当前是否符合预期，将暂停测试逻辑，可以重新进入此方法退出。仅 Debug 下有效
    /// </summary>
    /// <returns></returns>
    public static async Task FreezeTestToDebug()
    {
        if (IsDebug())
        {
            for (int i = 0; i < int.MaxValue; i++)
            {
                // 可以在这里打断点进行退出逻辑
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public static bool IsDebug()
    {
#if DEBUG
        return Debugger.IsAttached;
#else
        return false;
#endif
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}

public record TextEditTestContext(Window TestWindow, TextEditor TextEditor) : IDisposable
{
    public async Task DebugWaitWindowClose()
    {
#if DEBUG
        var taskCompletionSource = new TaskCompletionSource();
        TestWindow.Closed += (sender, args) =>
        {
            taskCompletionSource.SetResult();
        };

        while (TestFramework.IsDebug())
        {
            var task = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(1)));

            if (taskCompletionSource.Task.IsCompleted || ReferenceEquals(task, taskCompletionSource.Task))
            {
                return;
            }
        }
#else
        await Task.CompletedTask;
#endif
    }

    public void Dispose()
    {
        if (TestFramework.IsDebug())
        {
            // 如果在附加调试，那就先不退出了
            return;
        }

        TestWindow.Close();
    }
}

class TestApp : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
    }
}