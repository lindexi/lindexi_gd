using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.UI.Composition;

namespace SamplesApp;

public class GraphicsCanvasElement : FrameworkElement
{
    public GraphicsCanvasElement()
    {
        Visual.Children.InsertAtBottom(new GraphicsCanvasVisual(Visual.Compositor, this));
    }

    class GraphicsCanvasVisual : Visual
    {
        public GraphicsCanvasVisual(Compositor compositor, GraphicsCanvasElement owner) : base(compositor)
        {
            _owner = new WeakReference<GraphicsCanvasElement>(owner);
        }

        private readonly WeakReference<GraphicsCanvasElement> _owner;

        internal override void Draw(in DrawingSession session)
        {
            if (_owner.TryGetTarget(out var graphicsCanvasElement))
            {
                var canvas = session.Surface.Canvas;
                canvas.DrawRect(0, 0, 100, 100, new SKPaint()
                {
                    Color = new SKColor(0xF1, 0x56, 0x56, 0xFF),
                    IsStroke = true,
                    StrokeWidth = 1,
                });
            }
        }
    }
}


