using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Tests;

public class TestPlatformProvider : IPlatformProvider
{
    public void RequireDispatchUpdateLayout(Action textLayout)
    {
        textLayout();
    }

    public ITextLogger? BuildTextLogger()
    {
        return null;
    }
}