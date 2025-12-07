using System;

namespace SimpleWrite.Business.ShortcutManagers;

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