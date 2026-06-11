using System.Windows.Media;

namespace PptxGenerator;

/// <summary>
/// 渲染引擎接口，包含全部 WPF 依赖：文本测量、图片加载、DrawingContext 绘制。
/// </summary>
internal interface ISlideRenderEngine
{
    /// <summary>
    /// 执行框架相关测量（PreMeasure 阶段）：文本度量、图片加载。
    /// 返回包装后的测量结果。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    /// <returns>测量结果。</returns>
    SlideElementMeasurements PreMeasure(SlidePage page, SlidePipelineContext context);

    /// <summary>
    /// 执行最终渲染（Render 阶段），将页面绘制到 <see cref="DrawingContext"/>。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="dc">WPF DrawingContext。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    void Render(SlidePage page, DrawingContext dc, SlidePipelineContext context);

    /// <summary>
    /// 渲染错误预览图。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="context">渲染上下文。</param>
    /// <returns>错误预览位图。</returns>
    System.Windows.Media.Imaging.RenderTargetBitmap RenderErrorPreview(string message, SlidePipelineContext context);
}
