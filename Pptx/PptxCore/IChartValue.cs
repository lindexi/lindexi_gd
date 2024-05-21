namespace PptxCore;

/// <summary>
///     表示图表的一个值
/// </summary>
public interface IChartValue
{
    /// <summary>
    ///     获取界面显示文本
    /// </summary>
    /// <returns></returns>
    string? GetViewText();
}