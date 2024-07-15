using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JallhelfallwhearwirajiKefaywiwhe;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        _mainPage = this;
        this.InitializeComponent();
    }

    private static MainWindow _mainPage = null!;

    private void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        TouchInfoDictionary[e.Pointer.PointerId] = new TouchInfo(e);
        Output();
    }

    private void UIElement_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (TouchInfoDictionary.TryGetValue(e.Pointer.PointerId, out var touchInfo))
        {
            touchInfo.Update(e);
            Output();
            LogTextBlock.Text += $"Count of IntermediatePoint = {e.GetIntermediatePoints(null).Count}\r\n";
            //LogTextBlock.Text += $"是否包含不同点 : {e.GetIntermediatePoints(null).Any(t => t.PointerId != e.Pointer.PointerId)}\r\n";

            Thread.Sleep(100);
        }
    }

    private void UIElement_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (TouchInfoDictionary.TryGetValue(e.Pointer.PointerId, out var touchInfo))
        {
            touchInfo.Update(e);
            touchInfo.IsUp = true;
            Output();
        }
    }

    private void Output()
    {
        LogTextBlock.Text = $"当前落下点数： {TouchInfoDictionary.Count(t => t.Value.IsUp is false)}\r\n";

        foreach (var (_, value) in TouchInfoDictionary)
        {
            if (value.IsUp)
            {
                continue;
            }

            LogTextBlock.Text += $"[{value.PointerId}] XY={value.X:0.00},{value.Y:0.00} WH={value.Width:0.00},{value.Height:0.00} \r\n";
        }
    }

    private Dictionary<uint, TouchInfo> TouchInfoDictionary { get; } = new Dictionary<uint, TouchInfo>();

    class TouchInfo
    {
        public TouchInfo(PointerRoutedEventArgs e)
        {
            Update(e);
        }

        public uint PointerId { set; get; }

        public double X { set; get; }
        public double Y { set; get; }


        public double Width { set; get; }
        public double Height { set; get; }

        public bool IsUp { set; get; }

        public void Update(PointerRoutedEventArgs e)
        {
            PointerId = e.Pointer.PointerId;

            var point = e.GetCurrentPoint(_mainPage.Content);
            X = point.Position.X;
            Y = point.Position.Y;
            Width = point.Properties.ContactRect.Width;
            Height = point.Properties.ContactRect.Height;
        }
    }
}
