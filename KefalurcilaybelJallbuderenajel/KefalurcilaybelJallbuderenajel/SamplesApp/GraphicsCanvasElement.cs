using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Microsoft.Maui.Graphics.Skia;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.UI.Composition;
using Microsoft.Maui.Graphics;

namespace SamplesApp;

public class GraphicsCanvasElement : FrameworkElement
{
    public GraphicsCanvasElement()
    {
        Visual.Children.InsertAtBottom(new GraphicsCanvasVisual(Visual.Compositor, this));
    }

    public event EventHandler<ICanvas>? Draw;

    internal void InvokeDraw(ICanvas canvas)
    {
        Draw?.Invoke(this, canvas);
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
                using var skiaCanvas = new SkiaCanvas();
                skiaCanvas.Canvas = session.Surface.Canvas;
                graphicsCanvasElement.InvokeDraw(skiaCanvas);
            }
        }
    }
}


