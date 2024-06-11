using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Windows.Foundation;
using Windows.Foundation.Collections;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RalllawfairlekolairHemyiqearkice;

public sealed partial class CornerRadiusRectangleEraserView : HighPrecisionEraserViewBase
{
    public CornerRadiusRectangleEraserView()
    {
        this.InitializeComponent();
        Loaded += CornerRadiusRectangleEraserView_Loaded;
    }

    private void CornerRadiusRectangleEraserView_Loaded(object sender, RoutedEventArgs e)
    {
        // 此时获取到的 ActualWidth 和 ActualHeight 都是 0 的值

        //Rect rect = Rect.Empty;
        //foreach (var rootPanelChild in RootPanel.Children)
        //{
        //    rect.Union(new Rect(new Point(), rootPanelChild.DesiredSize));
        //}

        //IEraserView eraserView = this;
        //eraserView.Width = ActualWidth;
        //eraserView.Height = ActualHeight;
    }
}
