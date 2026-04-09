using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 只包含一个元素的列表
/// </summary>
class SingleImmutableRunList : IImmutableRunList
{
    public SingleImmutableRunList(IImmutableRun run)
    {
        Run = run;
    }

    public IImmutableRun Run { get; }

    public int CharCount => Run.Count;
    public int RunCount => 1;
    public IImmutableRun GetRun(int index)
    {
        if (index != 0)
        {
            throw new TextEditorInnerException(ExceptionMessages.Format(
                nameof(SingleImmutableRunList) + "_GetRun_IndexMustBeZero", nameof(SingleImmutableRunList),
                nameof(index), index));
        }

        return Run;
    }
}