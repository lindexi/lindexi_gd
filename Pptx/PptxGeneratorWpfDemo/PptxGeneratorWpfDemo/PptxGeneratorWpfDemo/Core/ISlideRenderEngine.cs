using System.Collections.Generic;
using System.Windows.Media;

namespace PptxGenerator;

/// <summary>
/// 渲染引擎接口，包含全部 WPF 依赖：文本测量、图片加载、DrawingContext 绘制。
/// </summary>
internal interface ISlideRenderEngine
{
    /// <summary>
    /// 执行框架相关测量（PreMeasure 阶段）：文本度量、图片加载。
    /// 返回以元素 Id 为键的测量结果字典。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文。</param>
    /// <param name="warnings">警告收集列表。</param>
    /// <returns>测量结果字典。</returns>
    Dictionary<string, SlideMeasureResult> PreMeasure(SlidePage page, SlideRenderContext context, List<string> warnings);

    /// <summary>
    /// 执行最终渲染（Render 阶段），将页面绘制到 <see cref="DrawingContext"/>。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="dc">WPF DrawingContext。</param>
    /// <param name="context">渲染上下文。</param>
    /// <param name="warnings">警告收集列表。</param>
    void Render(SlidePage page, DrawingContext dc, SlideRenderContext context, List<string> warnings);

    /// <summary>
    /// 渲染错误预览图。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="context">渲染上下文。</param>
    /// <returns>错误预览位图。</returns>
    System.Windows.Media.Imaging.RenderTargetBitmap RenderErrorPreview(string message, SlideRenderContext context);
}
