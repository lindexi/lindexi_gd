using Avalonia.Input;

namespace SimpleWrite.Business.ShortcutManagers;

public record ShortcutKeyBind(KeyModifiers Modifiers, Key Key, string CommandName);