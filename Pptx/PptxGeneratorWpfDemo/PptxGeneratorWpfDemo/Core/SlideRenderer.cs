using System;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// 页面渲染器（已过时，请使用 <see cref="SlideRenderPipeline"/>）。
/// 保留为兼容层，内部委托给 <see cref="SlideRenderPipeline"/>。
/// </summary>
[Obsolete("请使用 SlideRenderPipeline 替代。SlideRenderer 将在未来版本中移除。")]
public class SlideRenderer
{
    [Obsolete("请使用 SlideRenderContext.DefaultCanvasWidth 替代。")]
    public const int CanvasWidth = SlideRenderContext.DefaultCanvasWidth;

    [Obsolete("请使用 SlideRenderContext.DefaultCanvasHeight 替代。")]
    public const int CanvasHeight = SlideRenderContext.DefaultCanvasHeight;

    private readonly SlideRenderPipeline _pipeline;

    /// <summary>
    /// 初始化 <see cref="SlideRenderer"/> 的新实例。
    /// </summary>
    public SlideRenderer()
    {
        _pipeline = new SlideRenderPipeline();
    }

    /// <summary>
    /// 将 SlideML 渲染为预览图，并返回回填后的 XML 与警告信息。
    /// </summary>
    public Task<SlideRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        return _pipeline.RenderAsync(slideXml, cancellationToken);
    }
}
