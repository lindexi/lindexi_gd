using System.Reflection;

using Microsoft.UI.Xaml.Input;

namespace ManipulationDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        _mainPage = this;
        this.InitializeComponent();
    }
    
    private static MainPage _mainPage = null!;

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
            LogTextBlock.Text += $"历史点： {e.GetIntermediatePoints(null).Count}";
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

            var point = e.GetCurrentPoint(_mainPage);
            X = point.Position.X;
            Y = point.Position.Y;
            Width = point.Properties.ContactRect.Width;
            Height = point.Properties.ContactRect.Height;
        }
    }
}
