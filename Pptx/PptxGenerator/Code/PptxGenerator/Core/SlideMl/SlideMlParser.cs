using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace PptxGenerator;

internal sealed class SlideMlParser
{
    private int _nextId = 1;

    public SlidePage Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("SlideML 根元素不能为空。");
        if (!string.Equals(root.Name.LocalName, "Page", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("SlideML 根元素必须是 Page。");
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
            _ => throw new InvalidOperationException($"不支持的标签: {element.Name.LocalName}"),
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
            BackgroundBrush = ParseBrush(element),
        };

        foreach (var child in element.Elements())
        {
            if (string.Equals(child.Name.LocalName, "Fill", StringComparison.Ordinal))
            {
                continue;
            }

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
            FillBrush = ParseBrush(element),
            StrokeBrush = ParseStrokeBrush(element),
            StrokeThickness = GetOptionalDouble(element, "StrokeThickness") ?? 0,
            CornerRadius = ParseCornerRadius(element),
            Shadow = ParseShadowFromElement(element),
        };
    }

    private SlideTextElement ParseTextElement(XElement element, string id)
    {
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
            throw new InvalidOperationException($"Image({id}) 必须包含 Source 属性。");
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

    /// <summary>
    /// 统一解析元素填充画刷：优先子元素 &lt;Fill&gt; 中的 LinearGradient，其次 Fill/Background 属性（纯色）。
    /// </summary>
    private static SlideBrush? ParseBrush(XElement element)
    {
        var fillChild = element.Element("Fill");
        if (fillChild is not null)
        {
            var gradient = ParseLinearGradientChild(fillChild);
            if (gradient is not null)
            {
                return gradient;
            }
        }

        var color = GetOptionalString(element, "Fill");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideSolidColorBrush { Color = color };
        }

        color = GetOptionalString(element, "Background");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideSolidColorBrush { Color = color };
        }

        return null;
    }

    /// <summary>
    /// 统一解析描边画刷：优先子元素 &lt;Stroke&gt; 中的 LinearGradient，其次 Stroke 属性（纯色）。
    /// </summary>
    private static SlideBrush? ParseStrokeBrush(XElement element)
    {
        var strokeChild = element.Element("Stroke");
        if (strokeChild is not null)
        {
            var gradient = ParseLinearGradientChild(strokeChild);
            if (gradient is not null)
            {
                return gradient;
            }
        }

        var color = GetOptionalString(element, "Stroke");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideSolidColorBrush { Color = color };
        }

        return null;
    }

    /// <summary>
    /// 统一解析阴影：优先 &lt;Shadow&gt; 子元素，其次 Shadow 属性字符串。
    /// </summary>
    private static SlideShadow? ParseShadowFromElement(XElement element)
    {
        var shadowChild = element.Element("Shadow");
        if (shadowChild is not null)
        {
            return new SlideShadow
            {
                OffsetX = GetOptionalDouble(shadowChild, "OffsetX") ?? 0,
                OffsetY = GetOptionalDouble(shadowChild, "OffsetY") ?? 4,
                Blur = GetOptionalDouble(shadowChild, "Blur") ?? 12,
                Color = GetOptionalString(shadowChild, "Color") ?? "#00000033",
                Opacity = GetOptionalDouble(shadowChild, "Opacity") ?? 1,
            };
        }

        var shadowText = GetOptionalString(element, "Shadow");
        return SlideShadow.Parse(shadowText);
    }

    private static SlideLinearGradientBrush? ParseLinearGradientChild(XElement parentElement)
    {
        var gradientElement = parentElement.Element("LinearGradient");
        if (gradientElement is null)
        {
            return null;
        }

        var stops = new List<SlideGradientStop>();
        foreach (var stopElement in gradientElement.Elements("Stop"))
        {
            var offset = GetOptionalDouble(stopElement, "Offset");
            var color = GetOptionalString(stopElement, "Color");
            if (offset is null || string.IsNullOrWhiteSpace(color))
            {
                continue;
            }

            stops.Add(new SlideGradientStop
            {
                Offset = Math.Clamp(offset.Value, 0, 1),
                Color = color,
            });
        }

        if (stops.Count == 0)
        {
            return null;
        }

        return new SlideLinearGradientBrush
        {
            X1 = GetOptionalDouble(gradientElement, "X1") ?? 0,
            Y1 = GetOptionalDouble(gradientElement, "Y1") ?? 0,
            X2 = GetOptionalDouble(gradientElement, "X2") ?? 1,
            Y2 = GetOptionalDouble(gradientElement, "Y2") ?? 0,
            Stops = stops,
        };
    }

    /// <summary>
    /// 解析圆角半径：支持单值或逗号分隔格式。
    /// </summary>
    private static SlideCornerRadius? ParseCornerRadius(XElement element)
    {
        var text = GetOptionalString(element, "CornerRadius");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cornerRadius = SlideCornerRadius.Parse(text);
        if (cornerRadius is not null)
        {
            return cornerRadius;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var singleValue))
        {
            return (SlideCornerRadius)singleValue;
        }

        return null;
    }

    private static string? GetOptionalString(XElement element, string attributeName)
    {
        return (string?)element.Attribute(attributeName);
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

    private static SlideHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element)
    {
        var text = GetOptionalString(element, "HorizontalAlignment");
        return string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.Parse<SlideHorizontalAlignment>(text, ignoreCase: true);
    }

    private static SlideVerticalAlignment? GetOptionalVerticalAlignment(XElement element)
    {
        var text = GetOptionalString(element, "VerticalAlignment");
        return string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.Parse<SlideVerticalAlignment>(text, ignoreCase: true);
    }

    private static SlideTextAlignment? GetOptionalTextAlignment(XElement element)
    {
        var text = GetOptionalString(element, "TextAlignment");
        return string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.Parse<SlideTextAlignment>(text, ignoreCase: true);
    }

    private static SlideImageStretch? GetOptionalImageStretch(XElement element)
    {
        var text = GetOptionalString(element, "Stretch");
        return string.IsNullOrWhiteSpace(text)
            ? null
            : Enum.Parse<SlideImageStretch>(text, ignoreCase: true);
    }
}
