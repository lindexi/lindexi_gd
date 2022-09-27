using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Tests;

public class TestPlatformProvider : IPlatformProvider
{
    public void RequireDispatchUpdateLayout(Action textLayout)
    {
        textLayout();
    }
}