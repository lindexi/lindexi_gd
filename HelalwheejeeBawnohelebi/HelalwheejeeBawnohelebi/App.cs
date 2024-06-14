using Microsoft.UI;
#if HAS_UNO
using Uno.UI.Xaml;
#endif

namespace HelalwheejeeBawnohelebi;

public class App : Application
{
    protected Window? MainWindow { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
        MainWindow = new Window();
#else
        MainWindow = Microsoft.UI.Xaml.Window.Current;
#endif

#if DEBUG
        MainWindow.EnableHotReload();
#endif

#if HAS_UNO
        // Do nothing in Skia.Gtk
        MainWindow.SetBackground(new SolidColorBrush(Colors.Green));
#endif

        MainWindow.Content = new TextBlock()
        {
            Text = "Hello Uno Platform",
            Foreground = new SolidColorBrush(Colors.Red)
        };

        // Ensure the current window is active
        MainWindow.Activate();

        var root = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(MainWindow.Content));
        // root is RootVisual
        ((Panel)root).Background = new SolidColorBrush(Colors.Transparent);
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }
}
