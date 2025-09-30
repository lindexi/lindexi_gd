namespace LightTextEditorPlus.Editing;

/// <summary>
/// 表示文本编辑器使用的命令标识。
/// Represents the identity of a command used by the text editor.
/// </summary>
/// <param name="Name">The string identifier of the command.</param>
public readonly record struct TextEditorCommandName(string Name);