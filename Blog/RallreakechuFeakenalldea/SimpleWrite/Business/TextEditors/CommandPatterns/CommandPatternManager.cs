using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleWrite.Business.TextEditors.CommandPatterns;

public class CommandPatternManager
{
    public IReadOnlyList<ICommandPattern> CommandPatternList => _commandPatternList;

    private readonly List<ICommandPattern> _commandPatternList = [];

    public void AddCommandPattern(ICommandPattern commandPattern)
    {
        ArgumentNullException.ThrowIfNull(commandPattern);

        int index = _commandPatternList.FindIndex(current => current.Priority < commandPattern.Priority);
        if (index < 0)
        {
            _commandPatternList.Add(commandPattern);
            return;
        }

        _commandPatternList.Insert(index, commandPattern);
    }

    public void AddCommandPattern(string title, Func<string, Task> doFunc, bool supportSingleLine = true,
        Func<string, ValueTask<bool>>? isMatchFunc = null, int priority = 0)
    {
        var delegateCommandPattern = new DelegateCommandPattern(title, doFunc, supportSingleLine, isMatchFunc, priority);
        AddCommandPattern(delegateCommandPattern);
    }
}