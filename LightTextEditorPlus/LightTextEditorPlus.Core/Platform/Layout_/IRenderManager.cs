using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 渲染管理器，可以用来在对应的平台上，执行实际的渲染逻辑
/// </summary>
public interface IRenderManager
{
    /// <summary>
    /// 执行渲染。在文本排版布局完成之后，将会立刻调用此方法，让具体的平台执行渲染
    /// </summary>
    /// <param name="renderInfoProvider">文本的渲染信息</param>
    void Render(RenderInfoProvider renderInfoProvider);
}