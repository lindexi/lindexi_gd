using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWrite.Business.ShortcutManagers;

/// <summary>
/// 快捷方式和命令的管理器
/// </summary>
internal class ShortcutManager
{
    public ShortcutManager()
    {
    }

    private readonly List<ShortcutCommand> _commands = new();

    private readonly List<ShortcutKeyBind> _keyBinds = new();

    public void AddKeyBind(ShortcutKeyBind keyBind)
    {
        if (keyBind == null) throw new ArgumentNullException(nameof(keyBind));
        _keyBinds.Add(keyBind);
    }

    /// <summary>
    /// 添加一个快捷方式命令
    /// </summary>
    /// <param name="command">快捷方式命令</param>
    public void AddCommand(ShortcutCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        _commands.Add(command);
    }

    /// <summary>
    /// 获取所有的快捷方式命令
    /// </summary>
    /// <returns>所有的快捷方式命令</returns>
    public IReadOnlyList<ShortcutCommand> GetCommands()
    {
        return _commands;
    }

    public ShortcutCommand? GetCommand(string name)
    {
        return _commands.FirstOrDefault(t => t.Name == name);
    }

    public IReadOnlyList<ShortcutKeyBind> GetKeyBinds()
    {
        return _keyBinds;
    }
}