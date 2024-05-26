using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace PptxCore;

/// <summary>
///     图表里面表示值列表的类型
/// </summary>
public class ChartValueList
{
    /// <summary>
    ///     创建值列表
    /// </summary>
    /// <param name="valueList"></param>
    public ChartValueList(List<IChartValue> valueList)
    {
        ValueList = valueList;
    }

    /// <summary>
    ///     实际存放的值列表内容
    /// </summary>
    /// 也许可以修改为不可变类型
    public List<IChartValue> ValueList { get; }

    /// <summary>
    ///     从 OpenXML 读取出值列表信息
    /// </summary>
    /// <param name="element"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static ChartValueList GetChartValueList(OpenXmlCompositeElement element, AreaChartRenderContext context)
    {
        foreach (var openXmlElement in element.Elements())
        {
            if (openXmlElement is NumberReference numberReference)
            {
                var chartValueList = new List<IChartValue>();
                // 这个公式表示是从 Excel 哪个数据获取的，获取的方式比较复杂。这里还是先从缓存获取
                var formula = element.GetFirstChild<Formula>();

                // 读取缓存
                var numberingCache = numberReference.GetFirstChild<NumberingCache>();
                if (numberingCache is null)
                {
                    // 理论上不为空
                    continue;
                }

                // 对于数值来说，可视效果需要有格式化
                // +		[0]	"m/d/yyyy"	DocumentFormat.OpenXml.OpenXmlElement {}

                var formatCode = numberingCache.GetFirstChild<FormatCode>();
                var formatCodeText = formatCode?.Text;
                // 如果值是 m/d/yyyy 那自动替换为中文的 yyyy/m/d
                foreach (var numericPoint in numberingCache.Elements<NumericPoint>())
                {
                    var numericPointFormatCode = numericPoint.FormatCode;
                    var numericPointFormatCodeText = numericPointFormatCode?.Value ?? formatCodeText;

                    var numericValueText = numericPoint.NumericValue?.Text;

                    var chartValue = new NumericChartValue(numericValueText, numericPointFormatCodeText,
                        context.UseDate1904);
                    chartValueList.Add(chartValue);
                }

                return new ChartValueList(chartValueList);
            }

            if (openXmlElement is StringReference stringReference)
            {
                var chartValueList = new List<IChartValue>();

                // 这个公式表示是从 Excel 哪个数据获取的，获取的方式比较复杂。这里还是先从缓存获取
                var formula = stringReference.GetFirstChild<Formula>();

                // 读取缓存
                var stringCache = stringReference.GetFirstChild<StringCache>();
                if (stringCache != null)
                {
                    foreach (var stringPoint in stringCache.Elements<StringPoint>())
                    {
                        var text = stringPoint.NumericValue?.Text;
                        var chartValue = new TextChartValue(text);
                        chartValueList.Add(chartValue);
                    }
                }

                return new ChartValueList(chartValueList);
            }
        }

        return new ChartValueList(new List<IChartValue>());
    }
}