using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Editing;

public static class TextFeaturesExtension
{
    public static TextFeatures EnableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
       return currentFeatures | features;
    }

    public static TextFeatures DisableFeatures(this TextFeatures currentFeatures, TextFeatures features)
    {
        return currentFeatures & ~features;
    }

    public static bool IsFeaturesEnable(this TextFeatures currentFeatures, TextFeatures features)
    {
        return (currentFeatures & features) == features;
    }
}

[Flags]
public enum TextFeatures
{
    Editable = 1 << 1,
    AlignVerticalTop = 1 << 2,
    AlignVerticalCenter = 1 << 3,
    AlignVerticalBottom = 1 << 4,
    AlignVertical = AlignVerticalTop | AlignVerticalCenter | AlignVerticalBottom,

    IncreaseFontSize = 1 << 5,
    DecreaseFontSize = 1 << 6,
    SetFontSize = 1 << 7,

    SetFontName = 1 << 8,
    SetForeground = 1 << 9,
    SetUnderline = 1 << 10,
    SetBold = 1 << 11,
    SetStriketh = 1 << 12,
    SetItalic = 1 << 13,

    SetFontSubscript = 1 << 14,
    SetFontSuperscript = 1 << 15,

    SetLineSpacing = 1 << 16,
    DecreaseIndentation = 1 << 17,
    IncreaseIndentation = 1 << 18,
    SetIndentation = 1 << 19,

    AlignHorizontalLeft = 1 << 20,
    AlignHorizontalCenter = 1 << 21,
    AlignHorizontalRight = 1 << 22,
    AlignJustify = 1 << 23,
    HorizontalAlign = AlignHorizontalLeft | AlignHorizontalCenter | AlignHorizontalRight | AlignJustify,

    SetParagraphSpaceBefore = 1 << 24,
    SetParagraphSpaceAfter = 1 << 25,

    ChangeArrangingType = 1 << 26,

    CopyEnable = 1 << 27,
    CutEnable = 1 << 28,
    PasteEnable = 1 << 29,

    SelectionEnable = 1 << 30,
}
