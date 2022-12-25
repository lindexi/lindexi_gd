using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests;

class RenderManagerTestPlatformProvider : TestPlatformProvider
{
    public override IRenderManager GetRenderManager() => TestRenderManager;

    public TestRenderManager TestRenderManager { set; get; } = new TestRenderManager();
}