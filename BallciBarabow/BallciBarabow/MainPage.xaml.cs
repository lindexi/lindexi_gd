using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace BallciBarabow
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

        private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            using (var canvasPathBuilder = new CanvasPathBuilder(args.DrawingSession))
            {
                // 这里可以画出 Path 或写出文字 lindexi.github.io
                canvasPathBuilder.BeginFigure(100, 100);
                canvasPathBuilder.AddLine(200, 100);
                canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), Colors.Gray, 2);
            }

            //DrawText(args);

            //DrawPath(sender, args);

            //DrawAlphaMaskEffect(sender, args);
        }

        private void DrawAlphaMaskEffect(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var alphaGradientBrush = new CanvasRadialGradientBrush(sender, Colors.Black, Colors.Transparent)
            {
                Center = _bitmap1.Size.ToVector2() / 2,

                RadiusX = (float) _bitmap1.Size.Width / 2,
                RadiusY = (float) _bitmap1.Size.Height / 2
            };

            var alphaMask = new CanvasCommandList(sender);

            var center = sender.Size.ToVector2() / 2;
            center.X -= 100;
            center.Y -= 100;

            using (var canvasDrawingSession = alphaMask.CreateDrawingSession())
            {
                //var canvasTextFormat = new CanvasTextFormat {FontSize = 100};

                //using (canvasTextFormat)
                //{
                //    canvasDrawingSession.DrawText("林德熙", center, Color.FromArgb(0xA1, 0, 0, 0), canvasTextFormat);
                //}
                canvasDrawingSession.FillRectangle(_bitmap1.Bounds, alphaGradientBrush);
            }

            var alphaMaskEffect = new AlphaMaskEffect
            {
                Source = _bitmap1,
                AlphaMask = alphaMask
            };

            using (var canvasDrawingSession = args.DrawingSession)
            {
                canvasDrawingSession.DrawImage(alphaMaskEffect);
            }
        }

        private void DrawPath(CanvasControl canvas, CanvasDrawEventArgs args)
        {
            var width = (float) canvas.ActualWidth - 20;
            var height = (float) (canvas.ActualHeight) - 20;
            var midWidth = (float) (width * .5);
            var midHeight = (float) (height * .5);
            Color color = Colors.Gray;
            using (var canvasPathBuilder = new CanvasPathBuilder(args.DrawingSession))
            {
                // Horizontal line
                // 水平线
                canvasPathBuilder.BeginFigure(new Vector2(0, midHeight));
                canvasPathBuilder.AddLine(new Vector2(width, midHeight));
                canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                // Horizontal line arrow
                // 水平箭头
                canvasPathBuilder.BeginFigure(new Vector2(width - 10, midHeight - 3));
                canvasPathBuilder.AddLine(new Vector2(width, midHeight));
                canvasPathBuilder.AddLine(new Vector2(width - 10, midHeight + 3));
                canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);

                args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(canvasPathBuilder), color, strokeWidth: 1);
            }

            using (var cpb = new CanvasPathBuilder(args.DrawingSession))
            {
                // Vertical line
                // 垂直线
                cpb.BeginFigure(new Vector2(midWidth, 0));
                cpb.AddLine(new Vector2(midWidth, height));
                cpb.EndFigure(CanvasFigureLoop.Open);

                // Vertical line arrow
                cpb.BeginFigure(new Vector2(midWidth - 3, 10));
                cpb.AddLine(new Vector2(midWidth, 0));
                cpb.AddLine(new Vector2(midWidth + 3, 10));
                cpb.EndFigure(CanvasFigureLoop.Open);

                args.DrawingSession.DrawGeometry(CanvasGeometry.CreatePath(cpb), color, 1);
            }
        }

        private static void DrawText(CanvasDrawEventArgs args)
        {
            using (var canvasDrawingSession = args.DrawingSession)
            {
                var canvasTextFormat = new CanvasTextFormat {FontSize = 100};

                using (canvasTextFormat)
                {
                    canvasDrawingSession.DrawText("林德熙", new Vector2(100, 100), Color.FromArgb(0xA1, 100, 100, 100),
                        canvasTextFormat);
                }
            }
        }

        private void Canvas_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        private async Task CreateResourcesAsync(CanvasControl canvasControl)
        {
            _bitmap1 = await CanvasBitmap.LoadAsync(canvasControl, "Assets/1.png");
            _bitmap2 = await CanvasBitmap.LoadAsync(canvasControl, "Assets/2.png");
        }

        private CanvasBitmap _bitmap1;
        private CanvasBitmap _bitmap2;
    }
}