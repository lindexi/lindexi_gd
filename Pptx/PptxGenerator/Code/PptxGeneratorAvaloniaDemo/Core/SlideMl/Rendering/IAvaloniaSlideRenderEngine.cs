using Avalonia.Media;

namespace PptxGenerator;

/// <summary>
/// 渲染引擎接口，包含全部 Avalonia 依赖：文本测量、图片加载、DrawingContext 绘制。
/// </summary>
internal interface IAvaloniaSlideRenderEngine
{
    /// <summary>
    /// 执行框架相关测量（PreMeasure 阶段）：文本度量、图片加载。
    /// 返回包装后的测量结果。
    /// </summary>
    SlideElementMeasurements PreMeasure(SlidePage page, SlidePipelineContext context);

    /// <summary>
    /// 执行最终渲染（Render 阶段），将页面绘制到 <see cref="IDrawingContextImpl"/>。
    /// </summary>
    void Render(SlidePage page, DrawingContext dc, SlidePipelineContext context);

    /// <summary>
    /// 渲染错误预览图。
    /// </summary>
    Avalonia.Media.Imaging.Bitmap RenderErrorPreview(string message, SlidePipelineContext context);
}