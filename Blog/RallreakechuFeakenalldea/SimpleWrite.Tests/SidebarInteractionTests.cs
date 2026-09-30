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
    public void OverlayButtonReopensWithoutReservingAButtonBar(double expandedWidth)
    {
        var sidebar = new SimpleWriteSideBar { Width = expandedWidth, Transitions = null };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(sidebar);
        var editor = new Border { Background = Avalonia.Media.Brushes.White };
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        var editorClicks = 0;
        editor.PointerPressed += (_, _) => editorClicks++;
        var window = new Window { Width = 800, Height = 500, Content = grid };
        try
        {
            window.Show();
            window.UpdateLayout();
            var button = sidebar.FindControl<Button>("ToggleSidebarButton")
                ?? throw new InvalidOperationException("Toggle button missing.");
            for (var i = 0; i < 3; i++)
            {
                Click(window, button);
                window.UpdateLayout();
                Assert.Equal(2, editor.Bounds.X);
                var origin = button.TranslatePoint(default, window)!.Value;
                Assert.InRange(origin.X, 4.5, 5.5);
                Assert.Equal(32, button.Bounds.Width);
                // Outside the floating button, the overlay must not intercept editor input.
                window.MouseDown(new Point(20, 30), MouseButton.Left);
                window.MouseUp(new Point(20, 30), MouseButton.Left);
                Assert.Equal(i + 1, editorClicks);
                Click(window, button);
                window.UpdateLayout();
                Assert.Equal(expandedWidth, sidebar.Width);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void Click(Window window, Button button)
    {
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("Button is detached.");
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }
}
