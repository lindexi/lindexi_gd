using MS.Internal.Ink;
using WpfInk.PresentationCore.System.Windows.Ink;
using WpfInk.PresentationCore.System.Windows.Input.Stylus;

namespace WpfInk;

public static class InkStrokeRenderer
{
    public static void Render(IStreamGeometryContext streamGeometryContext, in StrokeRendererInfo info)
    {
        var drawingAttributes = new DrawingAttributes()
        {
            Width = info.Width,
            Height = info.Height,
        };

        var stylusPointCollection = new StylusPointCollection(info.StylusPointCollection.Count);

        foreach (InkStylusPoint2D inkStylusPoint2D in info.StylusPointCollection)
        {
            stylusPointCollection.Add(inkStylusPoint2D.ToStylusPoint());
        }

        var stroke = new Stroke(stylusPointCollection, drawingAttributes);
        StrokeNodeIterator strokeNodeIterator = StrokeNodeIterator.GetIterator(stroke, drawingAttributes);
        var internalStreamGeometryContext = new InternalStreamGeometryContext(streamGeometryContext);
        StrokeRenderer.CalcGeometryAndBounds(strokeNodeIterator, drawingAttributes, calculateBounds: false, internalStreamGeometryContext, out _);
    }
}