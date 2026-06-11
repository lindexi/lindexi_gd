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

    public SlidePage Parse(string xml, SlidePipelineContext context)
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

    private SlideElement ParseElement(XElement element, SlidePipelineContext context)
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

    private SlidePanelElement ParsePanel(XElement element, string id, SlidePipelineContext context)
    {
        ValidateAttributes(element, id, _panelKnownAttributes, context);

        var panel = new SlidePanelElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X", context),
            Y = GetOptionalDouble(element, "Y", context),
            Width = GetOptionalDouble(element, "Width", context),
            Height = GetOptionalDouble(element, "Height", context),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
            Padding = GetOptionalDouble(element, "Padding", context) ?? 0,
            Background = GetOptionalString(element, "Background"),
        };

        foreach (var child in element.Elements())
        {
            panel.Children.Add(ParseElement(child, context));
        }

        return panel;
    }

    private SlideRectElement ParseRect(XElement element, string id, SlidePipelineContext context)
    {
        ValidateAttributes(element, id, _rectKnownAttributes, context);

        return new SlideRectElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X", context),
            Y = GetOptionalDouble(element, "Y", context),
            Width = GetOptionalDouble(element, "Width", context),
            Height = GetOptionalDouble(element, "Height", context),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
            Fill = GetOptionalString(element, "Fill"),
            Stroke = GetOptionalString(element, "Stroke"),
            StrokeThickness = GetOptionalDouble(element, "StrokeThickness", context) ?? 0,
            CornerRadius = GetOptionalDouble(element, "CornerRadius", context) ?? 0,
        };
    }

    private SlideTextElement ParseTextElement(XElement element, string id, SlidePipelineContext context)
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
            X = GetOptionalDouble(element, "X", context),
            Y = GetOptionalDouble(element, "Y", context),
            Width = GetOptionalDouble(element, "Width", context),
            Height = GetOptionalDouble(element, "Height", context),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
            Text = text,
            FontName = GetOptionalString(element, "FontName") ?? "Microsoft YaHei",
            FontSize = GetOptionalDouble(element, "FontSize", context) ?? 16,
            Foreground = GetOptionalString(element, "Foreground") ?? "#000000",
            TextAlignment = GetOptionalTextAlignment(element, id, context) ?? SlideTextAlignment.Left,
            LineHeight = GetOptionalDouble(element, "LineHeight", context) ?? 1.2,
        };
    }

    private SlideImageElement ParseImageElement(XElement element, string id, SlidePipelineContext context)
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
            X = GetOptionalDouble(element, "X", context),
            Y = GetOptionalDouble(element, "Y", context),
            Width = GetOptionalDouble(element, "Width", context),
            Height = GetOptionalDouble(element, "Height", context),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
            Source = source,
            Stretch = GetOptionalImageStretch(element, id, context) ?? SlideImageStretch.Uniform,
        };
    }

    private static void ValidateAttributes(XElement element, string elementId,
        HashSet<string> knownAttributes, SlidePipelineContext context)
    {
        foreach (var attr in element.Attributes())
        {
            if (!knownAttributes.Contains(attr.Name.LocalName))
            {
                context.AddWarning(
                    string.Format(
                        "[Warning] {0}: 未知属性 \"{1}\"，已忽略",
                        elementId,
                        attr.Name.LocalName));
            }
        }
    }

    private static string? GetOptionalString(XElement element, string attributeName)
    {
        return (string?) element.Attribute(attributeName);
    }

    private static double? GetOptionalDouble(XElement element, string attributeName, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        var elementId = GetOptionalString(element, "Id");
        context.AddError(
            string.Format(
                "[Error] {0}: {1} 值 \"{2}\" 不是有效的数值",
                elementId ?? "unknown",
                attributeName,
                text));
        return null;
    }

    private static SlideHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element, string elementId, SlidePipelineContext context)
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

        context.AddError(
            string.Format(
                "[Error] {0}: HorizontalAlignment 值 \"{1}\" 无效，已忽略（有效值：Left, Center, Right）",
                elementId,
                text));
        return null;
    }

    private static SlideVerticalAlignment? GetOptionalVerticalAlignment(XElement element, string elementId, SlidePipelineContext context)
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

        context.AddError(
            string.Format(
                "[Error] {0}: VerticalAlignment 值 \"{1}\" 无效，已忽略（有效值：Top, Center, Bottom）",
                elementId,
                text));
        return null;
    }

    private static SlideTextAlignment? GetOptionalTextAlignment(XElement element, string elementId, SlidePipelineContext context)
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

        context.AddError(
            string.Format(
                "[Error] {0}: TextAlignment 值 \"{1}\" 无效，已忽略（有效值：Left, Center, Right, Justify）",
                elementId,
                text));
        return null;
    }

    private static SlideImageStretch? GetOptionalImageStretch(XElement element, string elementId, SlidePipelineContext context)
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

        context.AddError(
            string.Format(
                "[Error] {0}: Stretch 值 \"{1}\" 无效，已忽略（有效值：None, Fill, Uniform, UniformToFill）",
                elementId,
                text));
        return null;
    }
}
