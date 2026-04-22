using System;
using System.Threading.Tasks;

namespace SimpleWrite.Business.TextEditors.CommandPatterns;

public class DelegateCommandPattern(string title, Func<string, Task> doFunc, bool supportSingleLine = true, Func<string, ValueTask<bool>>? isMatchFunc = null)
    : ICommandPattern
{
    public bool SupportSingleLine { get; } = supportSingleLine;
    public string Title { get; } = title;
    private Func<string, Task> DoFunc { get; } = doFunc;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        if (isMatchFunc is not null)
        {
            return isMatchFunc(text);
        }

        return new ValueTask<bool>(true);
    }
    public Task DoAsync(string text)
    {
        return DoFunc(text);
    }
}