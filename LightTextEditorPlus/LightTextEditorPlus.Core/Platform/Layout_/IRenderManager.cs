using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.Platform;

public interface IRenderManager
{
    void Render(RenderInfoProvider renderInfoProvider);
}