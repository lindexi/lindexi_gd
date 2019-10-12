using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LurjuweejeGeewukercha
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // 如果修改外层控件的大小让滚动条的显示大小修改，是不会进入这个方法
            // 只有用户滚动或放大完成之后才会调用这个方法
            //CheckControlShow();
        }

        private void CheckControlShow()
        {
            UIElement control = TextBlock;

            var top = control.TransformToVisual(StackPanel).TransformPoint(new Point());
            var controlBounds = new Rect(top, control.DesiredSize);

            var viewBounds = new Rect(new Point(ScrollViewer.HorizontalOffset, ScrollViewer.VerticalOffset), new Size(ScrollViewer.ViewportWidth, ScrollViewer.ViewportHeight));

            if (RectIntersects(viewBounds, controlBounds))
            {
                Debug.WriteLine("歪楼");
            }
            else
            {
                Debug.WriteLine("不歪楼");
            }
        }

        private void ScrollViewer_LayoutUpdated(object sender, object e)
        {
            CheckControlShow();
        }

        private static bool RectIntersects(Rect a, Rect b)
        {
            return !(b.Left > a.Right
                || b.Right < a.Left
                || b.Top > a.Bottom
                || b.Bottom < a.Top);
        }
    }
}
