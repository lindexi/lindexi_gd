using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

public class TextEditorWrapper
{
    public const string DllName = "LightTextEditorPlus.dll";

    [DllImport(DllName)]
    public static extern TextEditorId CreateTextEditor();

    [DllImport(DllName)]
    public static extern int FreeTextEditor(TextEditorId textEditorId);

    [DllImport(DllName)]
    public static extern int AppendText(TextEditorId textEditorId, IntPtr unicode16Text, int charCount);

    public static unsafe int AppendText(TextEditorId textEditorId, string text)
    {
        fixed (char* p = text)
        {
            return AppendText(textEditorId, new IntPtr(p), text.Length);
        }
    }

    [DllImport(DllName)]
    public static extern int SaveAsImageFile
        (TextEditorId textEditorId, IntPtr unicode16FilePath, int charCountOfFilePath);

    public static unsafe int SaveAsImageFile(TextEditorId textEditorId, string filePath)
    {
        fixed (char* p = filePath)
        {
            return SaveAsImageFile(textEditorId, new IntPtr(p), filePath.Length);
        }
    }

    #region RunProperty

    [DllImport(DllName)]
    public static extern RunPropertyId CreateRunPropertyFromStyle(TextEditorId textEditorId);

    [DllImport(DllName)]
    public static extern int FreeRunProperty(RunPropertyId runPropertyId);

    [DllImport(DllName)]
    public static extern int SetRunPropertyFontName(RunPropertyId runPropertyId, IntPtr unicode16Text, int charCount);

    public static unsafe int SetRunPropertyFontName(RunPropertyId runPropertyId, string fontName)
    {
        fixed (char* p = fontName)
        {
            return SetRunPropertyFontName(runPropertyId, new IntPtr(p), fontName.Length);
        }
    }

    [DllImport(DllName)]
    public static extern int SetRunPropertyFontSize(RunPropertyId runPropertyId, double fontSize);

    [DllImport(DllName)]
    public static extern int SetRunPropertyOpacity(RunPropertyId runPropertyId, double opacity);

    [DllImport(DllName)]
    public static extern int SetRunPropertyForegroundColor(RunPropertyId runPropertyId, byte alpha, byte red, byte green, byte blue);

    [DllImport(DllName)]
    public static extern int SetRunPropertyBackgroundColor(RunPropertyId runPropertyId, byte alpha, byte red, byte green, byte blue);

    [DllImport(DllName)]
    public static extern int SetRunPropertyStretch(RunPropertyId runPropertyId, int stretch);

    [DllImport(DllName)]
    public static extern int SetRunPropertyFontWeight(RunPropertyId runPropertyId, int fontWeight);

    [DllImport(DllName)]
    public static extern int SetRunPropertyFontStyle(RunPropertyId runPropertyId, int fontStyle);

    [DllImport(DllName)]
    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    public static extern int SetRunPropertyIsBold(RunPropertyId runPropertyId, int isBold);

    [DllImport(DllName)]
    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    public static extern int SetRunPropertyIsItalic(RunPropertyId runPropertyId, int isItalic);

    [DllImport(DllName)]
    public static extern int SetRunPropertyFontVariant(RunPropertyId runPropertyId, byte fontVariant, double baselineProportion);

    [DllImport(DllName)]
    public static extern int SetRunPropertyDecorationFlags(RunPropertyId runPropertyId, int decorationFlags);

    [DllImport(DllName, EntryPoint = "GetRunPropertyFontName")]
    private static extern int GetRunPropertyFontNameCore(RunPropertyId runPropertyId, IntPtr unicode16Text, int charCount,
        IntPtr textLength);

