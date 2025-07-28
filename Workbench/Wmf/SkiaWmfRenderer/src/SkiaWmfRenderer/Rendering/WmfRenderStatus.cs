using System.Text;

using Oxage.Wmf;

using SkiaSharp;

namespace SkiaWmfRenderer.Rendering;

class WmfRenderStatus : IDisposable
{
    public required float CurrentX { get; set; }
    public required float CurrentY { get; set; }

    public required int Width { get; set; }
    public required int Height { get; set; }

    public int CurrentRecordsIndex { get; set; }

    public SKColor CurrentPenColor { get; set; } = SKColors.Empty;
    public float CurrentPenThickness { get; set; } = 0;

    public SKColor CurrentFillColor { get; set; } = SKColor.Empty;

    public float CurrentFontSize { get; set; }

    public string? CurrentFontName { get; set; } = null;

    public bool IsItalic { get; set; } = false;
    public int FontWeight { get; set; } = 400;

    public CharacterSet CurrentCharacterSet
    {
        get => _currentCharacterSet;
        set
        {
            _currentCharacterSet = value;
            CurrentEncoding = _currentCharacterSet.CharacterSetToEncoding();
        }
    }

    private CharacterSet _currentCharacterSet = CharacterSet.DEFAULT_CHARSET;

    public Encoding CurrentEncoding { get; private set; } = CharacterSet.DEFAULT_CHARSET.CharacterSetToEncoding();

    public float LastDxOffset { get; set; } = 0;

    public SKColor CurrentTextColor { get; set; } =
        SKColors.Black;

    public SKPaint Paint { get; } = new SKPaint()
    {
        IsAntialias = true
    };

    public SKFont SKFont { get; } = new SKFont();


    public void UpdateSkiaTextStatus()
    {
        var skFont = SKFont;
        skFont.Size = CurrentFontSize;
        skFont.Typeface = SKTypeface.FromFamilyName(CurrentFontName, (SKFontStyleWeight) FontWeight,
            SKFontStyleWidth.Normal, IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        Paint.Style = SKPaintStyle.Fill;
        Paint.Color = CurrentTextColor;
    }

    public void UpdateSkiaStrokeStatus()
    {
        Paint.IsStroke = true;
        Paint.Color = CurrentPenColor;
        Paint.StrokeWidth = CurrentPenThickness;
        Paint.Style = SKPaintStyle.Stroke;
    }

    public void UpdateSkiaFillStatus()
    {
        Paint.Style = SKPaintStyle.Fill;
        Paint.Color = CurrentFillColor;
    }

    public bool IsIncludeText { get; set; } = false;
    public bool IsIncludeOtherEncoding { get; set; } = false;
    public bool IsIncludeTextWithDx { get; set; } = false;

    public void Dispose()
    {
        Paint.Dispose();
        SKFont.Dispose();
    }
}