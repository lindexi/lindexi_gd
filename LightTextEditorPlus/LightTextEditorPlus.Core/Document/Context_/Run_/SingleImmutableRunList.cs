using LightTextEditorPlus.Core.Exceptions;

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
            throw new TextEditorInnerException($"获取只有单个 Run 的 {nameof(SingleImmutableRunList)} 时，传入的 {nameof(index)} 参数是 {index} 而不是 0 的值");
        }

        return Run;
    }
}