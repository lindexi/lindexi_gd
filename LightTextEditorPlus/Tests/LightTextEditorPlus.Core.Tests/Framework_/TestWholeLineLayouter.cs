using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Tests;

class TestWholeLineLayouter : IWholeLineLayouter
{
    public Func<WholeLineLayoutArgument, WholeLineLayoutResult>? LayoutWholeLineFunc { set; get; }

    public WholeLineLayoutResult LayoutWholeLine(in WholeLineLayoutArgument argument)
    {
        TextSize size = new TextSize(10, 10);
        return LayoutWholeLineFunc?.Invoke(argument) ?? new WholeLineLayoutResult(size, size, 1, default);
    }
}
