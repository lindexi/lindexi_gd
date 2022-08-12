using System;
using System.IO;
using System.Reflection;
using Gtk;
using Microsoft.Maui.Graphics.Skia;
using PptxCore;

using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.Gtk;

namespace PptxGtk
{
    public class MainWindow : Window
    {
        public MainWindow()
            : this(new Builder("MainWindow.glade"))
        {
        }

        private MainWindow(Builder builder)
            : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);
            DeleteEvent += OnWindowDeleteEvent;

            var skiaView = new SKDrawingArea();
            skiaView.PaintSurface += OnPaintSurface;
            skiaView.Show();
            Child = skiaView;
        }

        private void OnWindowDeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var file = new FileInfo(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Test.pptx"));

            var modelReader = new ModelReader();
            var areaChartRender = modelReader.BuildAreaChartRender(file);

            // the the canvas and properties
            var canvas = e.Surface.Canvas;

            // make sure the canvas is blank
            canvas.Clear(SKColors.White);

            areaChartRender.Render(new SkiaCanvas()
            {
                Canvas = canvas,
            });
        }
    }
}
