using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Editing;

/// <summary>
/// 功能特性，针对 API 级的功能开关
/// </summary>
[Flags]
public enum TextFeatures : long
{
    /// <summary>
    /// 全部功能
    /// </summary>
    All = ~0,

    /// <summary>
    /// 可编辑。如果禁用，则 API 级的编辑功能将无法使用，即无法进行输入和替换文本内容
    /// </summary>
    Editable = 1L << 1,

    /// <summary>
    /// 垂直对齐-顶部
    /// </summary>
    AlignVerticalTop = 1L << 2,

    /// <summary>
    /// 垂直对齐-中间
    /// </summary>
    AlignVerticalCenter = 1L << 3,

    /// <summary>
    /// 垂直对齐-底部
    /// </summary>
    AlignVerticalBottom = 1L << 4,

    /// <summary>
    /// 垂直对齐
    /// </summary>
    AlignVertical = AlignVerticalTop | AlignVerticalCenter | AlignVerticalBottom,

    /// <summary>
    /// 增加字体大小
    /// </summary>
    IncreaseFontSize = 1L << 5,

    /// <summary>
    /// 减小字体大小
    /// </summary>
    DecreaseFontSize = 1L << 6,

    /// <summary>
    /// 设置字体大小
    /// </summary>
    SetFontSize = 1L << 7,

    /// <summary>
    /// 设置字体名称
    /// </summary>
    SetFontName = 1L << 8,

    /// <summary>
    /// 设置前景色
    /// </summary>
    SetForeground = 1L << 9,

    /// <summary>
    /// 设置下划线
    /// </summary>
    SetUnderline = 1L << 10,

    /// <summary>
    /// 设置加粗
    /// </summary>
    SetBold = 1L << 11,

    /// <summary>
    /// 设置删除线
    /// </summary>
    SetStriketh = 1L << 12,

    /// <summary>
    /// 设置斜体
    /// </summary>
    SetItalic = 1L << 13,

    /// <summary>
    /// 设置下标
    /// </summary>
    SetFontSubscript = 1L << 14,

    /// <summary>
    /// 设置上标
    /// </summary>
    SetFontSuperscript = 1L << 15,

    /// <summary>
    /// 设置行间距
    /// </summary>
    SetLineSpacing = 1L << 16,

    /// <summary>
    /// 减少缩进
    /// </summary>
    DecreaseIndentation = 1L << 17,

    /// <summary>
    /// 增加缩进
    /// </summary>
    IncreaseIndentation = 1L << 18,

    /// <summary>
    /// 设置缩进
    /// </summary>
    SetIndentation = 1L << 19,

    /// <summary>
    /// 水平对齐-左对齐
    /// </summary>
    AlignHorizontalLeft = 1L << 20,

    /// <summary>
    /// 水平对齐-居中
    /// </summary>
    AlignHorizontalCenter = 1L << 21,

    /// <summary>
    /// 水平对齐-右对齐
    /// </summary>
    AlignHorizontalRight = 1L << 22,

    /// <summary>
    /// 水平对齐-两端对齐
    /// </summary>
    AlignJustify = 1L << 23,

    /// <summary>
    /// 水平对齐
    /// </summary>
    HorizontalAlign = AlignHorizontalLeft | AlignHorizontalCenter | AlignHorizontalRight | AlignJustify,

    /// <summary>
    /// 设置段前间距
    /// </summary>
    SetParagraphSpaceBefore = 1L << 24,

    /// <summary>
    /// 设置段后间距
    /// </summary>
    SetParagraphSpaceAfter = 1L << 25,

    /// <summary>
    /// 更改排列类型
    /// </summary>
    ChangeArrangingType = 1L << 26,

    /// <summary>
    /// 复制是否可用
    /// </summary>
    CopyEnable = 1L << 27,

    /// <summary>
    /// 剪切是否可用
    /// </summary>
    CutEnable = 1L << 28,

    /// <summary>
    /// 粘贴是否可用
    /// </summary>
    PasteEnable = 1L << 29,

    /// <summary>
    /// 选择是否可用
    /// </summary>
    SelectionEnable = 1L << 30,

    /// <summary>
    /// 覆盖模式是否可用
    /// </summary>
    /// > Use Insert key to control overtype.
    /// > By https://support.microsoft.com/en-us/office/type-over-text-in-word-for-windows-62c15c48-0936-4902-affe-4cadd71b7038
    OvertypeModeEnable = 1L << 31,

    /// <summary>
    /// 设置行间距配置
    /// </summary>
    SetLineSpacingConfiguration = 1L << 32, // 这是后面补的，只好继续加一了

    /// <summary>
    /// 设置大小自适应内容
    /// </summary>
    SetSizeToContent = 1L << 33,

    /// <summary>
    /// 方向键的移动光标。和直接设置光标坐标位置不同，直接设置光标是无法防的，否则也难以正常实现业务逻辑
    /// </summary>
    CaretMoveTypeEnable = 1L << 34,
}