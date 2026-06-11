using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// 包装 PreMeasure 阶段产出的元素测量结果，提供类型安全的查询方法。
/// </summary>
public sealed class SlideElementMeasurements
{
    private readonly Dictionary<string, SlideMeasureResult> _measurements;

    /// <summary>
    /// 初始化 <see cref="SlideElementMeasurements"/> 的新实例。
    /// </summary>
    /// <param name="measurements">以元素 Id 为键的测量结果字典。</param>
    public SlideElementMeasurements(Dictionary<string, SlideMeasureResult> measurements)
    {
        _measurements = measurements ?? throw new System.ArgumentNullException(nameof(measurements));
    }

    /// <summary>
    /// 尝试获取指定元素 Id 的测量结果。
    /// </summary>
    /// <param name="elementId">元素 Id。</param>
    /// <param name="result">测量结果。</param>
    /// <returns>如果找到则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool TryGetValue(string elementId, out SlideMeasureResult result)
        => _measurements.TryGetValue(elementId, out result);

    /// <summary>
    /// 查找指定元素 Id 的测量结果。
    /// </summary>
    /// <param name="elementId">元素 Id。</param>
    /// <returns>测量结果；若未找到则返回 <see langword="null"/>。</returns>
    public SlideMeasureResult? Find(string elementId)
        => _measurements.TryGetValue(elementId, out var r) ? r : null;
}
