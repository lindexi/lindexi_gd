using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;

namespace KarhelearkuDemkunalhaw.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += MainView_Loaded;
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        var mainView = this;

        var renderTargetBitmap =
            new RenderTargetBitmap(new PixelSize((int) mainView.Bounds.Width, (int) mainView.Bounds.Height), new Vector(96, 96));
        renderTargetBitmap.Render(mainView);

        var file = Path.Join(AppContext.BaseDirectory, "1.png");
        renderTargetBitmap.Save(file, 100);
    }
}