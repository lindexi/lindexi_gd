using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// 布局引擎接口，负责纯数学计算（坐标、尺寸、对齐），无 UI 框架依赖。
/// </summary>
internal interface ISlideLayoutEngine
{
    /// <summary>
    /// 使用声明尺寸进行临时布局（PreLayout 阶段）。
    /// 文本和图片使用声明的 Width/Height，若未声明则使用默认值。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸）。</param>
    /// <param name="warnings">警告收集列表。</param>
    void PreLayout(SlidePage page, SlideRenderContext context, List<string> warnings);

    /// <summary>
    /// 使用 PreMeasure 阶段获得的实测值进行最终布局（FinalLayout 阶段）。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="context">渲染上下文（含画布尺寸）。</param>
    /// <param name="measurements">PreMeasure 阶段产出的测量结果，以元素 Id 为键。</param>
    /// <param name="warnings">警告收集列表。</param>
    void FinalLayout(SlidePage page, SlideRenderContext context, IReadOnlyDictionary<string, SlideMeasureResult> measurements, List<string> warnings);

    /// <summary>
    /// 在页面中查找指定 Id 元素的渲染指标。
    /// </summary>
    /// <param name="page">页面数据模型。</param>
    /// <param name="id">元素 Id。</param>
    /// <returns>渲染指标；若未找到则返回 null。</returns>
    SlideRenderedMetrics? FindMetrics(SlidePage page, string id);
}
