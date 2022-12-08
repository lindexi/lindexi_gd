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

    public static (Window mainWindow, TextEditor textEditor) CreateTextEditorInNewWindow()
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
        return (mainWindow, textEditor);
    }
}