using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoInk;
public sealed partial class PointerEventArgsTests : UserControl
{
    public PointerEventArgsTests()
    {
        this.InitializeComponent();
        Border.PointerEntered += PointerEventArgsTests_PointerEntered;
        Border.PointerExited += PointerEventArgsTests_PointerExited;
        Border.PointerPressed += PointerEventArgsTests_PointerPressed;
        Border.PointerMoved += PointerEventArgsTests_PointerMoved;
        Border.PointerReleased += PointerEventArgsTests_PointerReleased;
        Border.PointerCanceled += PointerEventArgsTests_PointerCanceled;
    }

    private void PointerEventArgsTests_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Entered", e);
    }

    private void PointerEventArgsTests_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Exited", e);
    }

    private void PointerEventArgsTests_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Pressed", e);
    }

    private void PointerEventArgsTests_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Moved", e);
    }

    private void PointerEventArgsTests_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Released", e);
    }

    private void PointerEventArgsTests_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        LogPointerMessage("Canceled", e);
    }

    private void LogPointerMessage(string message, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(Border);

        var pointProperties = currentPoint.Properties;

        TextBox.Text += $"{message} Id:{currentPoint.PointerId} ({currentPoint.Position.X:0.00},{currentPoint.Position.Y:0.00}) LeftButtonPressed:{pointProperties.IsLeftButtonPressed} IsPrimary:{pointProperties.IsPrimary} IsInRange:{pointProperties.IsInRange} Pressure:{pointProperties.Pressure:0.00} WH:({pointProperties.ContactRect.Width:0.00},{pointProperties.ContactRect.Height:0.00})\r\n";
    }
}
