using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Graphics;

namespace BihuwelcairkiDelalurnere
{
    internal class AreaDraw
    {
        public List<double> 系列1 { get; } = new List<double>()
        {
            10, 32, 29, 12, 15
        };

        public List<double> 系列2 { get; } = new List<double>()
        {
            12, 5, 12, 21, 29
        };

        public double Width { get; } = 730;
        public double Height { get; } = 500;

        public void OnRender(ICanvas canvas)
        {
            // 绘制图表框
            for (int i = 0; i <= 35; i += 5)
            {
                // 绘制网格线
                var leftMargin = 30;
                var bottomMargin = 5;
                canvas.StrokeSize = 2;
                canvas.StrokeColor = Colors.Gray;
                var left = leftMargin;
                var right = (float) Width;
                var top = bottomMargin + i / 5 * 70;
                var bottom = top;
                canvas.DrawLine(left, top, right, bottom);
            }
        }
    }
}