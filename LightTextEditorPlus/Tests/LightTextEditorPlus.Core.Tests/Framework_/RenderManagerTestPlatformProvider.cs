using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests;

class RenderManagerTestPlatformProvider : TestPlatformProvider
{
    public RenderManagerTestPlatformProvider()
    {
        RenderManager = new TestRenderManager();
    }

    public TestRenderManager TestRenderManager
    {
        set => RenderManager = value;
        get => (TestRenderManager) RenderManager!;
    }
}