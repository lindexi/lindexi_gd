namespace LightTextEditorPlus.Editing;

/// <summary>
/// 将命令名称与按键手势关联。
/// Associates a command name with a key gesture.
/// </summary>
/// <param name="CommandName">The name of the command.</param>
/// <param name="KeyGesture">The key gesture that triggers the command.</param>
public readonly record struct TextEditorShortCutKeyBinding(TextEditorCommandName CommandName, TextEditorKeyGesture KeyGesture)
{
}