using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.FontManagers;
using LightTextEditorPlus.Utils;

using SkiaSharp;

namespace LightTextEditorPlus.Platform;

public class AvaloniaTextEditorResourceManager : SkiaPlatformResourceManager, IFontNameToSKTypefaceManager
{
    public AvaloniaTextEditorResourceManager(TextEditor textEditor, SkiaTextEditor skiaTextEditor) : base(skiaTextEditor)
    {
        // 为什么此时不能用 textEditor.SkiaTextEditor 属性？
        // 因为初始化过程中 textEditor.SkiaTextEditor 属性还没有被赋值
        _ = textEditor; // 现在 TextEditor 还没什么用
    }

    protected override SKTypeface? TryResolveFont(string fontName, SKFontStyle skFontStyle)
    {
        if (TextEditorFontResourceManager.TryGetFontFile(fontName, out var fontFile))
        {
            // 自定义的字体
            return SKTypeface.FromFile(fontFile.FullName);
        }

        return base.TryResolveFont(fontName, skFontStyle);
    }
}
