using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Tests;

class TestWholeLineLayouter : IWholeLineLayouter
{
    public Func<WholeLineLayoutArgument, WholeLineLayoutResult>? LayoutWholeLineFunc { set; get; }

    public WholeLineLayoutResult LayoutWholeLine(in WholeLineLayoutArgument argument)
    {
        return LayoutWholeLineFunc?.Invoke(argument) ?? new WholeLineLayoutResult(new Size(10, 10), 1);
    }
}