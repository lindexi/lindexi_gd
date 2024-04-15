using System;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Validation;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace LurkinurhuwarcuWhawhiweayea
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("测试.pptx", false))
            {
                var openXmlValidator = new OpenXmlValidator();
                foreach (var validationErrorInfo in openXmlValidator.Validate(presentationDocument))
                {
                }

                var presentationPart = presentationDocument.PresentationPart;
                var slidePart = presentationPart.SlideParts.First();
                var shape = slidePart.Slide.Descendants<Shape>().First();
                var lineReference = shape.Descendants<LineReference>().First();
                /*
       <p:sp>
        <p:style>
          <a:lnRef idx="5">
            <a:schemeClr val="accent1">
              <a:shade val="50000" />
            </a:schemeClr>
          </a:lnRef>
        </p:style>
       </p:sp>
                */
                var lineStyle = lineReference.Index.Value;
                // 这里的值是 5 表示使用主题的第 5 个样式
                // 文档规定，Index是从1开始的
                // https://docs.microsoft.com/en-za/dotnet/api/documentformat.openxml.drawing.linereference?view=openxml-2.8.1
                lineStyle--;

                /*
                  <a:themeElements>
                    <a:fmtScheme name="Office">
                      <a:lnStyleLst>
                        <a:ln w="6350" cap="flat" cmpd="sng" algn="ctr">
                          <a:solidFill>
                            <a:schemeClr val="phClr" />
                          </a:solidFill>
                          <a:prstDash val="solid" />
                          <a:miter lim="800000" />
                        </a:ln>
                        <a:ln w="12700" cap="flat" cmpd="sng" algn="ctr">
                          <a:solidFill>
                            <a:schemeClr val="phClr" />
                          </a:solidFill>
                          <a:prstDash val="solid" />
                          <a:miter lim="800000" />
                        </a:ln>
                        <a:ln w="69050" cap="flat" cmpd="sng" algn="ctr">
                          <a:solidFill>
                            <a:srgbClr val="954F72" />
                          </a:solidFill>
                          <a:prstDash val="solid" />
                          <a:miter lim="800000" />
                        </a:ln>
                      </a:lnStyleLst>
                    </a:fmtScheme>
                  </a:themeElements>
                 */
                // 获取主题
                var themeOverride = slidePart.ThemeOverridePart?.ThemeOverride
                    ?? slidePart.SlideLayoutPart.ThemeOverridePart?.ThemeOverride;
                FormatScheme formatScheme = themeOverride?.FormatScheme;
                if (formatScheme is null)
                {
                    formatScheme = slidePart.SlideLayoutPart.SlideMasterPart.ThemePart.Theme.ThemeElements.FormatScheme;
                }

                var lineStyleList = formatScheme.LineStyleList;
                var outlineList = lineStyleList.Elements<Outline>().ToList();
                Outline themeOutline;
                if (lineStyle > outlineList.Count)
                {
                    themeOutline = outlineList[^1];
                }
                else
                {
                    themeOutline = outlineList[(int)lineStyle];
                }
            }
        }
    }
}
