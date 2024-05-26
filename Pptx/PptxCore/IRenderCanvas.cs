using Microsoft.Maui.Graphics;

namespace PptxCore;

/// <summary>
///     渲染画板
/// </summary>
public interface IRenderCanvas
{
    /// <summary>
    ///     开始进行渲染
    /// </summary>
    /// <param name="action"></param>
    void Render(Action<ICanvas> action);
}