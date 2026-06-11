using System;
using System.Linq;
using System.Xml.Linq;

namespace PptxGenerator;

internal static class SlideXmlUtilities
{
    public static string ExtractXml(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var xmlStartIndex = text.IndexOf("<?xml", StringComparison.OrdinalIgnoreCase);
        if (xmlStartIndex >= 0)
        {
            return text[xmlStartIndex..].Trim();
        }

        var pageStartIndex = text.IndexOf("<Page", StringComparison.OrdinalIgnoreCase);
        if (pageStartIndex >= 0)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + text[pageStartIndex..].Trim();
        }

        return text.Trim();
    }

    public static string NormalizeXml(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return XDocument.Parse(xml, LoadOptions.PreserveWhitespace).ToString();
    }

    public static string FormatRenderedXml(string xml, Func<string, SlideRenderedMetrics?> metricsProvider)
    {
        return FormatRenderedXml(xml, metricsProvider, new SlideRenderContext());
    }

    /// <summary>
    /// 回填渲染后的实际尺寸到 XML 中。
    /// </summary>
    /// <param name="xml">SlideML XML 字符串。</param>
    /// <param name="metricsProvider">通过元素 Id 获取渲染指标的委托。</param>
    /// <param name="context">渲染上下文（含画布尺寸）。</param>
    /// <returns>回填后的 XML。</returns>
    public static string FormatRenderedXml(string xml, Func<string, SlideRenderedMetrics?> metricsProvider, SlideRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(metricsProvider);
        ArgumentNullException.ThrowIfNull(context);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new SlideMlRootElementException("SlideML 缺少根元素。");

        root.SetAttributeValue("ActualWidth", FormatNumber(context.CanvasWidth));
        root.SetAttributeValue("ActualHeight", FormatNumber(context.CanvasHeight));

        foreach (var element in root.DescendantsAndSelf().Where(t => t.Name.LocalName is "Page" or "Panel" or "Rect" or "TextElement" or "Image"))
        {
            var id = (string?) element.Attribute("Id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var metrics = metricsProvider(id);
            if (metrics is null)
            {
                continue;
            }

            element.SetAttributeValue("ActualWidth", FormatNumber(metrics.ActualWidth));
            element.SetAttributeValue("ActualHeight", FormatNumber(metrics.ActualHeight));
            if (metrics.ActualLineCount is not null)
            {
                element.SetAttributeValue("ActualLineCount", metrics.ActualLineCount.Value);
            }
            else
            {
                element.Attribute("ActualLineCount")?.Remove();
            }
        }

        return document.ToString();
    }

    public static string FormatNumber(double value)
    {
        return Math.Round(value, 2).ToString("0.##");
    }
}
