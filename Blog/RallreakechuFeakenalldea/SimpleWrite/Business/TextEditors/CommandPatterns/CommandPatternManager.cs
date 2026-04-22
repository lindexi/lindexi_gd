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
        _commandPatternList.Add(commandPattern);
    }

    public void AddCommandPattern(string title, Func<string, Task> doFunc, bool supportSingleLine = true, Func<string, ValueTask<bool>>? isMatchFunc = null)
    {
        var delegateCommandPattern = new DelegateCommandPattern(title, doFunc, supportSingleLine, isMatchFunc);
        _commandPatternList.Add(delegateCommandPattern);
    }
}