using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;

using NarjejerechowainoBuwurjofear.Inking;

using UnoInk.Inking.InkCore;

namespace NarjejerechowainoBuwurjofear.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;

        RootGrid.PointerPressed += RootGrid_PointerPressed;
        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.PointerReleased += RootGrid_PointerReleased;
    }

    private void RootGrid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        AvaSkiaInkCanvas.Down(ToInkingInputArgs(e));
    }

    private void RootGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        AvaSkiaInkCanvas.Move(ToInkingInputArgs(e));
    }

    private void RootGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        AvaSkiaInkCanvas.Up(ToInkingInputArgs(e));
    }

    private InkingInputArgs ToInkingInputArgs(PointerEventArgs args)
    {
        var currentPoint = args.GetCurrentPoint(RootGrid);
        var (x, y) = currentPoint.Position;
        return new InkingInputArgs(currentPoint.Pointer.Id, new StylusPoint(x, y));
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            await Task.Delay(100);
            AvaSkiaInkCanvas.InvalidateVisual();
        }
    }
}