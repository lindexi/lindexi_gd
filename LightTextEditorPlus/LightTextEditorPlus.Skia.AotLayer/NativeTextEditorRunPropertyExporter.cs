using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus;

static class NativeTextEditorRunPropertyExporter
{
    [UnmanagedCallersOnly(EntryPoint = "CreateRunPropertyFromStyle")]
    public static long CreateRunPropertyFromStyle(uint textEditorId)
    {
        if (!TextEditor.TryGetEditor(textEditorId,out var textEditor,out var errorCode))
        {
            return errorCode;
        }

        var id = Interlocked.Increment(ref _id);
        SkiaTextRunProperty skiaTextRunProperty = textEditor.TextEditorCore.DocumentManager.StyleRunProperty.AsSkiaRunProperty();
        RunPropertyDictionary[id] = skiaTextRunProperty;
        return id;
    }

    /// <summary>
    /// 释放文本编辑器
    /// </summary>
    /// <param name="runPropertyId"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "FreeRunProperty")]
    public static int FreeRunProperty(uint runPropertyId)
    {
        if (RunPropertyDictionary.TryRemove(runPropertyId, out _))
        {
            return ErrorCode.Success;
        }
        else
        {
            return ErrorCode.RunPropertyNotFound;
        }
    }

    /// <summary>
    /// 设置文本字符属性的字体名
    /// </summary>
    /// <param name="runPropertyId"></param>
    /// <param name="unicode16Text"></param>
    /// <param name="charCount"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontName")]
    public static int SetRunPropertyFontName(uint runPropertyId, IntPtr unicode16Text, int charCount)
    {
        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        return UpdateRunProperty(runPropertyId, runProperty => runProperty with { FontName = new FontName(text) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontSize")]
    public static int SetRunPropertyFontSize(uint runPropertyId, double fontSize)
    {
        return UpdateRunProperty(runPropertyId, runProperty => runProperty with { FontSize = fontSize });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyOpacity")]
    public static int SetRunPropertyOpacity(uint runPropertyId, double opacity)
    {
        return UpdateRunProperty(runPropertyId, runProperty => runProperty with { Opacity = opacity });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyForegroundColor")]
    public static int SetRunPropertyForegroundColor(uint runPropertyId, byte alpha, byte red, byte green, byte blue)
    {
        var skColor = new SKColor(red, green, blue, alpha);
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { Foreground = new SolidColorSkiaTextBrush(skColor) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyBackgroundColor")]
    public static int SetRunPropertyBackgroundColor(uint runPropertyId, byte alpha, byte red, byte green, byte blue)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { Background = new SKColor(red, green, blue, alpha) });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyStretch")]
    public static int SetRunPropertyStretch(uint runPropertyId, int stretch)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { Stretch = (SKFontStyleWidth) stretch });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontWeight")]
    public static int SetRunPropertyFontWeight(uint runPropertyId, int fontWeight)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { FontWeight = (SKFontStyleWeight) fontWeight });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontStyle")]
    public static int SetRunPropertyFontStyle(uint runPropertyId, int fontStyle)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { FontStyle = (SKFontStyleSlant) fontStyle });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyIsBold")]
    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    public static int SetRunPropertyIsBold(uint runPropertyId, int isBold)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { IsBold = isBold != 0 });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyIsItalic")]
    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    public static int SetRunPropertyIsItalic(uint runPropertyId, int isItalic)
    {
        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { IsItalic = isItalic != 0 });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyFontVariant")]
    public static int SetRunPropertyFontVariant(uint runPropertyId, byte fontVariant, double baselineProportion)
    {
        var textFontVariant = new TextFontVariant
        {
            FontVariants = (TextFontVariants) fontVariant,
            BaselineProportion = baselineProportion
        };

        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { FontVariant = textFontVariant });
    }

    [UnmanagedCallersOnly(EntryPoint = "SetRunPropertyDecorationFlags")]
    public static int SetRunPropertyDecorationFlags(uint runPropertyId, int decorationFlags)
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

        return UpdateRunProperty(runPropertyId,
            runProperty => runProperty with { DecorationCollection = decorationCollection });
    }

    private static int UpdateRunProperty(uint runPropertyId, Func<SkiaTextRunProperty, SkiaTextRunProperty> update)
    {
        if (!TryGetRunProperty(runPropertyId, out var runProperty, out var errorCode))
        {
            return errorCode;
        }

        RunPropertyDictionary[runPropertyId] = update(runProperty);
        return ErrorCode.Success;
    }

    private const int UnderlineDecorationFlag = 1 << 0;
    private const int StrikethroughDecorationFlag = 1 << 1;
    private const int EmphasisDotsDecorationFlag = 1 << 2;

    private static long _id = 0;

    private static readonly ConcurrentDictionary<long/*Id*/, SkiaTextRunProperty> RunPropertyDictionary = [];

    internal static bool TryGetRunProperty
        (long runPropertyId, [NotNullWhen(true)] out SkiaTextRunProperty? runProperty, out ErrorCode errorCode)
    {
        if (runPropertyId < 0)
        {
            // 定义所有小于零的都是错误
            errorCode = new ErrorCode((int) runPropertyId, "");
            runProperty = null;
            return false;
        }

        if (RunPropertyDictionary.TryGetValue(runPropertyId, out runProperty))
        {
            errorCode = ErrorCode.Success;
            return true;
        }

        if (runPropertyId < _id)
        {
            errorCode = ErrorCode.RunPropertyBeFree;
        }
        else
        {
            errorCode = ErrorCode.RunPropertyNotFound;
        }

        return false;
    }
}

static class RunPropertyExtension
{
    public static SkiaTextRunProperty AsSkiaRunProperty(this IReadOnlyRunProperty runProperty) => (SkiaTextRunProperty) runProperty;
}