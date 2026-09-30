using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using SimpleWrite.Views.Components;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(SimpleWrite.Tests.SidebarTestApplication))]

namespace SimpleWrite.Tests;

public class SidebarTestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<SidebarTestApplication>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://SimpleWrite/"))
        {
            Source = new Uri("avares://SimpleWrite/Styles/MainStyles.axaml")
        });
    }
}

public class SidebarInteractionTests
{
    [AvaloniaTheory]
    [InlineData(200)]
    [InlineData(280)]
    public void CollapsedSidebarCanBeExpandedByPointer(double expandedWidth)
    {
        var sidebar = new SimpleWriteSideBar { Width = expandedWidth };
        // Disable animation to check the final layout deterministically, without sleeps.
        sidebar.Transitions = null;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(sidebar);
        var editor = new Border { Background = Avalonia.Media.Brushes.White };
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        var window = new Window { Width = 800, Height = 500, Content = grid };
        try
        {
            window.Show();
            window.UpdateLayout();
            var button = sidebar.FindControl<Button>("ToggleSidebarButton")!;
            Click(window, button);
            window.UpdateLayout();
            Assert.True(sidebar.Width < expandedWidth);

            Click(window, button);
            window.UpdateLayout();

            Assert.Equal(expandedWidth, sidebar.Width);
        }
        finally
        {
            window.Close();
        }
    }

    private static void Click(Window window, Button button)
    {
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("Button is not attached to the test window.");
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
}
