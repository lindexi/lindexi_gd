using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Rendering;

/// <summary>
/// 布局引擎接口，负责纯数学计算（坐标、尺寸、对齐），无 UI 框架依赖。
/// 使用 <see cref="SlideMlRect"/>、<see cref="SlideMlPoint"/> 等纯数据类型。
/// </summary>
public interface ISlideMlLayoutEngine
{
    /// <summary>
    /// 使用声明尺寸进行临时布局（PreLayout 阶段）。
    /// 文本和图片使用声明的 Width/Height，若未声明则使用默认值。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    void PreLayout(SlideMlPage page, SlideMlPipelineContext context);

    /// <summary>
    /// 使用 PreMeasure 阶段获得的实测值进行最终布局（FinalLayout 阶段）。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸和警告收集）。</param>
    /// <param name="measurements">PreMeasure 阶段产出的测量结果。</param>
    void FinalLayout(SlideMlPage page, SlideMlPipelineContext context, SlideMlElementMeasurements measurements);
}
