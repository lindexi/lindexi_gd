using System;
using System.Windows;

namespace Microsoft.Maui.Graphics.Xaml
{
    static class Fonts
    {
        public readonly static FontService CurrentService = new FontService();
    }

    class FontService
    {
        internal FakeFontStyle? GetFontStyleById(string fontName)
        {
            return null;
        }
    }

    class FakeFontStyle
    {
        public FontStyleType StyleType { get; internal set; }

        public Font FontFamily { get; internal set; }
        public int Weight { get; internal set; }
    }
}
