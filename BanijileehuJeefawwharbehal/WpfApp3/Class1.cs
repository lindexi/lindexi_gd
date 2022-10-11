using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfApp3
{
    internal class TestControl : FrameworkElement
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 100, 50));
            drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(200, 200, 100, 50));
        }
    }
}
