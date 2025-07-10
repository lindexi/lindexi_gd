using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace KarhelearkuDemkunalhaw.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void TakeSnapshotButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var mainView = this;

        var originMode = RenderOptions.GetTextRenderingMode(mainView);
        RenderOptions.SetTextRenderingMode(mainView, TextRenderingMode.Antialias);
        var renderTargetBitmap =
            new RenderTargetBitmap(new PixelSize((int) mainView.Bounds.Width, (int) mainView.Bounds.Height), new Vector(96, 96));
        renderTargetBitmap.Render(mainView);
        RenderOptions.SetTextRenderingMode(mainView, originMode);

        var file = Path.Join(AppContext.BaseDirectory, "1.png");
        renderTargetBitmap.Save(file, 100);
    }

    private void TakeSnapshotWithFixButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RootGrid.Background = Brushes.White;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var mainView = this;

            var renderTargetBitmap =
                new RenderTargetBitmap(new PixelSize((int) mainView.Bounds.Width, (int) mainView.Bounds.Height), new Vector(96, 96));
            renderTargetBitmap.Render(mainView);

            var file = Path.Join(AppContext.BaseDirectory, "2.png");
            renderTargetBitmap.Save(file, 100);

            RootGrid.Background = Brushes.Transparent;
        });
    }
}