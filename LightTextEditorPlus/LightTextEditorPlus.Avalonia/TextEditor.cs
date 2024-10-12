using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using LightTextEditorPlus.Core;

namespace LightTextEditorPlus.Avalonia;

public partial class TextEditor : Control
{
    public TextEditor()
    {
        SkiaTextEditor = new SkiaTextEditor();
        SkiaTextEditor.RenderRequested += (sender, args) => InvalidateVisual();

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public SkiaTextEditor SkiaTextEditor { get; }
    public TextEditorCore TextEditorCore => SkiaTextEditor.TextEditorCore;

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        TextEditorCore.DocumentManager.DocumentWidth = finalSize.Width;
        TextEditorCore.DocumentManager.DocumentHeight = finalSize.Height;

        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        ITextEditorSkiaRender textEditorSkiaRender = SkiaTextEditor.GetCurrentRender();
        context.Custom(new TextEditorCustomDrawOperation(new Rect(DesiredSize), textEditorSkiaRender));
    }
}

class TextEditorCustomDrawOperation : ICustomDrawOperation
{
    public TextEditorCustomDrawOperation(Rect bounds, ITextEditorSkiaRender render)
    {
        _render = render;
        Bounds = bounds;
    }

    private readonly ITextEditorSkiaRender _render;

    public void Dispose()
    {

    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }

    public void Render(ImmediateDrawingContext context)
    {
        ISkiaSharpApiLeaseFeature? skiaSharpApiLeaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skiaSharpApiLeaseFeature != null)
        {
            using ISkiaSharpApiLease skiaSharpApiLease = skiaSharpApiLeaseFeature.Lease();
            _render.Render(skiaSharpApiLease.SkCanvas);
        }
    }

    public Rect Bounds { get; }
}
