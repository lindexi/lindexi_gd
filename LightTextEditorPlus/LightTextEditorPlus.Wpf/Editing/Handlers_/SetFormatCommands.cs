using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 设置格式的命令，用于接受界面命令
/// </summary>
/// todo 改名
public static class SetFormatCommands
{
    /// <summary>
    /// 设置字号命令
    /// </summary>
    public static readonly RoutedUICommand SetFontSizeCommand = new RoutedUICommand();

    /// <summary>
    /// 设置字体名命令
    /// </summary>
    public static readonly RoutedUICommand SetFontNameCommand = new RoutedUICommand();

    /// <summary>
    /// 设置前景色命令
    /// </summary>
    public static readonly RoutedUICommand SetForegroundCommand = new RoutedUICommand();

    /// <summary>
    /// 设置行距命令
    /// </summary>
    public static readonly RoutedUICommand SetLineSpaceCommand = new RoutedUICommand();

    /// <summary>
    /// 设置项目符号命令
    /// </summary>
    public static readonly RoutedUICommand MarkerCommand = new RoutedUICommand();

    /// <summary>
    /// 设置特效命令
    /// </summary>
    public static readonly RoutedUICommand TextEffectsCommand = new RoutedUICommand();

    /// <summary>
    /// 应用格式命令
    /// </summary>
    public static readonly RoutedUICommand ApplyFormatPainterCommand = new RoutedUICommand();

    /// <summary>
    /// 设置垂直向上对齐命令
    /// </summary>
    public static readonly RoutedUICommand AlignTopCommand = new RoutedUICommand();

    /// <summary>
    /// 设置垂直居中对齐命令
    /// </summary>
    public static readonly RoutedUICommand AlignMiddleCommand = new RoutedUICommand();

    /// <summary>
    /// 设置垂直向下对齐命令
    /// </summary>
    public static readonly RoutedUICommand AlignBottomCommand = new RoutedUICommand();

    /// <summary>
    /// 设置还原文本到默认格式命令
    /// </summary>
    public static readonly RoutedUICommand ResetCommand = new RoutedUICommand();

    /// <summary>
    /// 粘贴纯文本命令
    /// </summary>
    public static readonly RoutedUICommand PastePlainTextCommand = new RoutedUICommand();

    /// <summary>
    /// 设置段前距离命令
    /// </summary>
    public static readonly RoutedUICommand SetParagraphSpaceBeforeCommand = new RoutedUICommand();

    /// <summary>
    /// 设置段后距离命令
    /// </summary>
    public static readonly RoutedUICommand SetParagraphSpaceAfterCommand = new RoutedUICommand();

    /// <summary>
    /// 设置布局方式命令
    /// </summary>
    public static readonly RoutedUICommand SetTextArrangingTypeCommand = new RoutedUICommand();

    /// <summary>
    /// 编辑超链接命令
    /// </summary>
    public static readonly RoutedUICommand EditHyperlinkCommand = new RoutedUICommand();

    /// <summary>
    /// 触发超链接命令
    /// </summary>
    public static readonly RoutedUICommand RaiseHyperlinkCommand = new RoutedUICommand();

    /// <summary>
    /// 删除超链接命令
    /// </summary>
    public static readonly RoutedUICommand RemoveHyperlinkCommand = new RoutedUICommand();

    /// <summary>
    /// 加上超链接命令
    /// </summary>
    public static readonly RoutedUICommand AddHyperlinkCommand = new RoutedUICommand();

    /// <summary>
    /// 设置段前方向命令
    /// </summary>
    public static readonly RoutedUICommand SetDirectionCommand = new RoutedUICommand();

    #region 键盘操作

    /// <summary>
    /// 附加的换行命令，banding到Ctrl+Enter和Alt+Enter换行
    /// </summary>
    public static readonly RoutedUICommand AttachingEnterCommand = new RoutedUICommand();

    /// <summary>
    /// 光标向左命令
    /// </summary>
    public static readonly RoutedUICommand MoveLeftCommand = new RoutedUICommand();

    /// <summary>
    /// 光标向右命令
    /// </summary>
    public static readonly RoutedUICommand MoveRightCommand = new RoutedUICommand();

    /// <summary>
    /// 光标向上命令
    /// </summary>
    public static readonly RoutedUICommand MoveUpCommand = new RoutedUICommand();

    /// <summary>
    /// 光标向下命令
    /// </summary>
    public static readonly RoutedUICommand MoveDownCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+向左命令
    /// </summary>
    public static readonly RoutedUICommand ControlLeftCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+向右命令
    /// </summary>
    public static readonly RoutedUICommand ControlRightCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+向上命令
    /// </summary>
    public static readonly RoutedUICommand ControlUpommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+向下命令
    /// </summary>
    public static readonly RoutedUICommand ControlDownCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Shift+向左命令
    /// </summary>
    public static readonly RoutedUICommand ShiftLeftCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Shift+向右命令
    /// </summary>
    public static readonly RoutedUICommand ShiftRightCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Shift+向上命令
    /// </summary>
    public static readonly RoutedUICommand ShiftUpommand = new RoutedUICommand();

    /// <summary>
    /// 光标Shift+向下命令
    /// </summary>
    public static readonly RoutedUICommand ShiftDownCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+Shift+向左命令
    /// </summary>
    public static readonly RoutedUICommand ControlShiftLeftCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+Shift+向右命令
    /// </summary>
    public static readonly RoutedUICommand ControlShiftRightCommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+Shift+向上命令
    /// </summary>
    public static readonly RoutedUICommand ControlShiftUpommand = new RoutedUICommand();

    /// <summary>
    /// 光标Ctrl+Shift+向下命令
    /// </summary>
    public static readonly RoutedUICommand ControlShiftDownCommand = new RoutedUICommand();

    #endregion
}