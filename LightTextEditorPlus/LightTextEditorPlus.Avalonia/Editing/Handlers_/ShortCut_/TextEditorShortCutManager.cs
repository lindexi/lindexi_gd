namespace LightTextEditorPlus.Editing;

/// <summary>
/// 管理文本编辑器的键盘快捷键以及它们对应的命令处理程序。
/// Manages keyboard shortcuts (key bindings) and their associated command handlers
/// for the text editor.
/// </summary>
public class TextEditorShortCutManager
{
    /// <summary>
    /// 将命令及其按键手势与处理程序一起添加到管理器中。
    /// Adds a command with its key gesture and handler to the manager.
    /// </summary>
    /// <param name="commandName">The name of the command to add.</param>
    /// <param name="keyGesture">The key gesture that triggers the command.</param>
    /// <param name="handler">The handler to invoke when the command is executed.</param>
    public void Add(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture, TextEditorCommandHandler handler)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    /// <summary>
    /// 添加已构造好的按键绑定到管理器中。
    /// Adds a pre-constructed key binding to the manager.
    /// </summary>
    /// <param name="keyBinding">The key binding to add.</param>
    public void AddKeyBindings(TextEditorShortCutKeyBinding keyBinding)
    {
        KeyBindings.Add(keyBinding);
    }

    /// <summary>
    /// 根据命令名和按键手势添加按键绑定。
    /// Adds a key binding defined by command name and key gesture.
    /// </summary>
    /// <param name="commandName">The name of the command to bind.</param>
    /// <param name="keyGesture">The key gesture that triggers the command.</param>
    public void AddKeyBindings(TextEditorCommandName commandName, TextEditorKeyGesture keyGesture)
    {
        KeyBindings.Add(new TextEditorShortCutKeyBinding(commandName, keyGesture));
    }

    /// <summary>
    /// 添加已构造好的命令绑定到管理器中。
    /// Adds a pre-constructed command binding to the manager.
    /// </summary>
    /// <param name="commandBinding">The command binding to add.</param>
    public void AddCommandBindings(TextEditorShortCutCommandBinding commandBinding)
    {
        CommandBindings.Add(commandBinding);
    }

    /// <summary>
    /// 根据命令名和处理程序添加命令绑定。
    /// Adds a command binding defined by command name and handler.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="handler">The handler to execute for the command.</param>
    public void AddCommandBindings(TextEditorCommandName commandName, TextEditorCommandHandler handler)
    {
        CommandBindings.Add(new TextEditorShortCutCommandBinding(commandName, handler));
    }

    /// <summary>
    /// 获取此实例管理的命令绑定集合。
    /// Gets the collection of command bindings managed by this instance.
    /// </summary>
    public TextEditorShortCutCommandBindingCollection CommandBindings { get; } = new();

    /// <summary>
    /// 获取此实例管理的按键绑定集合。
    /// Gets the collection of key bindings managed by this instance.
    /// </summary>
    public TextEditorShortCutKeyBindingCollection KeyBindings { get; } = new();

    /// <summary>
    /// 查找与指定按键手势匹配的最后添加的按键绑定；如果未找到则返回 null。
    /// Finds the last-added key binding that matches the specified key gesture.
    /// Returns null if no matching binding is found.
    /// </summary>
    /// <param name="keyGesture">The key gesture to search for.</param>
    /// <returns>The matching key binding or <c>null</c> if not found.</returns>
    public TextEditorShortCutKeyBinding? FindShortCutKeyBinding(in TextEditorKeyGesture keyGesture)
    {
        for (var i = KeyBindings.Count - 1; i >= 0; i--)
        {
            var keyBinding = KeyBindings[i];
            if (keyBinding.KeyGesture == keyGesture)
            {
                return keyBinding;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找与给定按键手势关联的命令绑定；如果存在对应的按键绑定则返回相应的命令绑定，否则返回 null。
    /// Finds the command binding associated with the given key gesture.
    /// If a key binding is found for the gesture, this method returns the
    /// corresponding command binding; otherwise returns <c>null</c>.
    /// </summary>
    /// <param name="keyGesture">The key gesture to look up.</param>
    /// <returns>The matching command binding or <c>null</c> if none exists.</returns>
    public TextEditorShortCutCommandBinding? FindCommandBinding(in TextEditorKeyGesture keyGesture)
    {
        TextEditorShortCutKeyBinding? keyBinding = FindShortCutKeyBinding(in keyGesture);
        if (keyBinding is not null)
        {
            TextEditorCommandName commandName = keyBinding.Value.CommandName;

            foreach (var commandBinding in CommandBindings)
            {
                if (commandBinding.CommandName == commandName)
                {
                    return commandBinding;
                }
            }
        }

        return null;
    }
}