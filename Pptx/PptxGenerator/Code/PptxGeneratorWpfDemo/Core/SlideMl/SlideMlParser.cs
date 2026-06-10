using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace PptxGenerator;

internal sealed class SlideMlParser
{
    private int _nextId = 1;

    private static readonly HashSet<string> _pageKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Background",
    };

    private static readonly HashSet<string> _panelKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Padding", "Background",
    };

    private static readonly HashSet<string> _rectKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Fill", "Stroke", "StrokeThickness", "CornerRadius",
    };

    private static readonly HashSet<string> _textElementKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Text", "FontName", "FontSize", "Foreground",
        "TextAlignment", "LineHeight",
    };

    private static readonly HashSet<string> _imageKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Source", "Stretch",
    };

    public SlidePage Parse(string xml, SlideParseContext context)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(context);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("SlideML 根元素不能为空。");
        if (!string.Equals(root.Name.LocalName, "Page", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("SlideML 根元素必须是 Page。");
        }

        ValidateAttributes(root, "Page", _pageKnownAttributes, context);

        var page = new SlidePage
        {
            Background = GetOptionalString(root, "Background") ?? "#FFFFFF",
        };

        foreach (var child in root.Elements())
        {
            page.Children.Add(ParseElement(child, context));
        }

        return page;
    }

    private SlideElement ParseElement(XElement element, SlideParseContext context)
    {
        var id = GetOptionalString(element, "Id") ?? $"elem_{_nextId++:000}";

        return element.Name.LocalName switch
        {
            "Panel" => ParsePanel(element, id, context),
            "Rect" => ParseRect(element, id, context),
            "TextElement" => ParseTextElement(element, id, context),
            "Image" => ParseImageElement(element, id, context),
            _ => throw new InvalidOperationException($"不支持的标签: {element.Name.LocalName}"),
        };
    }

    private SlidePanelElement ParsePanel(XElement element, string id, SlideParseContext context)
    {
        ValidateAttributes(element, id, _panelKnownAttributes, context);

        var panel = new SlidePanelElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Padding = GetOptionalDouble(element, "Padding") ?? 0,
            Background = GetOptionalString(element, "Background"),
        };

        foreach (var child in element.Elements())
        {
            panel.Children.Add(ParseElement(child, context));
        }

        return panel;
    }

    private SlideRectElement ParseRect(XElement element, string id, SlideParseContext context)
    {
        ValidateAttributes(element, id, _rectKnownAttributes, context);

        return new SlideRectElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Fill = GetOptionalString(element, "Fill"),
            Stroke = GetOptionalString(element, "Stroke"),
            StrokeThickness = GetOptionalDouble(element, "StrokeThickness") ?? 0,
            CornerRadius = GetOptionalDouble(element, "CornerRadius") ?? 0,
        };
    }

    private SlideTextElement ParseTextElement(XElement element, string id, SlideParseContext context)
    {
        ValidateAttributes(element, id, _textElementKnownAttributes, context);

        var text = GetOptionalString(element, "Text");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"TextElement({id}) 必须包含 Text 属性。");
        }

        return new SlideTextElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Text = text,
            FontName = GetOptionalString(element, "FontName") ?? "Microsoft YaHei",
            FontSize = GetOptionalDouble(element, "FontSize") ?? 16,
            Foreground = GetOptionalString(element, "Foreground") ?? "#000000",
            TextAlignment = GetOptionalTextAlignment(element, id, context) ?? SlideTextAlignment.Left,
            LineHeight = GetOptionalDouble(element, "LineHeight") ?? 1.2,
        };
    }

    private SlideImageElement ParseImageElement(XElement element, string id, SlideParseContext context)
    {
        ValidateAttributes(element, id, _imageKnownAttributes, context);

        var source = GetOptionalString(element, "Source");
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidOperationException($"Image({id}) 必须包含 Source 属性。");
        }

        return new SlideImageElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X"),
            Y = GetOptionalDouble(element, "Y"),
            Width = GetOptionalDouble(element, "Width"),
            Height = GetOptionalDouble(element, "Height"),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity") ?? 1,
            Source = source,
            Stretch = GetOptionalImageStretch(element, id, context) ?? SlideImageStretch.Uniform,
        };
    }

    private static void ValidateAttributes(XElement element, string elementId,
        HashSet<string> knownAttributes, SlideParseContext context)
    {
        foreach (var attr in element.Attributes())
        {
            if (!knownAttributes.Contains(attr.Name.LocalName))
            {
                context.AddWarning($"[Warning] {elementId}: 未知属性 \"{attr.Name.LocalName}\"，已忽略");
            }
        }
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

        return double.Parse(text, CultureInfo.InvariantCulture);
    }

    private static SlideHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element, string elementId, SlideParseContext context)
    {
        var text = GetOptionalString(element, "HorizontalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideHorizontalAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddWarning($"[Warning] {elementId}: HorizontalAlignment 值 \"{text}\" 无效，已忽略（有效值：Left, Center, Right）");
        return null;
    }

    private static SlideVerticalAlignment? GetOptionalVerticalAlignment(XElement element, string elementId, SlideParseContext context)
    {
        var text = GetOptionalString(element, "VerticalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideVerticalAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddWarning($"[Warning] {elementId}: VerticalAlignment 值 \"{text}\" 无效，已忽略（有效值：Top, Center, Bottom）");
        return null;
    }

    private static SlideTextAlignment? GetOptionalTextAlignment(XElement element, string elementId, SlideParseContext context)
    {
        var text = GetOptionalString(element, "TextAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideTextAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddWarning($"[Warning] {elementId}: TextAlignment 值 \"{text}\" 无效，已忽略（有效值：Left, Center, Right, Justify）");
        return null;
    }

    private static SlideImageStretch? GetOptionalImageStretch(XElement element, string elementId, SlideParseContext context)
    {
        var text = GetOptionalString(element, "Stretch");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideImageStretch>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddWarning($"[Warning] {elementId}: Stretch 值 \"{text}\" 无效，已忽略（有效值：None, Fill, Uniform, UniformToFill）");
        return null;
    }
}