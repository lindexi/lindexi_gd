using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Primitive;

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
        if (!TryGetRunProperty(runPropertyId, out var runProperty, out var errorCode))
        {
            return errorCode;
        }

        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        var newRunProperty = runProperty with { FontName = new FontName(text) };
        RunPropertyDictionary[runPropertyId] = newRunProperty;
        return ErrorCode.Success;
    }

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