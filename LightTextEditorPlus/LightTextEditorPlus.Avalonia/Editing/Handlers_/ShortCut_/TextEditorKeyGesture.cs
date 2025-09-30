using Avalonia.Input;

namespace LightTextEditorPlus.Editing;

/// <summary>
/// 表示由按键和修饰键组成的键盘手势。
/// Represents a keyboard gesture consisting of a key and modifier keys.
/// </summary>
/// <param name="Key">The primary key of the gesture.</param>
/// <param name="Modifiers">Modifier keys (Ctrl, Alt, Shift, etc.).</param>
public readonly record struct TextEditorKeyGesture(Key Key, KeyModifiers Modifiers);