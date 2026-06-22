using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Rendering;

/// <summary>
/// 渲染引擎接口，完全无 UI 框架依赖。
/// 各 UI 框架（WPF、Avalonia）通过实现此接口来封装各自的渲染逻辑。
/// </summary>
public interface ISlideMlRenderEngine
{
    /// <summary>
    /// 执行框架相关测量（PreMeasure 阶段）：文本度量、图片加载。
    /// 返回包装后的测量结果。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    /// <returns>测量结果。</returns>
    SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context);

    /// <summary>
    /// 执行最终渲染（Render 阶段），生成预览图片。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    /// <returns>渲染预览图片。</returns>
    IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context);

    /// <summary>
    /// 渲染错误预览图。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="context">渲染上下文。</param>
    /// <returns>错误预览图片。</returns>
    IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context);
}
