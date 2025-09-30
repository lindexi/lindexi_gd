namespace LightTextEditorPlus.Editing;

/// <summary>
/// 将命令名称与其处理程序关联。
/// Associates a command name with its handler.
/// </summary>
/// <param name="CommandName">The name of the command.</param>
/// <param name="Handler">The handler to execute for the command.</param>
public readonly record struct TextEditorShortCutCommandBinding(TextEditorCommandName CommandName, TextEditorCommandHandler Handler)
{
}