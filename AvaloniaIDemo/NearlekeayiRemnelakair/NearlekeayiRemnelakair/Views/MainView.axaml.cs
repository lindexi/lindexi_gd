using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace NearlekeayiRemnelakair.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
}

class Foo : Control
{
    public override void Render(DrawingContext context)
    {
        context.Custom(new C(new Rect(0, 0, Bounds.Width, Bounds.Height)));
    }
}

class C : ICustomDrawOperation
{
    public C(Rect bounds)
    {
        Bounds = bounds;
    }

    public void Dispose()
    {

    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public bool HitTest(Point p)
    {
        return false;
    }

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature != null)
        {
            var skiaSharpApiLease = leaseFeature.Lease();
            var skSurface = skiaSharpApiLease.SkSurface;
            var canvas = skiaSharpApiLease.SkCanvas;
        }
    }

    public Rect Bounds { get; }
}