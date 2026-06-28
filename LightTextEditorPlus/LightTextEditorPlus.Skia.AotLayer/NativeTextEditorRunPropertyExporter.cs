using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus;

static class NativeTextEditorRunPropertyExporter
{
    [UnmanagedCallersOnly(EntryPoint = "CreateRunPropertyFromStyle")]
    public static int CreateRunPropertyFromStyle(int textEditorId)
    {
        if (!TextEditor.TryGetEditor(textEditorId,out var textEditor,out var errorCode))
        {
            return errorCode;
        }

        SkiaTextRunProperty skiaTextRunProperty = textEditor.TextEditorCore.DocumentManager.StyleRunProperty.AsSkiaRunProperty();
        return RunPropertyStore.Create(skiaTextRunProperty);
    }

    /// <summary>
    /// 释放文本字符属性
    /// </summary>
    /// <param name="runPropertyId"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "FreeRunProperty")]
    public static int FreeRunProperty(int runPropertyId)
    {
        return RunPropertyStore.TryRemove(runPropertyId, out var errorCode)
            ? ErrorCode.Success
            : errorCode;
    }

    /// <summary>
    /// 设置文本字符属性的字体名
    /// </summary>
    /// <param name="runPropertyId"></param>
    /// <param name="unicode16Text"></param>
    /// <param name="charCount"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontName")]
    public static int SetRunPropertyFontName(int runPropertyId, IntPtr unicode16Text, int charCount)
    {
        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        return RunPropertyStore.Update(runPropertyId, runProperty => runProperty with { FontName = new FontName(text) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontSize")]
    public static int SetRunPropertyFontSize(int runPropertyId, double fontSize)
    {
        return RunPropertyStore.Update(runPropertyId, runProperty => runProperty with { FontSize = fontSize });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyOpacity")]
    public static int SetRunPropertyOpacity(int runPropertyId, double opacity)
    {
        return RunPropertyStore.Update(runPropertyId, runProperty => runProperty with { Opacity = opacity });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyForegroundColor")]
    public static int SetRunPropertyForegroundColor(int runPropertyId, byte alpha, byte red, byte green, byte blue)
    {
        var skColor = new SKColor(red, green, blue, alpha);
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { Foreground = new SolidColorSkiaTextBrush(skColor) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyBackgroundColor")]
    public static int SetRunPropertyBackgroundColor(int runPropertyId, byte alpha, byte red, byte green, byte blue)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { Background = new SKColor(red, green, blue, alpha) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyStretch")]
    public static int SetRunPropertyStretch(int runPropertyId, int stretch)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { Stretch = (SKFontStyleWidth) stretch });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontWeight")]
    public static int SetRunPropertyFontWeight(int runPropertyId, int fontWeight)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { FontWeight = (SKFontStyleWeight) fontWeight });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontStyle")]
    public static int SetRunPropertyFontStyle(int runPropertyId, int fontStyle)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { FontStyle = (SKFontStyleSlant) fontStyle });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyIsBold")]
    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    public static int SetRunPropertyIsBold(int runPropertyId, int isBold)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { IsBold = isBold != 0 });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyIsItalic")]
    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    public static int SetRunPropertyIsItalic(int runPropertyId, int isItalic)
    {
        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { IsItalic = isItalic != 0 });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontVariant")]
    public static int SetRunPropertyFontVariant(int runPropertyId, byte fontVariant, double baselineProportion)
    {
        var textFontVariant = new TextFontVariant
        {
            FontVariants = (TextFontVariants) fontVariant,
            BaselineProportion = baselineProportion
        };

        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { FontVariant = textFontVariant });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyDecorationFlags")]
    public static int SetRunPropertyDecorationFlags(int runPropertyId, int decorationFlags)
    {
        TextEditorImmutableDecorationCollection decorationCollection = new TextEditorImmutableDecorationCollection();

        if ((decorationFlags & UnderlineDecorationFlag) != 0)
        {
            decorationCollection = decorationCollection.Add(TextEditorDecorations.Underline);
        }

        if ((decorationFlags & StrikethroughDecorationFlag) != 0)
        {
            decorationCollection = decorationCollection.Add(TextEditorDecorations.Strikethrough);
        }

        if ((decorationFlags & EmphasisDotsDecorationFlag) != 0)
        {
            decorationCollection = decorationCollection.Add(TextEditorDecorations.EmphasisDots);
        }

        return RunPropertyStore.Update(runPropertyId,
            runProperty => runProperty with { DecorationCollection = decorationCollection });
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyFontName")]
    public static unsafe int GetRunPropertyFontName(int runPropertyId, IntPtr unicode16Text, int charCount, int* textLength)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (textLength != null)
            {
                *textLength = 0;
            }

            return errorCode;
        }

        string text = runProperty.FontName.UserFontName;
        if (textLength != null)
        {
            *textLength = text.Length;
        }

        if (unicode16Text == IntPtr.Zero || charCount <= 0)
        {
            return ErrorCode.Success;
        }

        int copyCount = Math.Min(charCount, text.Length);
        if (copyCount > 0)
        {
            Marshal.Copy(text.ToCharArray(), 0, unicode16Text, copyCount);
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyFontSize")]
    public static unsafe int GetRunPropertyFontSize(int runPropertyId, double* fontSize)
    {
        var errorCode = GetRunPropertyValue(runPropertyId, runProperty => runProperty.FontSize, out var value);
        if (fontSize != null)
        {
            *fontSize = errorCode == ErrorCode.Success ? value : default;
        }

        return errorCode;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyOpacity")]
    public static unsafe int GetRunPropertyOpacity(int runPropertyId, double* opacity)
    {
        var errorCode = GetRunPropertyValue(runPropertyId, runProperty => runProperty.Opacity, out var value);
        if (opacity != null)
        {
            *opacity = errorCode == ErrorCode.Success ? value : default;
        }

        return errorCode;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyForegroundColor")]
    public static unsafe int GetRunPropertyForegroundColor
        (int runPropertyId, byte* alpha, byte* red, byte* green, byte* blue)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (alpha != null) *alpha = 0;
            if (red != null) *red = 0;
            if (green != null) *green = 0;
            if (blue != null) *blue = 0;
            return errorCode;
        }

        SKColor color = runProperty.Foreground.AsSolidColor();
        if (alpha != null) *alpha = color.Alpha;
        if (red != null) *red = color.Red;
        if (green != null) *green = color.Green;
        if (blue != null) *blue = color.Blue;
        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyBackgroundColor")]
    public static unsafe int GetRunPropertyBackgroundColor
        (int runPropertyId, byte* alpha, byte* red, byte* green, byte* blue)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (alpha != null) *alpha = 0;
            if (red != null) *red = 0;
            if (green != null) *green = 0;
            if (blue != null) *blue = 0;
            return errorCode;
        }

        SKColor color = runProperty.Background;
        if (alpha != null) *alpha = color.Alpha;
        if (red != null) *red = color.Red;
        if (green != null) *green = color.Green;
        if (blue != null) *blue = color.Blue;
        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyStretch")]
    public static unsafe int GetRunPropertyStretch(int runPropertyId, int* stretch)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (stretch != null)
            {
                *stretch = default;
            }

            return errorCode;
        }

        if (stretch != null)
        {
            *stretch = (int) runProperty.Stretch;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyFontWeight")]
    public static unsafe int GetRunPropertyFontWeight(int runPropertyId, int* fontWeight)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (fontWeight != null)
            {
                *fontWeight = default;
            }

            return errorCode;
        }

        if (fontWeight != null)
        {
            *fontWeight = (int) runProperty.FontWeight;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyFontStyle")]
    public static unsafe int GetRunPropertyFontStyle(int runPropertyId, int* fontStyle)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (fontStyle != null)
            {
                *fontStyle = default;
            }

            return errorCode;
        }

        if (fontStyle != null)
        {
            *fontStyle = (int) runProperty.FontStyle;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyIsBold")]
    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    public static unsafe int GetRunPropertyIsBold(int runPropertyId, int* isBold)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (isBold != null)
            {
                *isBold = 0;
            }

            return errorCode;
        }

        if (isBold != null)
        {
            *isBold = runProperty.IsBold ? 1 : 0;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyIsItalic")]
    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    public static unsafe int GetRunPropertyIsItalic(int runPropertyId, int* isItalic)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (isItalic != null)
            {
                *isItalic = 0;
            }

            return errorCode;
        }

        if (isItalic != null)
        {
            *isItalic = runProperty.IsItalic ? 1 : 0;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyFontVariant")]
    public static unsafe int GetRunPropertyFontVariant(int runPropertyId, byte* fontVariant, double* baselineProportion)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (fontVariant != null) *fontVariant = 0;
            if (baselineProportion != null) *baselineProportion = default;
            return errorCode;
        }

        if (fontVariant != null)
        {
            *fontVariant = (byte) runProperty.FontVariant.FontVariants;
        }

        if (baselineProportion != null)
        {
            *baselineProportion = runProperty.FontVariant.BaselineProportion;
        }

        return ErrorCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetRunPropertyDecorationFlags")]
    public static unsafe int GetRunPropertyDecorationFlags(int runPropertyId, int* decorationFlags)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            if (decorationFlags != null)
            {
                *decorationFlags = 0;
            }

            return errorCode;
        }

        int flags = 0;

        if (runProperty.DecorationCollection.Contains(TextEditorDecorations.Underline))
        {
            flags |= UnderlineDecorationFlag;
        }

        if (runProperty.DecorationCollection.Contains(TextEditorDecorations.Strikethrough))
        {
            flags |= StrikethroughDecorationFlag;
        }

        if (runProperty.DecorationCollection.Contains(TextEditorDecorations.EmphasisDots))
        {
            flags |= EmphasisDotsDecorationFlag;
        }

        if (decorationFlags != null)
        {
            *decorationFlags = flags;
        }

        return ErrorCode.Success;
    }

    private static int GetRunPropertyValue<T>
        (int runPropertyId, Func<SkiaTextRunProperty, T> valueSelector, [MaybeNull] out T value)
    {
        if (!RunPropertyStore.TryGet(runPropertyId, out var runProperty, out var errorCode))
        {
            value = default;
            return errorCode;
        }

        value = valueSelector(runProperty);
        return ErrorCode.Success;
    }

    private const int UnderlineDecorationFlag = 1 << 0;
    private const int StrikethroughDecorationFlag = 1 << 1;
    private const int EmphasisDotsDecorationFlag = 1 << 2;

    private static readonly NativeObjectStore<SkiaTextRunProperty> RunPropertyStore =
        new(ErrorCode.RunPropertyNotFound, ErrorCode.RunPropertyBeFree);
}

static class RunPropertyExtension
{
    public static SkiaTextRunProperty AsSkiaRunProperty(this IReadOnlyRunProperty runProperty) => (SkiaTextRunProperty) runProperty;
}