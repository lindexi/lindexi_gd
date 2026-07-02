using System.Globalization;
using System.Xml.Linq;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Models;

/// <summary>
/// SlideML 解析器，将 XML 字符串解析为 <see cref="SlideMlPage"/> 对象树。
/// 无 UI 框架依赖。
/// </summary>
public sealed class SlideMlParser
{
    private int _nextId = 1;

    /// <summary>
    /// 必须携带 Id 属性的元素类型集合。
    /// </summary>
    private static readonly HashSet<string> _idRequiredElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Panel", "Rect", "TextElement", "Image",
    };

    /// <summary>
    /// 不需要 Id 的结构化子元素集合（Fill、Stroke、Shadow、Span、LinearGradient、Stop）。
    /// </summary>
    private static readonly HashSet<string> _structuredElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Span", "Fill", "Stroke", "Shadow", "LinearGradient", "Stop",
    };

    /// <summary>
    /// 可重复出现的结构化子元素集合（Span、Stop）。
    /// </summary>
    private static readonly HashSet<string> _repeatableStructuredElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Span", "Stop",
    };

    /// <summary>
    /// 判断元素是否为需要 Id 的类型（Panel、Rect、TextElement、Image）。
    /// 供 <see cref="Streaming.SlideMlStreamingMerger"/> 在合并时判断元素分类。
    /// </summary>
    /// <param name="localName">元素标签名。</param>
    /// <returns>需要 Id 返回 true，否则 false。</returns>
    public static bool IsIdRequiredElement(string localName)
        => _idRequiredElements.Contains(localName);

    /// <summary>
    /// 判断元素是否为结构化子元素（Span、Fill、Stroke、Shadow、LinearGradient、Stop）。
    /// 供 <see cref="Streaming.SlideMlStreamingMerger"/> 在合并时跳过不需要 Id 的子元素。
    /// </summary>
    /// <param name="localName">元素标签名。</param>
    /// <returns>是结构化子元素返回 true，否则 false。</returns>
    public static bool IsStructuredElement(string localName)
        => _structuredElements.Contains(localName);

    /// <summary>
    /// 判断结构化子元素是否可重复出现（Span、Stop）。
    /// 供 <see cref="Streaming.SlideMlStreamingMerger"/> 在合并时决定替换策略。
    /// </summary>
    /// <param name="localName">元素标签名。</param>
    /// <returns>可重复返回 true，否则 false。</returns>
    public static bool IsRepeatableStructuredElement(string localName)
        => _repeatableStructuredElements.Contains(localName);

    private static readonly HashSet<string> _pageKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Background",
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
        "TextAlignment", "Margin",
        "IsBold", "IsItalic",
    };

    private static readonly HashSet<string> _imageKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "X", "Y", "Width", "Height",
        "HorizontalAlignment", "VerticalAlignment", "Opacity",
        "Source", "Stretch", "Margin",
    };

    /// <summary>
    /// 将 SlideML XML 解析为 <see cref="SlideMlPage"/> 对象。
    /// </summary>
    /// <param name="xml">SlideML XML 字符串。</param>
    /// <param name="context">渲染流水线上下文，用于收集诊断信息。</param>
    /// <returns>解析后的页面对象。</returns>
    public SlideMlPage Parse(string xml, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(context);

        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new SlideMlRootElementException("SlideML 根元素不能为空。");
        if (!string.Equals(root.Name.LocalName, "Page", StringComparison.Ordinal))
        {
            throw new SlideMlRootElementException("SlideML 根元素必须是 Page。");
        }

        var pageId = GetOptionalString(root, "Id") ?? $"elem_{_nextId++:000}";

        ValidateAttributes(root, pageId, _pageKnownAttributes, context);

        var page = new SlideMlPage
        {
            Id = pageId,
            Background = GetOptionalString(root, "Background") ?? "#FFFFFF",
        };

        foreach (var child in root.Elements())
        {
            var element = ParseElement(child, $"Page({pageId})", context);
            if (element is not null)
            {
                page.Children.Add(element);
            }
        }

        return page;
    }

    private SlideMlElement? ParseElement(XElement element, string parentName, SlideMlPipelineContext context)
    {
        var localName = element.Name.LocalName;

        if (_idRequiredElements.Contains(localName))
        {
            return localName switch
            {
                "Panel" => ParsePanel(element, GetOrCreateElementId(element), context),
                "Rect" => ParseRect(element, GetOrCreateElementId(element), context),
                "TextElement" => ParseTextElement(element, GetOrCreateElementId(element), context),
                "Image" => ParseImageElement(element, GetOrCreateElementId(element), context),
                _ => WarnUnknownTag(element, parentName, context),
            };
        }

        if (_structuredElements.Contains(localName))
        {
            return WarnInvalidStructuredChild(element, parentName, context);
        }

        return WarnUnknownTag(element, parentName, context);
    }

    private string GetOrCreateElementId(XElement element)
    {
        return GetOptionalString(element, "Id") ?? $"elem_{_nextId++:000}";
    }

    private static SlideMlElement? WarnInvalidStructuredChild(XElement element, string parentName, SlideMlPipelineContext context)
    {
        var tagName = element.Name.LocalName;
        var supportedParent = GetSupportedParent(tagName);

        context.AddWarning($"[Warning] {parentName}: 子标签 \"{tagName}\" 位置无效，{tagName} 只能用于 {supportedParent}，已忽略");
        return null;
    }

    /// <summary>
    /// 获取结构化子元素对应的合法父元素描述文本，用于生成诊断消息。
    /// </summary>
    /// <param name="elementName">子元素标签名。</param>
    /// <returns>合法父元素的描述，如 "Panel 或 Rect"。</returns>
    private static string GetSupportedParent(string elementName)
    {
        return elementName switch
        {
            "Fill" or "Stroke" => "Panel 或 Rect",
            "Shadow" => "Rect",
            "Span" => "TextElement",
            "LinearGradient" => "Fill 或 Stroke",
            "Stop" => "LinearGradient",
            _ => elementName,
        };
    }

    private static SlideMlElement? WarnUnknownTag(XElement element, string parentName, SlideMlPipelineContext context)
    {
        context.AddWarning(
            string.Format(
                "[Warning] {0}: 未知标签 \"{1}\"，已忽略",
                parentName,
                element.Name.LocalName));
        return null;
    }

    private SlideMlPanelElement ParsePanel(XElement element, string id, SlideMlPipelineContext context)
    {
        ValidateAttributes(element, id, _panelKnownAttributes, context);

        var backgroundBrush = ParseBackground(element, id, context);

        var panel = new SlideMlPanelElement
        {
            Id = id,
            X = GetOptionalDouble(element, "X", context),
            Y = GetOptionalDouble(element, "Y", context),
            Width = GetOptionalDouble(element, "Width", context),
            Height = GetOptionalDouble(element, "Height", context),
            HorizontalAlignment = GetOptionalHorizontalAlignment(element, id, context),
            VerticalAlignment = GetOptionalVerticalAlignment(element, id, context),
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
            Padding = GetOptionalThickness(element, "Padding", context) ?? default,
            Background = backgroundBrush,
            Layout = GetOptionalLayoutDirection(element, id, context) ?? SlideMlLayoutDirection.Absolute,
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

            var childElement = ParseElement(child, $"Panel({id})", context);
            if (childElement is not null)
            {
                panel.Children.Add(childElement);
            }
        }

        return panel;
    }

    private SlideMlRectElement ParseRect(XElement element, string id, SlideMlPipelineContext context)
    {
        ValidateAttributes(element, id, _rectKnownAttributes, context);

        var fillBrush = ParseFill(element, id, context);
        var strokeBrush = ParseStroke(element, id, context);
        SlideMlShadow? shadowChild = null;

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

        return new SlideMlRectElement
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

    private SlideMlTextElement ParseTextElement(XElement element, string id, SlideMlPipelineContext context)
    {
        ValidateAttributes(element, id, _textElementKnownAttributes, context);

        var spans = new List<SlideMlSpan>();
        foreach (var child in element.Elements())
        {
            if (string.Equals(child.Name.LocalName, "Span", StringComparison.Ordinal))
            {
                spans.Add(ParseSpan(child, id, context));
            }
            else
            {
                context.AddWarning($"[Warning] {id}: TextElement 下未知子标签 \"{child.Name.LocalName}\"，TextElement 只支持 Span 子元素，已忽略");
            }
        }

        string? text = GetOptionalString(element, "Text");
        if (text is null && spans.Count == 0)
        {
            // 为什么不能用 string.IsNullOrEmpty？因为空字符串也是合法的文本内容
            throw new SlideMlRequiredAttributeMissingException($"TextElement({id}) 必须包含 Text 属性或 Span 子元素。", id, "Text");
        }

        if (string.IsNullOrWhiteSpace(text) && spans.Count > 0)
        {
            text = string.Concat(spans.Select(s => s.Text));
        }

        return new SlideMlTextElement
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
            TextAlignment = GetOptionalTextAlignment(element, id, context) ?? SlideMlTextAlignment.Left,
            Margin = GetOptionalThickness(element, "Margin", context),
            IsBold = GetOptionalBool(element, "IsBold"),
            IsItalic = GetOptionalBool(element, "IsItalic"),
            Spans = spans.Count > 0 ? spans : null,
        };
    }

    private static SlideMlSpan ParseSpan(XElement element, string parentId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "Text");
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new SlideMlRequiredAttributeMissingException($"TextElement({parentId}) 的 Span 子元素必须包含 Text 属性。", parentId, "Text");
        }

        return new SlideMlSpan
        {
            Text = text,
            FontSize = GetOptionalDouble(element, "FontSize", context),
            FontName = GetOptionalString(element, "FontName"),
            Foreground = GetOptionalString(element, "Foreground"),
            IsBold = GetOptionalBool(element, "IsBold"),
            IsItalic = GetOptionalBool(element, "IsItalic"),
            TextDecoration = GetOptionalString(element, "TextDecoration"),
        };
    }

    private SlideMlImageElement ParseImageElement(XElement element, string id, SlideMlPipelineContext context)
    {
        ValidateAttributes(element, id, _imageKnownAttributes, context);

        var source = GetOptionalString(element, "Source");
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new SlideMlRequiredAttributeMissingException($"Image({id}) 必须包含 Source 属性。", id, "Source");
        }

        return new SlideMlImageElement
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
            Stretch = GetOptionalImageStretch(element, id, context) ?? SlideMlImageStretch.Uniform,
            Margin = GetOptionalThickness(element, "Margin", context),
        };
    }

    private static void ValidateAttributes(XElement element, string elementId,
        HashSet<string> knownAttributes, SlideMlPipelineContext context)
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

    private static double? GetOptionalDouble(XElement element, string attributeName, SlideMlPipelineContext context)
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

    private static SlideMlHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "HorizontalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideMlHorizontalAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: HorizontalAlignment 值 \"{1}\" 无效（有效值：Left, Center, Right）",
                elementId,
                text));
        return null;
    }

    private static SlideMlVerticalAlignment? GetOptionalVerticalAlignment(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "VerticalAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideMlVerticalAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: VerticalAlignment 值 \"{1}\" 无效（有效值：Top, Center, Bottom）",
                elementId,
                text));
        return null;
    }

    private static SlideMlTextAlignment? GetOptionalTextAlignment(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "TextAlignment");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideMlTextAlignment>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: TextAlignment 值 \"{1}\" 无效（有效值：Left, Center, Right, Justify）",
                elementId,
                text));
        return null;
    }

    private static SlideMlImageStretch? GetOptionalImageStretch(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "Stretch");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideMlImageStretch>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: Stretch 值 \"{1}\" 无效（有效值：None, Fill, Uniform, UniformToFill）",
                elementId,
                text));
        return null;
    }

    private static SlideMlLayoutDirection? GetOptionalLayoutDirection(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "Layout");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (Enum.TryParse<SlideMlLayoutDirection>(text, ignoreCase: true, out var result))
        {
            return result;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: Layout 值 \"{1}\" 无效（有效值：Absolute, Horizontal, Vertical）",
                elementId,
                text));
        return null;
    }

    private static SlideMlThickness? GetOptionalThickness(XElement element, string attributeName, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var result = SlideMlThickness.Parse(text);
        if (result is null)
        {
            var elementId = GetOptionalString(element, "Id");
            context.AddError(
                string.Format(
                    "[Error] {0}: {1} 值 \"{2}\" 不是有效的间距格式（应为逗号或空格分隔的 1~4 个数值）",
                    elementId ?? "unknown",
                    attributeName,
                    text));
        }

        return result;
    }

    private static SlideMlCornerRadius? GetOptionalCornerRadius(XElement element, string elementId, SlideMlPipelineContext context)
    {
        var text = GetOptionalString(element, "CornerRadius");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cornerRadius = SlideMlCornerRadius.Parse(text);
        if (cornerRadius is not null)
        {
            return cornerRadius;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var singleValue))
        {
            return (SlideMlCornerRadius)singleValue;
        }

        context.AddError(
            string.Format(
                "[Error] {0}: CornerRadius 值 \"{1}\" 无效",
                elementId,
                text));
        return null;
    }

    private static SlideMlShadow? GetOptionalShadow(XElement element, string attributeName)
    {
        var text = GetOptionalString(element, attributeName);
        return SlideMlShadow.Parse(text);
    }

    private static IReadOnlyList<double>? GetOptionalDoubleList(XElement element, string attributeName, SlideMlPipelineContext context)
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
                        "[Error] {0}: {1} 值 \"{2}\" 包含无效数值",
                        elementId ?? "unknown",
                        attributeName,
                        part));
                return null;
            }
        }

        return list;
    }

    private static ISlideMlBrush? ParseBackground(XElement element, string id, SlideMlPipelineContext context)
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

    private static ISlideMlBrush? ParseFill(XElement element, string id, SlideMlPipelineContext context)
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

    private static ISlideMlBrush? ParseStroke(XElement element, string id, SlideMlPipelineContext context)
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

    private static SlideMlLinearGradientBrush? ParseGradientChild(XElement parentElement, string childName, string elementId, SlideMlPipelineContext context)
    {
        var fillElement = parentElement.Element(childName);
        if (fillElement is null)
        {
            return null;
        }

        XElement? gradientElement = null;
        foreach (var child in fillElement.Elements())
        {
            if (string.Equals(child.Name.LocalName, "LinearGradient", StringComparison.Ordinal))
            {
                if (gradientElement is null)
                {
                    gradientElement = child;
                }
                else
                {
                    context.AddWarning($"[Warning] {elementId}: {childName} 包含多个 LinearGradient 子元素，仅使用第一个，已忽略后续 LinearGradient");
                }
            }
            else
            {
                context.AddWarning($"[Warning] {elementId}: {childName} 下未知子标签 \"{child.Name.LocalName}\"，{childName} 只支持 LinearGradient 子元素，已忽略");
            }
        }

        if (gradientElement is null)
        {
            return null;
        }

        return ParseLinearGradient(gradientElement, elementId, context);
    }

    private static SlideMlLinearGradientBrush? ParseLinearGradient(XElement gradientElement, string elementId, SlideMlPipelineContext context)
    {
        var stops = new List<SlideMlGradientStop>();
        foreach (var stopElement in gradientElement.Elements())
        {
            if (!string.Equals(stopElement.Name.LocalName, "Stop", StringComparison.Ordinal))
            {
                context.AddWarning($"[Warning] {elementId}: LinearGradient 下未知子标签 \"{stopElement.Name.LocalName}\"，LinearGradient 只支持 Stop 子元素，已忽略");
                continue;
            }

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

    private static SlideMlShadow? ParseShadowElement(XElement element, string elementId, SlideMlPipelineContext context)
    {
        return new SlideMlShadow
        {
            OffsetX = GetOptionalDouble(element, "OffsetX", context) ?? 0,
            OffsetY = GetOptionalDouble(element, "OffsetY", context) ?? 4,
            Blur = GetOptionalDouble(element, "Blur", context) ?? 12,
            Color = GetOptionalString(element, "Color") ?? "#00000033",
            Opacity = GetOptionalDouble(element, "Opacity", context) ?? 1,
        };
    }

    private static bool? GetOptionalBool(XElement element, string attributeName)
    {
        var text = GetOptionalString(element, attributeName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (bool.TryParse(text, out var result))
        {
            return result;
        }

        return null;
    }
}
