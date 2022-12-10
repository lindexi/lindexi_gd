using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.Tests;

class TestRenderManager : IRenderManager
{
    public TestRenderManager(Action<RenderInfoProvider>? renderAction = null)
    {
        _renderAction = renderAction;
    }

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        _renderAction?.Invoke(renderInfoProvider);

        // 实际使用中，可不要缓存起来这个对象哦
        CurrentRenderInfoProvider = renderInfoProvider;
        RenderCount++;
    }

    public int RenderCount { set; get; }
    public RenderInfoProvider? CurrentRenderInfoProvider { set; get; }
    private readonly Action<RenderInfoProvider>? _renderAction;
}