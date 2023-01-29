using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Flatten.ElementConverters.CommonElement;
using DocumentFormat.OpenXml.Flatten.Framework.Context;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using dotnetCampus.OpenXmlUnitConverter;
using ShapeProperties = DocumentFormat.OpenXml.Presentation.ShapeProperties;

namespace PptxCore;

/// <summary>
/// 评估结果
/// </summary>
class EvaluationResult
{

}

readonly record struct EvaluationArgument(OpenXmlElement CurrentElement, OpenXmlAttribute? OpenXmlAttribute)
{
    public bool IsElement => OpenXmlAttribute is null;

    public XmlQualifiedName Name =>
        OpenXmlAttribute is null ? CurrentElement.XmlQualifiedName : OpenXmlAttribute.Value.XmlQualifiedName;

    public void BuildPath(StringBuilder output)
    {
        var element = CurrentElement.Parent;

        while (element is not null)
        {
            output.Insert(0, $"{element.Prefix}:{element.LocalName}.");

            element = element.Parent;
        }

        output.Append($"{CurrentElement.Prefix}:{CurrentElement.LocalName}");

        if (OpenXmlAttribute is not null)
        {
            output.Append($".{OpenXmlAttribute.Value.Prefix}:{OpenXmlAttribute.Value.LocalName}={OpenXmlAttribute.Value.Value}");
        }
    }
}

class OpenXmlElementEvaluationHandler
{

}

/// <summary>
/// 元素评估结果
/// </summary>
/// <param name="IsMatch">当前的 <see cref="OpenXmlElementEvaluationHandler"/> 是否能处理</param>
/// <param name="Success">在 <see cref="IsMatch"/> 的前提下，此属性才有用。表示是否能处理</param>
/// <param name="ShouldHandleChildrenElement">是否需要处理子元素</param>
/// <param name="Score">分数</param>
readonly record struct OpenXmlElementEvaluationResult(bool IsMatch, bool Success,bool ShouldHandleChildrenElement, OpenXmlElementEvaluationScore Score)
{
}

/// <summary>
/// 评估分数
/// </summary>
readonly record struct OpenXmlElementEvaluationScore(double Score, double Weight)
{
    public static OpenXmlElementEvaluationScore Zero => new OpenXmlElementEvaluationScore(Score: 0, Weight: 1);
}

class OpenXmlConverterEvaluator
{
    public void Evaluate(OpenXmlElement inputElement)
    {
        OpenXmlElement? element = inputElement;
        var unsupportedAttributeList = new List<OpenXmlAttribute>();

        {
            bool isFirst = true;
            var isLeafElement = false;
            var finishChild = false;

            while (element is not null)
            {
                // 处理元素本身
                var evaluationArgument = new EvaluationArgument(element, OpenXmlAttribute: null);

                var evaluationResult = Handle(evaluationArgument);

                // 如果元素本身不能处理，那就继续处理下一层的元素，直到元素属于叶子。依然没有任何处理，记录没有处理

                var firstChild = element.FirstChild;
                isLeafElement = firstChild is not null;

                if (isLeafElement && !finishChild)
                {
                    element = firstChild;

                    continue;
                }
                else
                {
                    finishChild = false;

                    // 这是叶子了

                    // 处理相同的一个元素下的其他元素
                    var elementAfter = element.ElementsAfter().FirstOrDefault();

                    if (elementAfter is not null)
                    {
                        element = elementAfter;

                        continue;
                    }
                    else
                    {
                        // 这个元素完成了
                        element = element.Parent;

                        finishChild = true;

                        continue;
                    }
                }
            }
        }

        var stack = new Stack<OpenXmlElement>();
        stack.Push(element);


        while (stack.TryPop(out var currentElement))
        {
            unsupportedAttributeList.Clear();
            var evaluationArgument = new EvaluationArgument(currentElement, OpenXmlAttribute: null);

            var evaluationResult = Handle(evaluationArgument);

            // 如果能有处理的类型，那就不需要报告类型不支持
            var anyHandle = evaluationResult.IsMatch;
            var isLeafElement = true; // 不能使用 currentElement is OpenXmlLeafElement 判断，因为这里是判断没有包含其他元素

            if (evaluationResult.IsMatch)
            {
                // 如果已经匹配，那就不需要遍历元素的属性
            }
            else
            {
                // 没有匹配到元素，也就是元素不是所有的属性就能支持的，那就遍历元素的属性
                foreach (OpenXmlAttribute openXmlAttribute in currentElement.GetAttributes())
                {
                    var argument = evaluationArgument with
                    {
                        OpenXmlAttribute = openXmlAttribute
                    };
                    var result = Handle(argument);
                    if (result.Success)
                    {
                        // 证明有一个能支持的，就不需要上报元素级不支持了
                        anyHandle = true;
                    }
                    else
                    {
                        unsupportedAttributeList.Add(openXmlAttribute);
                        // 先不立刻上报，如果是元素级不支持，那就只上报元素好了
                        //Report(argument);
                    }
                }
            }

            // 如果元素本身不能处理，那就继续处理下一层的元素，直到元素属于叶子。依然没有任何处理，记录没有处理

            if (evaluationResult.ShouldHandleChildrenElement)
            {
                foreach (var openXmlElement in currentElement.Elements())
                {
                    stack.Push(openXmlElement);
                    isLeafElement = false;
                }
            }
            else
            {
                
            }

            if (isLeafElement && !anyHandle)
            {
                // 没有任何处理
                Report(evaluationArgument);
            }
            else
            {
                if (unsupportedAttributeList.Any())
                {
                    // 如果不上报元素级，且存在不支持的属性，那就上报属性
                    foreach (var openXmlAttribute in unsupportedAttributeList)
                    {
                        var argument = evaluationArgument with
                        {
                            OpenXmlAttribute = openXmlAttribute
                        };
                        Report(argument);
                    }
                }
            }
        }
    }

    private void Report(in EvaluationArgument argument)
    {
        var stringBuilder = new StringBuilder();
        argument.BuildPath(stringBuilder);
        var message = stringBuilder.ToString();
        Debug.WriteLine(message);
    }

    private OpenXmlElementEvaluationResult Handle(in EvaluationArgument argument) =>
        new OpenXmlElementEvaluationResult(IsMatch: false, Success: true,ShouldHandleChildrenElement: true, OpenXmlElementEvaluationScore.Zero);
}

public class ModelReader
{
    /// <summary>
    ///     构建出面积图上下文
    /// </summary>
    /// <param name="file">这里是例子，要求只能传入 Test.pptx 文件。其他文件没有支持</param>
    /// <returns></returns>
    public AreaChartRender BuildAreaChartRender(FileInfo file)
    {
        using var presentationDocument = PresentationDocument.Open(file.FullName, false);
        var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;

        var openXmlConverterEvaluator = new OpenXmlConverterEvaluator();

        openXmlConverterEvaluator.Evaluate(slide);

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
        var chartReference = graphicData?.GetFirstChild<ChartReference>();

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
        }

        var chartPart = (ChartPart) openXmlPart!;

        // 这里的 ChartPart 对应的就是 charts\chartN.xml 文件。这里的 chartN.xml 表示的是 chart1.xml 或 chart2.xml 等文件
        var chartSpace = chartPart.ChartSpace;

        var slideContext = new SlideContext(slide, presentationDocument);

        var transformData = graphicFrame.GetOrCreateTransformData(slideContext);

        return new AreaChartRender(new AreaChartRenderContext(chartSpace, slideContext, transformData.Width.ToPixel(),
            transformData.Height.ToPixel()));
    }
}