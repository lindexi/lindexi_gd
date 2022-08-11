namespace PptxCore;

/// <summary>
///     表示图表的数值
/// </summary>
public interface INumericChartValue : IChartValue
{
    /// <summary>
    ///     获取数值
    /// </summary>
    /// <returns></returns>
    double GetValue();
}