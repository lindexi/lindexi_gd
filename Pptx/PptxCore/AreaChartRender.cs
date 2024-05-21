using System.Globalization;

using DocumentFormat.OpenXml.Drawing;

using dotnetCampus.OpenXmlUnitConverter;

using Microsoft.Maui.Graphics;

namespace PptxCore;

public class AreaChartRender
{
    public AreaChartRender(AreaChartRenderContext context)
    {
        Context = context;
    }

    public AreaChartRenderContext Context { get; }

    public void Render(ICanvas canvas)
    {
        var chartWidth = (float) Context.Width.Value;
        var chartHeight = (float) Context.Height.Value;

        // 图表标题
        float chartTitleHeight = 52;
        //canvas.Font = new Font("Microsoft Yahei");
        canvas.FontColor = Colors.Black;
        canvas.FontSize = (float) new Pound(18).ToPixel().Value;
        //var chartTitle = "图表标题"; // 待填充
        //canvas.DrawString(chartTitle, x: 0, y: 0, width: chartWidth, height: chartTitleHeight,
        //    horizontalAlignment: HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Top);

        // 图例高度，图例是放在最下方
        float chartLegendHeight = 42;
        // 类别信息的高度
        float axisValueHeight = 20;

        float yAxisLeftMargin = 42;
        float yAxisRightMargin = 42;
        float xAxisBottomMargin = 25;

        var plotAreaOffsetX = yAxisLeftMargin;
        var plotAreaOffsetY = chartTitleHeight;
        var plotAreaWidth = chartWidth - yAxisLeftMargin - yAxisRightMargin;
        var plotAreaHeight = chartHeight - chartTitleHeight - chartLegendHeight - xAxisBottomMargin;

        // void CreateCoordinate()
        // 绘制坐标系

        // 先找到 Y 轴的刻度，找到最大值

        // 有多少条行的线，保持和 PPT 相同
        var rowLineCount = 8;
        // 获取数据最大值
        var maxData = GetMaxValue();
        // 获取刻度的值
        var ratio = GetRatio(maxData, rowLineCount);
        var maxValue = ratio * (rowLineCount - 1);

        // 绘制网格线，先绘制 Y 轴，再绘制 X 轴
        // 绘制 Y 轴的刻度和 x 行线
        for (var i = 0; i < rowLineCount; i++)
        {
            canvas.StrokeSize = 2;
            canvas.StrokeColor = Colors.Gray;

            var offsetX = plotAreaOffsetX;
            var offsetY = plotAreaOffsetY + plotAreaHeight - plotAreaHeight / (rowLineCount - 1) * i;

            canvas.DrawLine(offsetX, offsetY, offsetX + plotAreaWidth, offsetY);

            // 获取刻度的值
            var fontSize = 16f;
            canvas.FontSize = fontSize;
            var textRightMargin = 5;
            var textX = 0;
            var textY = offsetY - fontSize / 2f;
            var textWidth = plotAreaOffsetX - textX - textRightMargin;
            var textHeight = 25;
            var value = (ratio * i).ToString(CultureInfo.CurrentCulture);
            canvas.DrawString(value, textX, textY, textWidth, textHeight, HorizontalAlignment.Right,
                VerticalAlignment.Top);
        }

        // 绘制 X 轴，绘制类别信息
        // 获取列项，无论拿哪个数据都应该是相同的数量，因此只取第0个

        var categoryAxisValueList = Context.CategoryAxisValueList.ValueList;
        for (var i = 0; i < categoryAxisValueList.Count; i++)
        {
            var offsetX = plotAreaOffsetX + plotAreaWidth * i / (categoryAxisValueList.Count - 1);
            var offsetY = plotAreaOffsetY + plotAreaHeight;

            var textX = offsetX - 20;
            var textY = offsetY + xAxisBottomMargin;
            if (i < categoryAxisValueList.Count)
            {
                var text = categoryAxisValueList[i].GetViewText();
                canvas.DrawString(text, textX, textY, HorizontalAlignment.Left);
            }
        }

        // void DrawArea()
        // 绘制内容
        for (var chartDataIndex = 0; chartDataIndex < Context.AreaChartSeriesInfoList.Count; chartDataIndex++)
        {
            var chartSeriesInfo = Context.AreaChartSeriesInfoList[chartDataIndex];
            if (chartSeriesInfo.ChartValueList is null)
            {
                continue;
            }

            using var path = new PathF();
            var startX = plotAreaOffsetX;
            var startY = plotAreaOffsetY + plotAreaHeight;
            path.Move(startX, startY);

            for (var i = 0; i < chartSeriesInfo.ChartValueList.ValueList.Count; i++)
            {
                var value = chartSeriesInfo.ChartValueList.ValueList[i];

                if (value is NumericChartValue numericChartValue)
                {
                    var offsetX = plotAreaOffsetX + plotAreaWidth * i / (categoryAxisValueList.Count - 1);
                    var offsetY = plotAreaOffsetY + plotAreaHeight -
                                  numericChartValue.GetValue() / maxValue * plotAreaHeight;

                    path.LineTo(offsetX, (float) offsetY);
                }
            }

            path.LineTo(plotAreaOffsetX + plotAreaWidth, plotAreaOffsetY + plotAreaHeight)
                .LineTo(startX, startY)
                .Close();

            if (chartDataIndex < Context.AreaChartSeriesInfoList.Count)
            {
                // 在这份课件里，一定是纯色
                var (success, a, r, g, b) =
                    BrushCreator.ConvertToColor(chartSeriesInfo.FillBrush!.GetFill<SolidFill>()!.RgbColorModelHex!
                        .Val!);

                var color = new Color(r, g, b, a);
                canvas.FillColor = color;
            }

            canvas.FillPath(path);
        }

        //// 绘制图例内容
        //// 内容如 系列1 系列2
        //// 默认是 16 字号
        //var seriesTitleText = string.Join(" ", Context.AreaChartSeriesInfoList.Select(t => t.SeriesTitleText.Text));
        //canvas.FontSize = 16;

        //var chartLegendOffsetX = 0;
        //var chartLegendOffsetY = plotAreaOffsetY + plotAreaHeight + axisValueHeight;
        //var chartLegendWidth = chartWidth - yAxisLeftMargin - yAxisRightMargin;

        //canvas.DrawString(seriesTitleText, chartLegendOffsetX, chartLegendOffsetY, chartLegendWidth, chartLegendHeight,
        //    HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    ///     获取坐标系下，一格代表多少数值
    /// </summary>
    private static double GetRatio(double maxDataNum, int count)
    {
        var countN = count - 1;
        //获取maxDataNum对应的最大位数字，以及位数
        var result = GetNumOfMax(maxDataNum);
        if (result.temp - (int) result.temp > 0)
        {
            if (maxDataNum < 100)
            {
                return (int) maxDataNum / countN + 1;
            }

            //第二位
            var second = (int) ((result.temp - (int) result.temp) * 10);
            var mx = (int) result.temp * Math.Pow(10, result.count) + (second + 1) * Math.Pow(10, result.count - 1);
            return mx / countN;
        }

        return maxDataNum / countN;
    }

    /// <summary>
    ///     获取数字中最大的数值代表的值，比如120最大位为1（百位）。
    ///     0.21最大位为2
    /// </summary>
    /// <returns></returns>
    private static (double temp, int count) GetNumOfMax(double maxDataNum)
    {
        if (maxDataNum <= 0)
        {
            throw new ArgumentException("参数必须为正数");
        }

        var temp = maxDataNum;
        //位数，即10的次方数
        var count = 0;
        if (maxDataNum > 1)
        {
            while (temp >= 10)
            {
                temp = temp / 10;
                count += 1;
            }

            return (temp, count);
        }

        while (temp <= 1)
        {
            temp = temp * 10;
            count -= 1;
        }

        return (temp, count);
    }

    private double GetMaxValue()
    {
        double maxValue = 0;
        foreach (var chartSeriesInfo in Context.AreaChartSeriesInfoList)
        {
            if (chartSeriesInfo.ChartValueList != null)
            {
                foreach (var chartValue in chartSeriesInfo.ChartValueList.ValueList)
                {
                    if (chartValue is NumericChartValue numericChartValue)
                    {
                        maxValue = Math.Max(maxValue, numericChartValue.GetValue());
                    }
                }
            }
        }

        return maxValue;
    }
}