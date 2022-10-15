using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Flatten.Framework.Context;

using dotnetCampus.OpenXmlUnitConverter;

namespace PptxCore;

/// <summary>
///     用来提供图表 面积图 渲染的上下文信息
/// </summary>
/// 先使用 OpenXml 的类型，后续将会替换为存储类型
public class AreaChartRenderContext
{
    public AreaChartRenderContext(ChartSpace chartSpace, SlideContext slideContext, Pixel width, Pixel height)
    {
        // 属性初始化有顺序，逻辑上需要先设置
        ChartSpace = chartSpace;
        SlideContext = slideContext;
        Width = width;
        Height = height;

        // 解析方法请参阅 `docs/OpenXML/图表/dotnet OpenXML 解析 PPT 图表 面积图入门.md` 文档
        /*
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <c:chartSpace>
          <c:chart>
            ...
            <c:plotArea>
              ...
            </c:plotArea>
          </c:chart>
        </c:chartSpace>
         */
        var chart = chartSpace.GetFirstChild<Chart>();
        var plotArea = chart?.GetFirstChild<PlotArea>();
        var areaChart = plotArea?.GetFirstChild<AreaChart>();

        if (areaChart == null)
        {
            // 在这份课件里，一定存在面积图，一定不会进入此分支
            return;
        }

        // 获取到面积图开始获取系列
        foreach (var areaChartChildElement in areaChart.ChildElements)
        {
            // 获取系列
            /*
                <c:plotArea>
                  <c:areaChart>
                    <c:ser>
                      ...
                    </c:ser>
                    <c:ser>
                      ...
                    </c:ser>
                  </c:areaChart>
                </c:plotArea>
             */
            if (areaChartChildElement is AreaChartSeries areaChartSeries)
            {
                // 是否为首个系列，只有首个系列的类别轴上的数据 横坐标轴上的数据有效，需要解析
                var isFirstSeries = CategoryAxisValueList == null;

                if (isFirstSeries)
                {
                    // 类别轴上的数据 横坐标轴上的数据
                    var categoryAxisData = areaChartSeries.GetFirstChild<CategoryAxisData>()!;

                    // 获取引用内容
                    var categoryAxisValueList = ChartValueList.GetChartValueList(categoryAxisData, this);
                    CategoryAxisValueList = categoryAxisValueList;
                }

                // 读取系列的文本等信息
                var areaChartSeriesInfo = new AreaChartSeriesInfo(areaChartSeries, this);
                AreaChartSeriesInfoList.Add(areaChartSeriesInfo);
            }
        }

        // 确保 CategoryAxisValueList 一定存在值
        CategoryAxisValueList ??= new ChartValueList(new List<IChartValue>());
    }

    public ChartSpace ChartSpace { get; }
    public SlideContext SlideContext { get; }
    public Pixel Width { get; }
    public Pixel Height { get; }

    /// <summary>
    ///     类别轴上的数据 横坐标轴上的数据
    /// </summary>
    public ChartValueList CategoryAxisValueList { get; } = null!;

    /// <summary>
    ///     面积图的系列信息集合
    /// </summary>
    public AreaChartSeriesInfoList AreaChartSeriesInfoList { get; } = new();

    /// <summary>
    ///     This element specifies that the chart uses the 1904 date system. If the 1904 date system is used, then all dates
    ///     and times shall be specified as a decimal number of days since Dec. 31, 1903. If the 1904 date system is not
    ///     used, then all dates and times shall be specified as a decimal number of days since Dec. 31, 1899.
    ///     [Date systems in
    ///     Excel](https://support.microsoft.com/en-us/office/date-systems-in-excel-e7fe7167-48a9-4b96-bb53-5612a800b487 )
    /// </summary>
    public bool UseDate1904 => ChartSpace.Date1904?.Val?.Value is true;
}