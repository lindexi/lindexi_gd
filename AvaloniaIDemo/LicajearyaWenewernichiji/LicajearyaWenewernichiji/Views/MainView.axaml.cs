using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;

namespace LicajearyaWenewernichiji.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var content = Content;
        var textBlock = (TextBlock) content!;

        var animation = new Animation()
        {
            Duration = TimeSpan.FromSeconds(10),
            IterationCount = IterationCount.Infinite,
            PlaybackDirection = PlaybackDirection.Alternate,
            Children =
            {
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 0d),
                    },
                    KeyTime = TimeSpan.FromSeconds(0)
                },
                new KeyFrame()
                {
                    Setters =
                    {
                        new Setter(TranslateTransform.XProperty, 500d),
                    },
                    KeyTime = TimeSpan.FromSeconds(10)
                }
            }
        };

        textBlock.RenderTransform = new TranslateTransform();

        _ = animation.RunAsync(textBlock);
    }
}