    public static unsafe int GetRunPropertyFontName(RunPropertyId runPropertyId, out string fontName)
    {
        int charCount = 0;
        int errorCode = GetRunPropertyFontNameCore(runPropertyId, IntPtr.Zero, 0, new IntPtr(&charCount));
        if (errorCode != 0)
        {
            fontName = string.Empty;
            return errorCode;
        }

        if (charCount <= 0)
        {
            fontName = string.Empty;
            return 0;
        }

        var buffer = new char[charCount];
        fixed (char* p = buffer)
        {
            errorCode = GetRunPropertyFontNameCore(runPropertyId, new IntPtr(p), buffer.Length, IntPtr.Zero);
        }

        fontName = errorCode == 0 ? new string(buffer) : string.Empty;
        return errorCode;
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyFontSize")]
    private static extern int GetRunPropertyFontSizeCore(RunPropertyId runPropertyId, IntPtr fontSize);

    public static unsafe int GetRunPropertyFontSize(RunPropertyId runPropertyId, out double fontSize)
    {
        fontSize = 0;
        fixed (double* p = &fontSize)
        {
            return GetRunPropertyFontSizeCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyOpacity")]
    private static extern int GetRunPropertyOpacityCore(RunPropertyId runPropertyId, IntPtr opacity);

    public static unsafe int GetRunPropertyOpacity(RunPropertyId runPropertyId, out double opacity)
    {
        opacity = 0;
        fixed (double* p = &opacity)
        {
            return GetRunPropertyOpacityCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyForegroundColor")]
    private static extern int GetRunPropertyForegroundColorCore(RunPropertyId runPropertyId, IntPtr alpha, IntPtr red, IntPtr green,
        IntPtr blue);

    public static unsafe int GetRunPropertyForegroundColor(RunPropertyId runPropertyId, out byte alpha, out byte red, out byte green,
        out byte blue)
    {
        alpha = 0;
        red = 0;
        green = 0;
        blue = 0;
        fixed (byte* pAlpha = &alpha)
        fixed (byte* pRed = &red)
        fixed (byte* pGreen = &green)
        fixed (byte* pBlue = &blue)
        {
            return GetRunPropertyForegroundColorCore(runPropertyId, new IntPtr(pAlpha), new IntPtr(pRed),
                new IntPtr(pGreen), new IntPtr(pBlue));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyBackgroundColor")]
    private static extern int GetRunPropertyBackgroundColorCore(RunPropertyId runPropertyId, IntPtr alpha, IntPtr red, IntPtr green,
        IntPtr blue);

    public static unsafe int GetRunPropertyBackgroundColor(RunPropertyId runPropertyId, out byte alpha, out byte red,
        out byte green, out byte blue)
    {
        alpha = 0;
        red = 0;
        green = 0;
        blue = 0;
        fixed (byte* pAlpha = &alpha)
        fixed (byte* pRed = &red)
        fixed (byte* pGreen = &green)
        fixed (byte* pBlue = &blue)
        {
            return GetRunPropertyBackgroundColorCore(runPropertyId, new IntPtr(pAlpha), new IntPtr(pRed),
                new IntPtr(pGreen), new IntPtr(pBlue));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyStretch")]
    private static extern int GetRunPropertyStretchCore(RunPropertyId runPropertyId, IntPtr stretch);

    public static unsafe int GetRunPropertyStretch(RunPropertyId runPropertyId, out int stretch)
    {
        stretch = 0;
        fixed (int* p = &stretch)
        {
            return GetRunPropertyStretchCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyFontWeight")]
    private static extern int GetRunPropertyFontWeightCore(RunPropertyId runPropertyId, IntPtr fontWeight);

    public static unsafe int GetRunPropertyFontWeight(RunPropertyId runPropertyId, out int fontWeight)
    {
        fontWeight = 0;
        fixed (int* p = &fontWeight)
        {
            return GetRunPropertyFontWeightCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyFontStyle")]
    private static extern int GetRunPropertyFontStyleCore(RunPropertyId runPropertyId, IntPtr fontStyle);

    public static unsafe int GetRunPropertyFontStyle(RunPropertyId runPropertyId, out int fontStyle)
    {
        fontStyle = 0;
        fixed (int* p = &fontStyle)
        {
            return GetRunPropertyFontStyleCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyIsBold")]
    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    private static extern int GetRunPropertyIsBoldCore(RunPropertyId runPropertyId, IntPtr isBold);

    [Obsolete("更加正确的用法应该是直接设置 FontWeight 属性")]
    public static unsafe int GetRunPropertyIsBold(RunPropertyId runPropertyId, out int isBold)
    {
        isBold = 0;
        fixed (int* p = &isBold)
        {
            return GetRunPropertyIsBoldCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyIsItalic")]
    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    private static extern int GetRunPropertyIsItalicCore(RunPropertyId runPropertyId, IntPtr isItalic);

    [Obsolete("更加正确的用法应该是直接设置 FontStyle 属性")]
    public static unsafe int GetRunPropertyIsItalic(RunPropertyId runPropertyId, out int isItalic)
    {
        isItalic = 0;
        fixed (int* p = &isItalic)
        {
            return GetRunPropertyIsItalicCore(runPropertyId, new IntPtr(p));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyFontVariant")]
    private static extern int GetRunPropertyFontVariantCore(RunPropertyId runPropertyId, IntPtr fontVariant,
        IntPtr baselineProportion);

    public static unsafe int GetRunPropertyFontVariant(RunPropertyId runPropertyId, out byte fontVariant,
        out double baselineProportion)
    {
        fontVariant = 0;
        baselineProportion = 0;

        fixed (byte* pFontVariant = &fontVariant)
        fixed (double* pBaseline = &baselineProportion)
        {
            return GetRunPropertyFontVariantCore(runPropertyId, new IntPtr(pFontVariant), new IntPtr(pBaseline));
        }
    }

    [DllImport(DllName, EntryPoint = "GetRunPropertyDecorationFlags")]
    private static extern int GetRunPropertyDecorationFlagsCore(RunPropertyId runPropertyId, IntPtr decorationFlags);

    public static unsafe int GetRunPropertyDecorationFlags(RunPropertyId runPropertyId, out int decorationFlags)
    {
        decorationFlags = 0;
        fixed (int* p = &decorationFlags)
        {
            return GetRunPropertyDecorationFlagsCore(runPropertyId, new IntPtr(p));
        }
    }

    #endregion
}