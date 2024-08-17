using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace QarnahedajaceYawemcehem.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();


        Loaded += MainView_Loaded;
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await Task.Delay(100);

        var content = Content;
        var textBlock = (TextBlock)content!;

        var animation = new Animation()
        {
            Duration = TimeSpan.FromSeconds(10),
            IterationCount = new IterationCount(5),
            PlaybackDirection = PlaybackDirection.Alternate,
            Children =
            {
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 0),
                    },
                    KeyTime = TimeSpan.FromSeconds(0)
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 500),
                    },
                    KeyTime = TimeSpan.FromSeconds(10)
                }
            }
        };

        //textBlock.RenderTransform = new TranslateTransform();

        _ = animation.RunAsync(textBlock);
    }
}