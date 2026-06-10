using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace PptxGenerator;

internal sealed class SlideMlParser
{
    private int _nextId = 1;

    public SlidePage Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new SlideMlRootElementException("SlideML 根元素不能为空。");
        if (!string.Equals(root.Name.LocalName, "Page", StringComparison.Ordinal))
        {
            throw new SlideMlRootElementException($"SlideML 根元素必须是 Page，但实际为 {root.Name.LocalName}。");
        }

        var page = new SlidePage
        {
            Background = GetOptionalString(root, "Background") ?? "#FFFFFF",
        };

        foreach (var child in root.Elements())
        {
            page.Children.Add(ParseElement(child));
        }

        return page;
    }

    private SlideElement ParseElement(XElement element)
    {
        var id = GetOptionalString(element, "Id") ?? $"elem_{_nextId++:000}";

        return element.Name.LocalName switch
        {
            "Panel" => ParsePanel(element, id),
            "Rect" => ParseRect(element, id),
            "TextElement" => ParseTextElement(element, id),
            "Image" => ParseImageElement(element, id),
            _ => throw new SlideMlUnsupportedElementException($"不支持的标签: {element.Name.LocalName}", element.Name.LocalName),
        };
    }

    private SlidePanelElement ParsePanel(XElement element, string id)
    {
        var panel = new SlidePanelElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element),
            VerticalAlignment = GetOptionalVerticalAlignment(element),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Padding = GetOptionalDouble(element, "Padding") ?? 0,
            Background = GetOptionalString(element, "Background"),
        };

        foreach (var child in element.Elements())
        {
            panel.Children.Add(ParseElement(child));
        }

        return panel;
    }

    private SlideRectElement ParseRect(XElement element, string id)
    {
        return new SlideRectElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element),
            VerticalAlignment = GetOptionalVerticalAlignment(element),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Fill = GetOptionalString(element, "Fill"),
            Stroke = GetOptionalString(element, "Stroke"),
            StrokeThickness = GetOptionalDouble(element, "StrokeThickness") ?? 0,
            CornerRadius = GetOptionalDouble(element, "CornerRadius") ?? 0,
        };
    }

    private SlideTextElement ParseTextElement(XElement element, string id)
    {
        var text = GetOptionalString(element, "Text");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new SlideMlRequiredAttributeMissingException($"TextElement({id}) 必须包含 Text 属性。", id, "Text");
        }

        return new SlideTextElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element),
            VerticalAlignment = GetOptionalVerticalAlignment(element),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Text = text,
            FontName = GetOptionalString(element, "FontName") ?? "Microsoft YaHei",
            FontSize = GetOptionalDouble(element, "FontSize") ?? 16,
            Foreground = GetOptionalString(element, "Foreground") ?? "#000000",
            TextAlignment = GetOptionalTextAlignment(element) ?? SlideTextAlignment.Left,
            LineHeight = GetOptionalDouble(element, "LineHeight") ?? 1.2,
        };
    }

    private SlideImageElement ParseImageElement(XElement element, string id)
    {
        var source = GetOptionalString(element, "Source");
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new SlideMlRequiredAttributeMissingException($"Image({id}) 必须包含 Source 属性。", id, "Source");
        }

        return new SlideImageElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element),
            VerticalAlignment = GetOptionalVerticalAlignment(element),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Source = source,
            Stretch = GetOptionalImageStretch(element) ?? SlideImageStretch.Uniform,
        };
    }

    private static string? GetOptionalString(XElement element, string attributeName)
    {
        return (string?) element.Attribute(attributeName);
    }

    private static double? GetOptionalDouble(XElement element, string attributeName)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var elementId = GetOptionalString(element, "Id");
        try
        {
            return double.Parse(text, CultureInfo.InvariantCulture);
        }
        catch (FormatException ex)
        {
            throw new SlideMlAttributeFormatException(
                $"元素({elementId ?? "unknown"}) 的属性 {attributeName} 值 \"{text}\" 不是有效的数值。",
                elementId, attributeName, text, ex);
        }
    }

    private static SlideHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element)
    {
        var text = GetOptionalString(element, "HorizontalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var elementId = GetOptionalString(element, "Id");
        try
        {
            return Enum.Parse<SlideHorizontalAlignment>(text, ignoreCase: true);
        }
        catch (ArgumentException ex)
        {
            throw new SlideMlAttributeFormatException(
                $"元素({elementId ?? "unknown"}) 的属性 HorizontalAlignment 值 \"{text}\" 不是有效的 SlideHorizontalAlignment。",
                elementId, "HorizontalAlignment", text, ex);
        }
    }

    private static SlideVerticalAlignment? GetOptionalVerticalAlignment(XElement element)
    {
        var text = GetOptionalString(element, "VerticalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var elementId = GetOptionalString(element, "Id");
        try
        {
            return Enum.Parse<SlideVerticalAlignment>(text, ignoreCase: true);
        }
        catch (ArgumentException ex)
        {
            throw new SlideMlAttributeFormatException(
                $"元素({elementId ?? "unknown"}) 的属性 VerticalAlignment 值 \"{text}\" 不是有效的 SlideVerticalAlignment。",
                elementId, "VerticalAlignment", text, ex);
        }
    }

    private static SlideTextAlignment? GetOptionalTextAlignment(XElement element)
    {
        var text = GetOptionalString(element, "TextAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var elementId = GetOptionalString(element, "Id");
        try
        {
            return Enum.Parse<SlideTextAlignment>(text, ignoreCase: true);
        }
        catch (ArgumentException ex)
        {
            throw new SlideMlAttributeFormatException(
                $"元素({elementId ?? "unknown"}) 的属性 TextAlignment 值 \"{text}\" 不是有效的 SlideTextAlignment。",
                elementId, "TextAlignment", text, ex);
        }
    }

    private static SlideImageStretch? GetOptionalImageStretch(XElement element)
    {
        var text = GetOptionalString(element, "Stretch");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var elementId = GetOptionalString(element, "Id");
        try
        {
            return Enum.Parse<SlideImageStretch>(text, ignoreCase: true);
        }
        catch (ArgumentException ex)
        {
            throw new SlideMlAttributeFormatException(
                $"元素({elementId ?? "unknown"}) 的属性 Stretch 值 \"{text}\" 不是有效的 SlideImageStretch。",
                elementId, "Stretch", text, ex);
        }
    }
}
