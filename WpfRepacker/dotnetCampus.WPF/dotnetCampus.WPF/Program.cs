using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Navigation;
using MS.Internal;


namespace dotnetCampus.WPF
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 开始之前
            // 从 [CSDN 下载](https://download.csdn.net/download/lindexi_gd/13769715) 基础框架逻辑
            // 获取发一封邮件给我，让我发链接给你

            // 解压缩内容，放在 Lib 文件夹下，参考 dotnetCampus.WPF.csproj 的代码，确保文件都能找到

            var application = new Application();
            var window = new Window()
            {
                Title = "林德熙是逗比",
                Content = new Foo(),
            };
            window.Loaded += (sender, eventArgs) =>
            {
                // 这里的 GetAppWindow 是 internal 的方法，但是在这个程序集可以访问
                var navigationWindow = application.GetAppWindow();
            };
            application.Run(window);
        }
    }

    class Foo : UIElement
    {
        public Foo()
        {
            DrawingVisual drawingVisual = Visual = new DrawingVisual();
            //using (var drawingContext = drawingVisual.RenderOpen())
            //{
            //    drawingContext.DrawRectangle(Brushes.CadetBlue, null, new Rect(10, 10, 100, 100));
            //}
            using var directDrawingContext = new DirectDrawingContext(drawingVisual);
            directDrawingContext.DrawLine(new Pen(Brushes.CadetBlue, 2), new Point(10, 10), null, new Point(100, 100),
                null);
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        private DrawingVisual Visual { get; }
    }

    class DirectDrawingContext : IDisposable
    {
        public DirectDrawingContext(DrawingVisual drawingVisual)
        {
            _ownerVisual = drawingVisual;
            _renderData = new RenderData();
        }

        public void DrawLine(
            Pen pen,
            Point point0,
            AnimationClock point0Animations,
            Point point1,
            AnimationClock point1Animations)
        {
            unsafe
            {
                // Always assume visual and drawing brushes need realization updates

                UInt32 hPoint0Animations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hPoint1Animations = CompositionResourceManager.InvalidResourceHandle;
                hPoint0Animations = UseAnimations(point0, point0Animations);
                hPoint1Animations = UseAnimations(point1, point1Animations);

                MILCMD_DRAW_LINE_ANIMATE record =
                    new MILCMD_DRAW_LINE_ANIMATE(
                        _renderData.AddDependentResource(pen),
                        point0,
                        hPoint0Animations,
                        point1,
                        hPoint1Animations
                    );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_LINE_ANIMATE) == 48);

                _renderData.WriteDataRecord(MILCMD.MilDrawLineAnimate,
                    (byte*)&record,
                    48 /* sizeof(MILCMD_DRAW_LINE_ANIMATE) */);
            }
        }

        private UInt32 UseAnimations(
            Point baseValue,
            AnimationClock animations)
        {
            if (animations == null)
            {
                return 0;
            }
            else
            {
                return _renderData.AddDependentResource(
                    new PointAnimationClockResource(
                        baseValue,
                        animations));
            }
        }

        public void Close()
        {
            _ownerVisual.RenderClose(_renderData);
        }

        private Visual _ownerVisual;
        private readonly RenderData _renderData;

        public void Dispose()
        {
            Close();
        }
    }
}
