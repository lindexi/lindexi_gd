using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dotnetCampus.UITest.WPF;
using CSharpMarkup.Wpf;
using static CSharpMarkup.Wpf.Helpers;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class TestFramework
{
    [AssemblyInitialize]
    public static void InitializeApplication(TestContext testContext)
    {
        UITestManager.InitializeApplication(() => new Application());
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