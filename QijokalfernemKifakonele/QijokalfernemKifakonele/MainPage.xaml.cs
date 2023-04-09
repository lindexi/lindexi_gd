using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace QijokalfernemKifakonele
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Canvas.Draw += Canvas_Draw;
        }

        private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_stopwatch.Elapsed.TotalSeconds > 1)
            {
                var fps = _renderCount / _stopwatch.Elapsed.TotalSeconds;
                _renderCount = 0;
                _stopwatch.Restart();

                TextBlock.Text = $"FPS {fps}";
            }

            _renderCount++;

            var drawingSession = args.DrawingSession;

            for (int i = 0; i < 100; i++)
            {
                var color = Color.FromArgb(100, (byte) _random.Next(255), (byte) _random.Next(255), (byte) _random.Next(255));

                for (int j = 0; j < 1000; j++)
                {
                    Rect rect = new Rect(_random.Next(1024), _random.Next(1024), 10, 10);
                    drawingSession.DrawRectangle(rect, color);
                }
            }

            Canvas.Invalidate();
        }

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        private int _renderCount = 0;

        private Random _random = new Random();
    }
}
