// See https://aka.ms/new-console-template for more information

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BeregemnallchunufeRojergalbu;

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
                    Title = "Create Window from console"
                };
                window.Content = new Grid()
                {
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = "ConsoleApplication",
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

public class App : Application
{
    public event EventHandler<LaunchActivatedEventArgs>? Launched;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Launched?.Invoke(this, args);
    }
}