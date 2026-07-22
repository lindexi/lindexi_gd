using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Resolves design tokens and typed slots into the existing SlideML syntax without extending the parser grammar.
/// </summary>
public sealed class CoursewareTemplateCompiler
{
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{(?<kind>token|slot):(?<id>[A-Za-z0-9_.-]+)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> AllowedElementNames = new(StringComparer.Ordinal)
    {
        "Page", "Panel", "Rect", "TextElement", "Image", "Span", "Fill", "Stroke", "Shadow", "LinearGradient", "Stop",
    };
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedAttributes =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["Page"] = CreateSet("Id", "Background"),
            ["Panel"] = CreateSet("Id", "X", "Y", "Width", "Height", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Padding", "Background", "Layout", "Gap", "Margin"),
            ["Rect"] = CreateSet("Id", "X", "Y", "Width", "Height", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Fill", "Stroke", "StrokeThickness", "CornerRadius", "Margin", "Shadow", "StrokeDashArray"),
            ["TextElement"] = CreateSet("Id", "X", "Y", "Width", "Height", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Text", "FontName", "FontSize", "Foreground", "TextAlignment", "Margin", "IsBold", "IsItalic"),
            ["Image"] = CreateSet("Id", "X", "Y", "Width", "Height", "HorizontalAlignment", "VerticalAlignment", "Opacity", "Source", "Stretch", "Margin"),
            ["Span"] = CreateSet("Text", "FontName", "FontSize", "Foreground", "IsBold", "IsItalic"),
            ["Fill"] = CreateSet("Color"),
            ["Stroke"] = CreateSet("Color", "Thickness", "DashArray"),
            ["Shadow"] = CreateSet("Color", "BlurRadius", "Direction", "Distance", "Opacity"),
            ["LinearGradient"] = CreateSet("StartPoint", "EndPoint"),
            ["Stop"] = CreateSet("Offset", "Color"),
        };

    /// <summary>
    /// Compiles one template for a specific canvas and slot-binding set.
    /// </summary>
    public CoursewareTemplateCompilationResult Compile(
        CoursewarePageTemplate template,
        CoursewareDesignSystem designSystem,
        CoursewareCanvasDesignProfile canvas,
        IReadOnlyDictionary<string, string> slotValues,
        IReadOnlySet<string> allowedResourceIds)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(designSystem);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(slotValues);
        ArgumentNullException.ThrowIfNull(allowedResourceIds);

        var diagnostics = new List<CoursewareValidationDiagnostic>();
        var tokens = CreateTokenValues(designSystem, canvas);
        XDocument document;
        try
        {
            document = XDocument.Parse(template.SlideMlTemplate, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException)
        {
            return Failed("TemplateXmlInvalid", "slideMlTemplate", ex.Message);
        }

        var root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "Page", StringComparison.Ordinal))
        {
            return Failed("TemplateRootInvalid", "slideMlTemplate", "SlideML 模板根节点必须是 Page。");
        }

        foreach (var element in root.DescendantsAndSelf())
        {
            var elementName = element.Name.LocalName;
            if (!AllowedElementNames.Contains(elementName))
            {
                AddError("UnknownSlideMlElement", elementName, $"未知 SlideML 标签：{elementName}", diagnostics);
                continue;
            }

            foreach (var attribute in element.Attributes().ToArray())
            {
                if (!AllowedAttributes[elementName].Contains(attribute.Name.LocalName))
                {
                    AddError("UnknownSlideMlAttribute", $"{elementName}.{attribute.Name.LocalName}", $"未知 SlideML 属性：{attribute.Name.LocalName}", diagnostics);
                    continue;
                }

                attribute.Value = ResolvePlaceholders(attribute.Value, tokens, slotValues, diagnostics, $"{elementName}.{attribute.Name.LocalName}");
            }

            if (!element.HasElements && !string.IsNullOrEmpty(element.Value))
            {
                element.Value = ResolvePlaceholders(element.Value, tokens, slotValues, diagnostics, elementName);
            }

            if (string.Equals(elementName, "Image", StringComparison.Ordinal))
            {
                var source = (string?)element.Attribute("Source");
                if (!string.IsNullOrWhiteSpace(source) && !allowedResourceIds.Contains(source))
                {
                    AddError("UnknownTemplateResource", $"Image[{(string?)element.Attribute("Id") ?? ""}].Source", $"模板引用未知 ResourceId：{source}", diagnostics);
                }
            }
        }

        foreach (var slot in template.Slots.Where(slot => slot.IsRequired && !slotValues.ContainsKey(slot.SlotId)))
        {
            AddError("RequiredSlotMissing", $"slots[{slot.SlotId}]", $"缺少必需槽位：{slot.SlotId}", diagnostics);
        }

        var unresolved = PlaceholderRegex.Matches(document.ToString(SaveOptions.DisableFormatting));
        foreach (Match match in unresolved)
        {
            AddError("UnresolvedTemplatePlaceholder", "slideMlTemplate", $"未解析模板占位符：{match.Value}", diagnostics);
        }

        return new CoursewareTemplateCompilationResult
        {
            Success = diagnostics.All(diagnostic => diagnostic.Severity != CoursewareValidationSeverity.Error),
            CompiledSlideMl = document.ToString(SaveOptions.DisableFormatting),
            Diagnostics = diagnostics,
        };
    }

    private static IReadOnlyDictionary<string, string> CreateTokenValues(
        CoursewareDesignSystem designSystem,
        CoursewareCanvasDesignProfile canvas)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["canvas.width"] = canvas.Width.ToString(CultureInfo.InvariantCulture),
            ["canvas.height"] = canvas.Height.ToString(CultureInfo.InvariantCulture),
            ["grid.safeLeft"] = designSystem.Grid.SafeLeft.ToString(CultureInfo.InvariantCulture),
            ["grid.safeTop"] = designSystem.Grid.SafeTop.ToString(CultureInfo.InvariantCulture),
            ["grid.safeRight"] = designSystem.Grid.SafeRight.ToString(CultureInfo.InvariantCulture),
            ["grid.safeBottom"] = designSystem.Grid.SafeBottom.ToString(CultureInfo.InvariantCulture),
            ["grid.gutter"] = designSystem.Grid.Gutter.ToString(CultureInfo.InvariantCulture),
        };
        foreach (var token in designSystem.Spacing.Tokens)
        {
            values[token.TokenId] = token.Value.ToString(CultureInfo.InvariantCulture);
        }

