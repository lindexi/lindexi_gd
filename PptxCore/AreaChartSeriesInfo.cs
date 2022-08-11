using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Flatten.ElementConverters;

namespace PptxCore;

/// <summary>
///     面积图的系列信息
/// </summary>
/// 后续也许会作为图表系列信息，不是只给面积图使用
public class AreaChartSeriesInfo
{
    /// <summary>
    ///     创建面积图的系列信息
    /// </summary>
    /// <param name="areaChartSeries"></param>
    /// <param name="context"></param>
    public AreaChartSeriesInfo(AreaChartSeries areaChartSeries, AreaChartRenderContext context)
    {
        SeriesTitleText = SeriesTitleText.ToSeriesTitleText(areaChartSeries.SeriesText, context);

        if (areaChartSeries.ChartShapeProperties is not null)
        {
            FillBrush = BrushHelper.BuildBrush(areaChartSeries.ChartShapeProperties.ChildElements, null, null, null);
        }

        var values = areaChartSeries.GetFirstChild<Values>();
        if (values is not null)
        {
            ChartValueList = ChartValueList.GetChartValueList(values, context);
        }
    }

    /// <summary>
    ///     值列表
    /// </summary>
    public ChartValueList? ChartValueList { get; }

    /// <summary>
    ///     系列填充颜色
    /// </summary>
    public BrushFill? FillBrush { get; }

    /// <summary>
    ///     系列标题内容
    /// </summary>
    public SeriesTitleText SeriesTitleText { get; }
}