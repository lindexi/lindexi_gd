﻿using HarfBuzzSharp;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

public record SkiaTextRunProperty : LayoutOnlyRunProperty
{
    internal SkiaTextRunProperty(SkiaPlatformFontManager skiaPlatformFontManager)
    {
        FontManager = skiaPlatformFontManager;
    }

    public override FontName FontName
    {
        get => _fontName;
        init
        {
            if (value.Equals(_fontName))
            {
                return;
            }

            _fontName = value;
            _skTypeface = null;
        }
    }

    private readonly FontName _fontName;
    private SkiaPlatformFontManager FontManager { get; }

    public SKTypeface GetRenderSKTypeface(char unicodeChar = '1')
    {
        if (_skTypeface is null)
        {
            RenderingFontInfo renderingFontInfo = FontManager.GetRenderingFontInfo(this, unicodeChar);
            _skTypeface = renderingFontInfo.Typeface;
        }

        return _skTypeface;
    }

    private SKTypeface? _skTypeface;

    public SKFont GetRenderSKFont(char unicodeChar = '1')
    {
        // todo 处理对齐情况
        // todo 处理缓存
        SKFont renderSkFont = new SKFont(GetRenderSKTypeface(unicodeChar), (float) FontSize);
        // From Avalonia
        // Ideally the requested edging should be passed to the glyph run.
        // Currently the edging is computed dynamically inside the drawing context, so we can't know it in advance.
        // But the bounds depends on the edging: for now, always use SubpixelAntialias so we have consistent values.
        // The resulting bounds may be shifted by 1px on some fonts:
        // "F" text with Inter size 14 has a 0px left bound with SubpixelAntialias but 1px with Antialias.

        var edging = SKFontEdging.SubpixelAntialias;

        renderSkFont.Hinting = SKFontHinting.Full;
        renderSkFont.Edging = edging;
        renderSkFont.Subpixel = edging != SKFontEdging.Alias;

        return renderSkFont;
    }
}