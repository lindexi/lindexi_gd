using System.Threading.Tasks;

namespace SimpleWrite.Business.TextEditors.CommandPatterns;

public interface ICommandPattern
{
    /// <summary>
    /// 是否支持单个段落
    /// </summary>
    bool SupportSingleLine { get; }

    ValueTask<bool> IsMatchAsync(string text);

    string Title { get; }

    Task DoAsync(string text);
}