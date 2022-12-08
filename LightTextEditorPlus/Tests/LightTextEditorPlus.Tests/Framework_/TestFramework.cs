using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dotnetCampus.UITest.WPF;
using CSharpMarkup.Wpf;
using static CSharpMarkup.Wpf.Helpers;
using Application = System.Windows.Application;
using Window = System.Windows.Window;
using static System.Net.Mime.MediaTypeNames;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TestFramework
{
    private static Application _application;

    [AssemblyInitialize]
    public static void InitializeApplication(TestContext testContext)
    {
        UITestManager.InitializeApplication(() =>
        {
            _application = new Application()
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            return _application;
        });
    }

    [AssemblyCleanup]
    public static void CleanApplication()
    {
        _application.Dispatcher.InvokeAsync(() =>
        {
            _application.Shutdown();
        });
    }

    public static TextEditTestContext CreateTextEditorInNewWindow()
    {
        var mainWindow = new Window()
        {
            Width = 1000,
            Height = 700,
            Content = Border
            (
                BorderThickness: Thickness(1),
                BorderBrush: Brushes.Blue,
                Child: Grid
                (
                    new TextEditor()
                    {
                        Width = 600,
                        Height = 600,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    }.Out(out var textEditor)
                )
            ).Margin(10).UI
        };

        mainWindow.Show();
        return new TextEditTestContext(mainWindow, textEditor);
    }

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
}