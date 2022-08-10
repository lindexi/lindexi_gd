#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;

using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using PlotArea = DocumentFormat.OpenXml.Drawing.Charts.PlotArea;
using Chart = DocumentFormat.OpenXml.Drawing.Charts.Chart;

namespace Pptx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var file = new FileInfo("Test.pptx");

            using var presentationDocument = PresentationDocument.Open(file.FullName, false);
            var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;

            /*
              <p:cSld>
                <p:spTree>
                  <p:graphicFrame>
                    ...
                  </p:graphicFrame>
                </p:spTree>
              </p:cSld>
             */
            // 获取图表元素，在这份课件里，有一个面积图。以下使用 First 忽略细节，获取图表
            var graphicFrame = slide.Descendants<GraphicFrame>().First();

            /*
                  <p:graphicFrame>
                    <a:graphic>
                      <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/chart">
                        <c:chart xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" r:id="rId2" />
                      </a:graphicData>
                    </a:graphic>
                  </p:graphicFrame>
             */
            // 获取到对应的图表信息，图表是引用的，内容不是放在 Slide 页面里面，而是放在独立的图表 xml 文件里
            var graphic = graphicFrame.Graphic;
            var graphicData = graphic?.GraphicData;
            var chartReference = graphicData?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.ChartReference>();

            // 获取到 id 也就是 `r:id="rId2"` 根据 Relationship 的描述，可以知道去 rels 文件里面获取关联的内容。在 OpenXml SDK 里，封装好了获取方法，获取时需要有两个参数，一个是 id 另一个是去哪里获取的 Part 内容
            var id = chartReference?.Id?.Value;

            // 这份课件一定能找到这个 Id 对应的图表
            Debug.Assert(id != null);

            // 这里需要告诉 OpenXml SDK 去哪里获取资源。详细请看 https://blog.lindexi.com/post/dotnet-OpenXML-%E4%B8%BA%E4%BB%80%E4%B9%88%E8%B5%84%E6%BA%90%E4%BD%BF%E7%94%A8-Relationship-%E5%BC%95%E7%94%A8.html 
            // 如果是放在模版里面，记得要用模版的 Part 去获取
            var currentPart = slide.SlidePart!;

            if (!currentPart.TryGetPartById(id!, out var openXmlPart))
            {
                // 在这份课件里，一定不会进入此分支
                // 一定能从页面找到对应的资源内容也就是图表
                return;
            }

            if (openXmlPart is not ChartPart chartPart)
            {
                // 这里拿到的一定是 ChartPart 对象，一定不会进入此分支。但是在实际项目的代码，还是要做这个判断
                return;
            }

            // 这里的 ChartPart 对应的就是 charts\chartN.xml 文件。这里的 chartN.xml 表示的是 chart1.xml 或 chart2.xml 等文件
            var chartSpace = chartPart.ChartSpace;

            // 这里的 Chart 是 DocumentFormat.OpenXml.Drawing.Charts.Chart 类型，在 OpenXmlSDK 里面，有多个同名的 Chart 类型，还请看具体的命名空间
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

            // 面积图
            /*
               <c:plotArea>
                 <c:areaChart>
                   ...
                 </c:areaChart>
               </c:plotArea>
             */
            // 在 chart 里，有不同的图表类型，例如 BarChart Bar3DChart LineChart PieChart Pie3DChart OfPieChart 不水字数了，就是很多不同的图表
            var areaChart = plotArea?.GetFirstChild<AreaChart>();

            if (areaChart == null)
            {
                // 在这份课件里，一定存在面积图，一定不会进入此分支
                return;
            }

            // 输出到界面的字符
            // 测试代码，别在意字符串拼接性能
            string outputText = $"读取图表：\r\n横坐标轴内容：";

            // 是否第一个系列，第一个系列才输出横坐标信息
            bool firstSeries = true;
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
                if (areaChartChildElement is DocumentFormat.OpenXml.Drawing.Charts.AreaChartSeries areaChartSeries)
                {
                    // 类别轴上的数据 横坐标轴上的数据
                    var categoryAxisData =
                        areaChartSeries.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.CategoryAxisData>()!;
                    // 类别轴上的数据 横坐标轴上的数据 可能是数据，也就是 NumberReference 类型。也可能是字符串，也就是 StringReference 类型。这份课件里面，存放的是 NumberReference 类型，以下代码只演示采用 NumberReference 类型的读取方式，还请在具体项目，自行判断
                    // 以下是 NumberReference 类型读取的例子
                    var categoryAxisDataNumberReference = categoryAxisData
                        .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumberReference>();
                    if (categoryAxisDataNumberReference != null)
                    {
                        // 这个公式表示是从 Excel 哪个数据获取的，获取的方式比较复杂。这里还是先从缓存获取
                        var categoryAxisDataFormula = categoryAxisDataNumberReference
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Formula>();

                        // 读取缓存
                        var categoryAxisDataNumberingCache = categoryAxisDataNumberReference
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumberingCache>()!;
                        // 字符串格式化方式，例如日期方式格式化，可以是空，空表示不需要格式化
                        var formatCodeText = categoryAxisDataNumberingCache.FormatCode?.Text;

                        var list = new List<string>();

                        foreach (var numericPoint in categoryAxisDataNumberingCache
                                     .Elements<DocumentFormat.OpenXml.Drawing.Charts.NumericPoint>())
                        {
                            var numericPointFormatCode = numericPoint.FormatCode;
                            var numericPointFormatCodeText = numericPointFormatCode?.Value ?? formatCodeText;

                            var numericValueText = numericPoint.NumericValue?.Text;

                            var format = numericPointFormatCodeText;
                            if (format == null || format == "m/d/yyyy")
                            {
                                // 如果是空和默认的，转换为中文的。后续可以根据设备的语言，转换为对应的日期
                                format = "yyyy/M/d";
                            }

                            if (numericValueText != null && double.TryParse(numericValueText, out var value))
                            {
                                var days = value;
                                days--; // 不包括当天

                                // 这里只格式化日期的
                                // This element specifies that the chart uses the 1904 date system. If the 1904 date system is used, then all dates
                                // and times shall be specified as a decimal number of days since Dec. 31, 1903. If the 1904 date system is not
                                // used, then all dates and times shall be specified as a decimal number of days since Dec. 31, 1899.
                                // [Date systems in Excel](https://support.microsoft.com/en-us/office/date-systems-in-excel-e7fe7167-48a9-4b96-bb53-5612a800b487 )
                                var useDate1904 = chartSpace.Date1904?.Val?.Value ?? false;
                                if (useDate1904)
                                {
                                    list.Add(new DateTime(1903, 12, 31).AddDays(days).ToString(format));
                                }
                                else
                                {
                                    list.Add(new DateTime(1899, 12, 31).AddDays(days).ToString(format));
                                }
                            }
                            else
                            {
                                list.Add(numericValueText ?? string.Empty);
                            }
                        }

                        if (firstSeries)
                        {
                            outputText += string.Join(',', list) + "\r\n";
                        }
                    }

                    // 获取系列标题，放心，可以不读取 Excel 的内容，通过缓存内容即可。但是缓存内容也许和 Excel 内容不对应
                    /*
                        <c:plotArea>
                          <c:areaChart>
                            <c:ser>
                              <c:tx>
                                <c:strRef>
                                  <c:f>Sheet1!$B$1</c:f>
                                  <c:strCache>
                                    <c:ptCount val="1" />
                                    <c:pt idx="0">
                                      <c:v>系列 1</c:v>
                                    </c:pt>
                                  </c:strCache>
                                </c:strRef>
                              </c:tx>
                              ...
                            </c:ser>
                          </c:areaChart>
                        </c:plotArea>
                     */
                    var seriesText = areaChartSeries.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.SeriesText>()!;
                    var seriesTextStringReference =
                        seriesText.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.StringReference>()!;
                    // 这个公式表示是从 Excel 哪个数据获取的，获取的方式比较复杂。这里还是先从缓存获取
                    var seriesTextFormula = seriesTextStringReference
                        .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Formula>();

                    // 有缓存的话，从缓存获取就可以，缓存内容也许和 Excel 内容不对应
                    /*
                        <c:strCache>
                          <c:ptCount val="1" />
                          <c:pt idx="0">
                            <c:v>系列 1</c:v>
                          </c:pt>
                        </c:strCache>
                     */
                    var seriesTextStringCache = seriesTextStringReference
                        .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.StringCache>();
                    if (seriesTextStringCache != null)
                    {
                        var seriesTextStringPoint = seriesTextStringCache
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.StringPoint>();

                        var numericValue = seriesTextStringPoint!
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumericValue>();
                        // 系列1 标题
                        var title = numericValue!.Text;

                        outputText += "\r\n" + title + ":\r\n";
                    }

                    // 获取系列的值
                    /*
                        <c:plotArea>
                          <c:areaChart>
                            <c:ser>
                              <c:tx>
                                ...
                              </c:tx>
                              <c:cat>
                                ...
                              </c:cat>
                              <c:val>
                                <c:numRef>
                                  <c:f>Sheet1!$B$2:$B$6</c:f>
                                  <c:numCache>
                                    <c:formatCode>General</c:formatCode>
                                    <c:ptCount val="5" />
                                    <c:pt idx="0">
                                      <c:v>32</c:v>
                                    </c:pt>
                                    <c:pt idx="1">
                                      <c:v>32</c:v>
                                    </c:pt>
                                    <c:pt idx="2">
                                      <c:v>28</c:v>
                                    </c:pt>
                                    <c:pt idx="3">
                                      <c:v>12</c:v>
                                    </c:pt>
                                    <c:pt idx="4">
                                      <c:v>15</c:v>
                                    </c:pt>
                                  </c:numCache>
                                </c:numRef>
                              </c:val>
                            </c:ser>
                            <c:ser>
                              ...
                            </c:ser>
                          </c:areaChart>
                        </c:plotArea>
                     */
                    // 这就是系列里面最重要的数据。然而在 PPT 里面，是允许为空的，如果是空，行为就是不绘制系列内容
                    var valueList = new List<string>();
                    var values = areaChartSeries.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Values>();
                    // 基本上都是 NumberReference 类型，存放的是数值
                    var valuesNumberReference =
                        values?.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumberReference>();
                    if (valuesNumberReference != null)
                    {
                        /*
                             <c:val>
                               <c:numRef>
                                 <c:f>Sheet1!$B$2:$B$6</c:f>
                                 <c:numCache>
                                   <c:formatCode>General</c:formatCode>
                                   <c:ptCount val="5" />
                                   <c:pt idx="0">
                                     <c:v>32</c:v>
                                   </c:pt>
                                   <c:pt idx="1">
                                     <c:v>32</c:v>
                                   </c:pt>
                                   <c:pt idx="2">
                                     <c:v>28</c:v>
                                   </c:pt>
                                   <c:pt idx="3">
                                     <c:v>12</c:v>
                                   </c:pt>
                                   <c:pt idx="4">
                                     <c:v>15</c:v>
                                   </c:pt>
                                 </c:numCache>
                               </c:numRef>
                             </c:val>
                         */
                        // 这份课件一定存在 values 内容
                        // 和其他的一样，存在引用 Excel 的内容。这里同样也是采用缓存
                        var valuesFormula = valuesNumberReference
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.Formula>();

                        var valuesNumberingCache = valuesNumberReference
                            .GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumberingCache>()!;

                        // 通过 FormatCode 决定界面效果。这份课件是 General 表示不用格式化
                        var formatCode = valuesNumberingCache.FormatCode;
                        Debug.Assert(formatCode?.Text == "General");

                        foreach (var numericPoint in valuesNumberingCache
                                     .Elements<DocumentFormat.OpenXml.Drawing.Charts.NumericPoint>())
                        {
                            var numericValue =
                                numericPoint.GetFirstChild<DocumentFormat.OpenXml.Drawing.Charts.NumericValue>()!;
                            var numericValueText = numericValue.Text;

                            valueList.Add(numericValueText);
                        }
                    }

                    outputText += "系列值内容 ";

                    outputText += string.Join(',', valueList);

                    firstSeries = false;
                }
            }

            Root.Children.Add(new TextBlock()
            {
                Text = outputText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new System.Windows.Thickness(10, 10, 10, 10),
            });
        }
    }
}