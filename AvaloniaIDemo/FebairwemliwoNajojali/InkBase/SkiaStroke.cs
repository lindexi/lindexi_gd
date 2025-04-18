using NarjejerechowainoBuwurjofear.Inking.Contexts;
using SkiaSharp;

namespace InkBase;

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