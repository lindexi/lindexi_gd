using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;

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
    public IEnumerable<ShortcutCommand> GetCommands()
    {
        return _commands.AsReadOnly();
    }

    public ShortcutCommand? GetCommand(string name)
    {
        return _commands.FirstOrDefault(t => t.Name == name);
    }
}

public record ShortcutKeyBind(KeyModifiers Modifiers, Key Key, ShortcutCommand Command);

public class ShortcutExecuteContext
{
}

public class ShortcutCommand
{
    public ShortcutCommand(string name, Action<ShortcutExecuteContext> command)
    {
        Name = name;
        Command = command;
    }

    /// <summary>
    /// 快捷方式的名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 执行的命令
    /// </summary>
    public Action<ShortcutExecuteContext> Command { get;}
}