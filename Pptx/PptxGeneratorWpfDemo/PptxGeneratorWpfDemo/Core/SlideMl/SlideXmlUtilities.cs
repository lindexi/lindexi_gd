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
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(metricsProvider);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new SlideMlRootElementException("SlideML 缺少根元素。");

        root.SetAttributeValue("ActualWidth", FormatNumber(SlideRenderer.CanvasWidth));
        root.SetAttributeValue("ActualHeight", FormatNumber(SlideRenderer.CanvasHeight));

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
