using Avalonia.Input;

namespace SimpleWrite.Business.ShortcutManagers;

/// <summary>
/// 快捷键执行器
/// </summary>
internal class ShortcutExecutor
{
    /// <summary>
    /// 尝试处理当前快捷键
    /// </summary>
    /// <param name="keyEventArgs"></param>
    /// <returns>返回 true 表示当前输入被当成快捷键处理了</returns>
    public bool Handle(KeyEventArgs keyEventArgs)
    {
        foreach (var shortcutKeyBind in ShortcutManager.GetKeyBinds())
        {
            if (shortcutKeyBind.Modifiers == keyEventArgs.KeyModifiers)
            {
                if (shortcutKeyBind.Key == keyEventArgs.Key)
                {
                    var command = ShortcutManager.GetCommand(shortcutKeyBind.CommandName);
                    command?.Command(new ShortcutExecuteContext());
                    return true;
                }
            }
        }

        return false;
    }

    public required ShortcutManager ShortcutManager { get; init; }
}