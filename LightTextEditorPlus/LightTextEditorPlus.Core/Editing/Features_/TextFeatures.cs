using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Editing;

/// <summary>
/// 功能特性
/// </summary>
[Flags]
public enum TextFeatures : long
{
    All = ~0,

    Editable = 1L << 1,
    AlignVerticalTop = 1L << 2,
    AlignVerticalCenter = 1L << 3,
    AlignVerticalBottom = 1L << 4,
    AlignVertical = AlignVerticalTop | AlignVerticalCenter | AlignVerticalBottom,

    IncreaseFontSize = 1L << 5,
    DecreaseFontSize = 1L << 6,
    SetFontSize = 1L << 7,

    SetFontName = 1L << 8,
    SetForeground = 1L << 9,
    SetUnderline = 1L << 10,
    SetBold = 1L << 11,
    SetStriketh = 1L << 12,
    SetItalic = 1L << 13,

    SetFontSubscript = 1L << 14,
    SetFontSuperscript = 1L << 15,

    SetLineSpacing = 1L << 16,

    DecreaseIndentation = 1L << 17,
    IncreaseIndentation = 1L << 18,
    SetIndentation = 1L << 19,

    AlignHorizontalLeft = 1L << 20,
    AlignHorizontalCenter = 1L << 21,
    AlignHorizontalRight = 1L << 22,
    AlignJustify = 1L << 23,
    HorizontalAlign = AlignHorizontalLeft | AlignHorizontalCenter | AlignHorizontalRight | AlignJustify,

    SetParagraphSpaceBefore = 1L << 24,
    SetParagraphSpaceAfter = 1L << 25,

    ChangeArrangingType = 1L << 26,

    CopyEnable = 1L << 27,
    CutEnable = 1L << 28,
    PasteEnable = 1L << 29,

    SelectionEnable = 1L << 30,

    /// <summary>
    /// 覆盖模式是否可用
    /// </summary>
    /// > Use Insert key to control overtype.
    /// > By https://support.microsoft.com/en-us/office/type-over-text-in-word-for-windows-62c15c48-0936-4902-affe-4cadd71b7038
    OvertypeModeEnable = 1L << 31,

    SetLineSpacingConfiguration = 1L << 32, // 这是后面补的，只好继续加一了
    SetSizeToContent = 1L << 33,
}
