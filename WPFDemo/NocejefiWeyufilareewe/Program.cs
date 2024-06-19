using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace NocejefiWeyufilareewe;

public class App : Application
{
    public event EventHandler<LaunchActivatedEventArgs>? Launched;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Launched?.Invoke(this, args);
    }
}

internal class Program
{
    static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var app = new App();
            app.Launched += (_, e) =>
            {
                var window = new Window()
                {
                    Title = "控制台创建应用"
                };
                window.Content = new Grid()
                {
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "控制台应用",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new Button()
                        {
                            Content = "点击",
                            Margin = new Thickness(0, 100, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        }.Do(button => button.Click += (s, _) =>
                        {
                            // System.InvalidCastException:“Specified cast is not valid.”
                            // https://stackoverflow.com/questions/73936140/how-to-get-the-window-hosting-a-uielement-instance
                            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(s);
                        })
                    }
                };
                window.Activate();
            };
        });
    }
}

static class CsharpMarkup
{
    public static T Do<T>(this T element, Action<T> action) where T : UIElement
    {
        action(element);

        return element;
    }
}