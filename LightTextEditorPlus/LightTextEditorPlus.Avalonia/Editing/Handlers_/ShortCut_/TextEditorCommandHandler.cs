using System;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 文本编辑器快捷键使用的命令处理程序委托类型。
/// Delegate type for command handlers used by the text editor shortcuts.
/// </summary>
/// <param name="sender">The sender of the command invocation.</param>
/// <param name="e">Event arguments for the invocation.</param>
public delegate void TextEditorCommandHandler(object sender, EventArgs e);