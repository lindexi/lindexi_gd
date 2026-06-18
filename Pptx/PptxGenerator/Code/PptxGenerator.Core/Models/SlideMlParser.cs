using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace PptxGenerator;

/// <summary>
/// SlideML 解析器，将 XML 字符串解析为 <see cref="SlidePage"/> 对象树。
/// 无 UI 框架依赖。
/// </summary>
public sealed class SlideMlParser
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
        "Padding", "Background", "Layout", "Gap", "Margin",
    };

    private static readonly HashSet<string> _rectKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Fill", "Stroke", "StrokeThickness", "CornerRadius", "Margin",
        "Shadow", "StrokeDashArray",
    };

    private static readonly HashSet<string> _textElementKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Text", "FontName", "FontSize", "Foreground",
        "TextAlignment", "LineHeight", "Margin",
        "FontWeight", "Style",
    };

    private static readonly HashSet<string> _imageKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Source", "Stretch", "Margin",
    };

    /// <summary>
    /// 将 SlideML XML 解析为 <see cref="SlidePage"/> 对象。
    /// </summary>
    /// <param name="xml">SlideML XML 字符串。</param>
    /// <param name="context">渲染流水线上下文，用于收集诊断信息。</param>
    /// <returns>解析后的页面对象。</returns>
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

        // 解析 Page.Styles 子元素
        var stylesElement = root.Element("Page.Styles");
        if (stylesElement is not null)
        {
            var styles = new List<SlideTextStyle>();
            foreach (var textStyleElement in stylesElement.Elements("TextStyle"))
            {
                var styleId = GetOptionalString(textStyleElement, "Id");
                if (string.IsNullOrWhiteSpace(styleId))
                {
                    context.AddWarning("[Warning] Page.Styles: TextStyle 缺少 Id 属性，已忽略");
                    continue;
                }

                styles.Add(new SlideTextStyle
                {
                    Id = styleId,
                    FontSize = GetOptionalDouble(textStyleElement, "FontSize", context),
                    FontWeight = GetOptionalFontWeight(textStyleElement, styleId, context),
                    Foreground = GetOptionalString(textStyleElement, "Foreground"),
                    FontName = GetOptionalString(textStyleElement, "FontName"),
                    LineHeight = GetOptionalDouble(textStyleElement, "LineHeight", context),
                    TextAlignment = GetOptionalTextAlignment(textStyleElement, styleId, context),
                });
            }

            if (styles.Count > 0)
            {
                page = new SlidePage
                {
                    Background = GetOptionalString(root, "Background") ?? "#FFFFFF",
                    Styles = styles,
                };
            }
        }

        foreach (var child in root.Elements().Where(e => !string.Equals(e.Name.LocalName, "Page.Styles", StringComparison.Ordinal)))
        {
            var element = ParseElement(child, context);
            if (element is not null)
            {
                page.Children.Add(element);
            }
        }

        return page;
    }

    private SlideElement? ParseElement(XElement element, SlidePipelineContext context)
    {
        var id = GetOptionalString(element, "Id") ?? $"elem_{_nextId++:000}";

        return element.Name.LocalName switch
        {
            "Panel" => ParsePanel(element, id, context),
            "Rect" => ParseRect(element, id, context),
            "TextElement" => ParseTextElement(element, id, context),
            "Image" => ParseImageElement(element, id, context),
            _ => WarnUnknownTag(element, id, context),
        };
    }

    private static SlideElement? WarnUnknownTag(XElement element, string id, SlidePipelineContext context)
    {
        context.AddWarning(
            string.Format(
                "[Warning] {0}: 未知标签 \"{1}\"，已忽略",
                id,
                element.Name.LocalName));
        return null;
    }

    private SlidePanelElement ParsePanel(XElement element, string id, SlidePipelineContext context)
    {
        ValidateAttributes(element, id, _panelKnownAttributes, context);

        var backgroundBrush = ParseBackground(element, id, context);

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
            Background = backgroundBrush,
            Layout = GetOptionalLayoutDirection(element, id, context) ?? SlideLayoutDirection.Absolute,
            Gap = GetOptionalDouble(element, "Gap", context) ?? 0,
            Margin = GetOptionalThickness(element, "Margin", context),
        };

        foreach (var child in element.Elements())
        {
            // 跳过已解析的 Fill 子元素
            if (string.Equals(child.Name.LocalName, "Fill", StringComparison.Ordinal))
            {
                continue;
            }

            var childElement = ParseElement(child, context);
            if (childElement is not null)
            {
                panel.Children.Add(childElement);
            }
        }

        return panel;
    }

    private SlideRectElement ParseRect(XElement element, string id, SlidePipelineContext context)
    {
        ValidateAttributes(element, id, _rectKnownAttributes, context);

        var fillBrush = ParseFill(element, id, context);
        var strokeBrush = ParseStroke(element, id, context);
        SlideShadow? shadowChild = null;

        foreach (var child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "Fill":
                case "Stroke":
                    // 已在 ParseFill / ParseStroke 中统一处理
                    break;
                case "Shadow":
                    shadowChild = ParseShadowElement(child, id, context);
                    break;
                default:
                    context.AddWarning(
                        string.Format(
                            "[Warning] {0}: Rect 下未知子标签 \"{1}\"，已忽略",
                            id,
                            child.Name.LocalName));
                    break;
            }
        }

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
            Fill = fillBrush,
            Stroke = strokeBrush,
            StrokeThickness = GetOptionalDouble(element, "StrokeThickness", context) ?? 0,
            CornerRadius = GetOptionalCornerRadius(element, id, context),
            Margin = GetOptionalThickness(element, "Margin", context),
            Shadow = shadowChild ?? GetOptionalShadow(element, "Shadow"),
            ShadowString = GetOptionalString(element, "Shadow"),
            StrokeDashArray = GetOptionalDoubleList(element, "StrokeDashArray", context),
        };
    }

    private SlideTextElement ParseTextElement(XElement element, string id, SlidePipelineContext context)
    {
        ValidateAttributes(element, id, _textElementKnownAttributes, context);

        var spans = new List<SlideSpan>();
        foreach (var child in element.Elements("Span"))
        {
            spans.Add(ParseSpan(child, id, context));
        }

        var text = GetOptionalString(element, "Text");
        if (string.IsNullOrWhiteSpace(text) && spans.Count == 0)
        {
            throw new InvalidOperationException($"TextElement({id}) 必须包含 Text 属性或 Span 子元素。");
        }

        if (string.IsNullOrWhiteSpace(text) && spans.Count > 0)
        {
            text = string.Concat(spans.Select(s => s.Text));
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
            Text = text!,
            FontName = GetOptionalString(element, "FontName") ?? "Microsoft YaHei",
            FontSize = GetOptionalDouble(element, "FontSize", context) ?? 16,
            Foreground = GetOptionalString(element, "Foreground") ?? "#000000",
            TextAlignment = GetOptionalTextAlignment(element, id, context) ?? SlideTextAlignment.Left,
            LineHeight = GetOptionalDouble(element, "LineHeight", context) ?? 1.2,
            Margin = GetOptionalThickness(element, "Margin", context),
            FontWeight = GetOptionalFontWeight(element, id, context),
            Style = GetOptionalString(element, "Style"),
            Spans = spans.Count > 0 ? spans : null,
        };
    }

    private static SlideSpan ParseSpan(XElement element, string parentId, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, "Text");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"TextElement({parentId}) 的 Span 子元素必须包含 Text 属性。");
        }

        return new SlideSpan
        {
            Text = text,
            FontSize = GetOptionalDouble(element, "FontSize", context),
            FontName = GetOptionalString(element, "FontName"),
            Foreground = GetOptionalString(element, "Foreground"),
            FontWeight = GetOptionalFontWeight(element, parentId, context),
            FontStyle = GetOptionalString(element, "FontStyle"),
            TextDecoration = GetOptionalString(element, "TextDecoration"),
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
            Margin = GetOptionalThickness(element, "Margin", context),
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

    private static SlideLayoutDirection? GetOptionalLayoutDirection(XElement element, string elementId, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, "Layout");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideLayoutDirection>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: Layout 值 \"{1}\" 无效，已忽略（有效值：Absolute, Horizontal, Vertical）",
                elementId,
                text));
        return null;
    }

    private static SlideThickness? GetOptionalThickness(XElement element, string attributeName, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var result = SlideThickness.Parse(text);
        if (result is null)
        {
            var elementId = GetOptionalString(element, "Id");
            context.AddError(
                string.Format(
                    "[Error] {0}: {1} 值 \"{2}\" 不是有效的间距格式（应为逗号分隔的 1~4 个数值）",
                    elementId ?? "unknown",
                    attributeName,
                    text));
        }

        return result;
    }

    private static SlideCornerRadius? GetOptionalCornerRadius(XElement element, string elementId, SlidePipelineContext context)
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

        context.AddError(
            string.Format(
                "[Error] {0}: CornerRadius 值 \"{1}\" 无效，已忽略",
                elementId,
                text));
        return null;
    }

    private static SlideShadow? GetOptionalShadow(XElement element, string attributeName)
    {
        var text = GetOptionalString(element, attributeName);
        return SlideShadow.Parse(text);
    }

    private static IReadOnlyList<double>? GetOptionalDoubleList(XElement element, string attributeName, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var list = new List<double>(parts.Length);
        foreach (var part in parts)
        {
            if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                list.Add(v);
            }
            else
            {
                var elementId = GetOptionalString(element, "Id");
                context.AddError(
                    string.Format(
                        "[Error] {0}: {1} 值 \"{2}\" 包含无效数值，已忽略",
                        elementId ?? "unknown",
                        attributeName,
                        part));
                return null;
            }
        }

        return list;
    }

    private static ISlideMlBrush? ParseBackground(XElement element, string id, SlidePipelineContext context)
    {
        var gradient = ParseGradientChild(element, "Fill", id, context);
        if (gradient is not null)
        {
            return gradient;
        }

        var color = GetOptionalString(element, "Background");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideMlSolidColorBrush { Color = color };
        }

        return null;
    }

    private static ISlideMlBrush? ParseFill(XElement element, string id, SlidePipelineContext context)
    {
        var gradient = ParseGradientChild(element, "Fill", id, context);
        if (gradient is not null)
        {
            return gradient;
        }

        var color = GetOptionalString(element, "Fill");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideMlSolidColorBrush { Color = color };
        }

        return null;
    }

    private static ISlideMlBrush? ParseStroke(XElement element, string id, SlidePipelineContext context)
    {
        var gradient = ParseGradientChild(element, "Stroke", id, context);
        if (gradient is not null)
        {
            return gradient;
        }

        var color = GetOptionalString(element, "Stroke");
        if (!string.IsNullOrWhiteSpace(color))
        {
            return new SlideMlSolidColorBrush { Color = color };
        }

        return null;
    }

    private static SlideMlLinearGradientBrush? ParseGradientChild(XElement parentElement, string childName, string elementId, SlidePipelineContext context)
    {
        var fillElement = parentElement.Element(childName);
        if (fillElement is null)
        {
            return null;
        }

        var gradientElement = fillElement.Element("LinearGradient");
        if (gradientElement is null)
        {
            return null;
        }

        return ParseLinearGradient(gradientElement, elementId, context);
    }

    private static SlideMlLinearGradientBrush? ParseLinearGradient(XElement gradientElement, string elementId, SlidePipelineContext context)
    {
        var stops = new List<SlideMlGradientStop>();
        foreach (var stopElement in gradientElement.Elements("Stop"))
        {
            var offset = GetOptionalDouble(stopElement, "Offset", context);
            var color = GetOptionalString(stopElement, "Color");
            if (offset is null || string.IsNullOrWhiteSpace(color))
            {
                context.AddWarning(
                    string.Format(
                        "[Warning] {0}: LinearGradient Stop 缺少 Offset 或 Color，已忽略",
                        elementId));
                continue;
            }

            stops.Add(new SlideMlGradientStop
            {
                Offset = Math.Clamp(offset.Value, 0, 1),
                Color = color,
            });
        }

        if (stops.Count == 0)
        {
            context.AddWarning(
                string.Format(
                    "[Warning] {0}: LinearGradient 不包含有效 Stop，已忽略",
                    elementId));
            return null;
        }

        return new SlideMlLinearGradientBrush
        {
            X1 = GetOptionalDouble(gradientElement, "X1", context) ?? 0,
            Y1 = GetOptionalDouble(gradientElement, "Y1", context) ?? 0,
            X2 = GetOptionalDouble(gradientElement, "X2", context) ?? 1,
            Y2 = GetOptionalDouble(gradientElement, "Y2", context) ?? 0,
            Stops = stops,
        };
    }

    private static SlideShadow? ParseShadowElement(XElement element, string elementId, SlidePipelineContext context)
    {
        return new SlideShadow
        {
            OffsetX = GetOptionalDouble(element, "OffsetX", context) ?? 0,
            OffsetY = GetOptionalDouble(element, "OffsetY", context) ?? 4,
            Blur = GetOptionalDouble(element, "Blur", context) ?? 12,
            Color = GetOptionalString(element, "Color") ?? "#00000033",
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
        };
    }

    private static SlideFontWeight? GetOptionalFontWeight(XElement element, string elementId, SlidePipelineContext context)
    {
        var text = GetOptionalString(element, "FontWeight");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideFontWeight>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue)
            && numericValue >= 100 && numericValue <= 900)
        {
            return (SlideFontWeight)numericValue;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: FontWeight 值 \"{1}\" 无效，已忽略",
                elementId,
                text));
        return null;
    }
}