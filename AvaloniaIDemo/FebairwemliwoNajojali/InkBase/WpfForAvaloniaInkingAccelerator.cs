using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NarjejerechowainoBuwurjofear.Inking.Contexts;

using SkiaSharp;

namespace InkBase;

public class WpfForAvaloniaInkingAccelerator
{
    public static WpfForAvaloniaInkingAccelerator Instance { get; } = new WpfForAvaloniaInkingAccelerator();

    public IWpfInkLayer InkLayer { get; set; } = null!;
}

public interface IWpfInkLayer
{
    void Down(InkPoint screenPoint);
    void Move(InkPoint screenPoint);
    void Up(InkPoint screenPoint);

    event EventHandler<SkiaStroke>? StrokeCollected;

    void HideStroke(SkiaStroke skiaStroke);

    SkiaStroke PointListToStroke(InkId id, IReadOnlyList<InkPoint> points);
}

public readonly record struct InkPoint(InkId Id, double X, double Y);

public class SkiaStroke
{
    public SkiaStroke(InkId id, SKPath inkPath)
    {
        Id = id;
        InkPath = inkPath;
    }

    public InkId Id { get; }

    public SKPath InkPath { get; }
}