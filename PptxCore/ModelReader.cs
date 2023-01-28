using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Flatten.ElementConverters.CommonElement;
using DocumentFormat.OpenXml.Flatten.Framework.Context;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using dotnetCampus.OpenXmlUnitConverter;

namespace PptxCore;

/// <summary>
/// 评估结果
/// </summary>
class EvaluationResult
{

}

readonly record struct EvaluationArgument(bool IsElement, OpenXmlElement CurrentElement, List<OpenXmlElement> PathElementList, XmlQualifiedName Name)
{

}

class OpenXmlElementEvaluationHandler
{

}

/// <summary>
/// 元素评估结果
/// </summary>
/// <param name="IsMatch">当前的 <see cref="OpenXmlElementEvaluationHandler"/> 是否能处理</param>
/// <param name="Success">在 <see cref="IsMatch"/> 的前提下，此属性才有用。表示是否能处理</param>
readonly record struct OpenXmlElementEvaluationResult(bool IsMatch, bool Success)
{
}

class OpenXmlConverterEvaluator
{
    public void Evaluate(Slide slide)
    {
        var stack = new Stack<OpenXmlElement>();
        stack.Push(slide);

        while (stack.TryPop(out var result))
        {

            foreach (var openXmlElement in result.Elements())
            {
                stack.Push(openXmlElement);
            }
        }
    }

    private OpenXmlElementEvaluationResult Handle(in EvaluationArgument argument) =>
        new OpenXmlElementEvaluationResult(IsMatch: false, Success: true);

    private void Evaluate(in EvaluationArgument argument)
    {
        var element = argument.CurrentElement;

        // 导航信息
        var navigateEvaluationArgumentList = new List<EvaluationArgument>();

       



        foreach (var currentElement in element.Elements())
        {
            // 深度遍历
            //argument.PathElementList.Add(currentElement);

            try
            {


                var xmlQualifiedName = currentElement.XmlQualifiedName;
                foreach (var openXmlAttribute in currentElement.GetAttributes())
                {

                }
            }
            finally
            {
                //argument.PathElementList.RemoveAt(argument.PathElementList.Count - 1);
            }


        }
    }
}

public class ModelReader
{


    private void ShowElement(OpenXmlElement element)
    {
        foreach (var openXmlElement in element.Elements())
        {
            var elementXName = element.XName;
            var xmlQualifiedName = openXmlElement.XmlQualifiedName;
            foreach (var openXmlAttribute in openXmlElement.GetAttributes())
            {

            }
            ShowElement(openXmlElement);
        }
    }

    /// <summary>
    ///     构建出面积图上下文
    /// </summary>
    /// <param name="file">这里是例子，要求只能传入 Test.pptx 文件。其他文件没有支持</param>
    /// <returns></returns>
    public AreaChartRender BuildAreaChartRender(FileInfo file)
    {
        using var presentationDocument = PresentationDocument.Open(file.FullName, false);
        var slide = presentationDocument.PresentationPart!.SlideParts.First().Slide;
        var slideXmlQualifiedName = slide.XmlQualifiedName;

        ShowElement(slide);

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