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

        public float Width { get; } = 730;
        public float Height { get; } = 500;

        public void OnRender(ICanvas canvas)
        {
            // 绘制图表框
            var leftMargin = 30;
            for (int i = 0; i <= 35; i += 5)
            {
                // 绘制网格线
                var bottomMargin = 0;
                canvas.StrokeSize = 2;
                canvas.StrokeColor = Colors.Gray;
                var left = leftMargin;
                var right = Width;
                var top = bottomMargin + (Height - i / 5 * 70);
                var bottom = top;
                canvas.DrawLine(left, top, right, bottom);

                // 绘制坐标轴的数值
                var textX = 0;
                var textY = top + 5;
                canvas.FontSize = 16;
                canvas.DrawString(i.ToString(), textX, textY, HorizontalAlignment.Left);
            }

            // 绘制坐标轴横轴
            var 横轴 = new[] { "2202/1/5", "2002/1/6", "2002/1/7", "2002/1/8", "2002/1/9" };
            for (var i = 0; i < 横轴.Length; i++)
            {
                var x = i * (Width - leftMargin) / (横轴.Length - 1) + leftMargin;
                var bottomMargin = 15;
                var textX = x - 20;
                var textY = Height + bottomMargin;
                canvas.DrawString(横轴[i], textX, textY, HorizontalAlignment.Left);
            }

            // 绘制 系列1
            var path1 = new PathF();
            var startX = leftMargin;
            path1.Move(startX, 0);
            for (var i = 0; i < 系列1.Count; i++)
            {
                var value1 = 系列1[i];
                var maxValue = 35;

                var x = i * (Width - leftMargin) / (系列1.Count - 1) + leftMargin;
                var y = Height - (value1 / maxValue) * Height;

                path1.LineTo(x, (float) y);
            }

            path1.LineTo(Width, Height)
                .LineTo(startX, Height)
                .Close();
            canvas.FillColor = Color.Parse($"#FF5B9BD5");
            canvas.FillPath(path1);

            // 绘制 系列2
            var path2 = new PathF();
            path2.Move(startX, 0);
            for (var i = 0; i < 系列2.Count; i++)
            {
                var value2 = 系列2[i];

                var maxValue = 35;

                var x = i * (Width - leftMargin) / (系列1.Count - 1) + leftMargin;
                var y = Height - (value2 / maxValue) * Height;

                path2.LineTo(x, (float) y);
            }



            path2.LineTo(Width, Height)
                .LineTo(startX, Height)
                .Close();
            canvas.FillColor = Color.Parse($"#FFADB9CA").WithAlpha(0.5f);
            canvas.FillPath(path2);
        }
    }
}