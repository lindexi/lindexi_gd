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
            app.Launched += (sender, e) =>
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
                        }
                    }
                };
                window.Activate();
            };
        });
    }
}