        foreach (var token in designSystem.Typography.Tokens)
        {
            values[token.TokenId] = token.FontSize.ToString(CultureInfo.InvariantCulture);
            values[$"{token.TokenId}.fontName"] = token.EastAsianFontStack.FirstOrDefault()
                ?? token.LatinFontStack.FirstOrDefault()
                ?? string.Empty;
        }

        foreach (var token in designSystem.Colors.Tokens)
        {
            values[token.TokenId] = token.HexValue;
        }

        foreach (var token in designSystem.Effects.Tokens)
        {
            values[token.TokenId] = token.Value;
        }

        return values;
    }

    private static string ResolvePlaceholders(
        string value,
        IReadOnlyDictionary<string, string> tokens,
        IReadOnlyDictionary<string, string> slots,
        ICollection<CoursewareValidationDiagnostic> diagnostics,
        string path)
    {
        return PlaceholderRegex.Replace(value, match =>
        {
            var kind = match.Groups["kind"].Value;
            var id = match.Groups["id"].Value;
            var source = string.Equals(kind, "token", StringComparison.Ordinal) ? tokens : slots;
            if (source.TryGetValue(id, out var replacement))
            {
                return replacement;
            }

            AddError(
                string.Equals(kind, "token", StringComparison.Ordinal) ? "UnknownTemplateToken" : "UnknownTemplateSlot",
                path,
                $"模板引用未知{(kind == "token" ? "令牌" : "槽位")}：{id}",
                diagnostics);
            return match.Value;
        });
    }

    private static IReadOnlySet<string> CreateSet(params string[] values)
    {
        return values.ToHashSet(StringComparer.Ordinal);
    }

    private static CoursewareTemplateCompilationResult Failed(string code, string path, string message)
    {
        return new CoursewareTemplateCompilationResult
        {
            Diagnostics =
            [
                new CoursewareValidationDiagnostic
                {
                    Code = code,
                    Path = path,
                    Message = message,
                    Severity = CoursewareValidationSeverity.Error,
                },
            ],
        };
    }

    private static void AddError(
        string code,
        string path,
        string message,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        diagnostics.Add(new CoursewareValidationDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Severity = CoursewareValidationSeverity.Error,
        });
    }
}

/// <summary>
/// Represents one deterministic template compilation result before real rendering.
/// </summary>
public sealed record CoursewareTemplateCompilationResult
{
    /// <summary>Gets whether template expansion and static validation passed.</summary>
    public bool Success { get; set; }

    /// <summary>Gets compiled SlideML.</summary>
    public string CompiledSlideMl { get; set; } = string.Empty;

    /// <summary>Gets field-level diagnostics.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];
}
