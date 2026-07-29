using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using CoursewarePptxGeneratorWpfDemo.Models;
using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Validates complete theme SlideML documents against the shared schema and the production rendering pipeline.
/// </summary>
public sealed class CoursewareThemeSlideMlValidator : ICoursewareThemeSlideMlValidator
{
    private static readonly HashSet<string> StreamingProtocolAttributes = new(StringComparer.Ordinal)
    {
        "Remove",
        "StyleFrom",
        "StyleId",
        "TargetId",
    };

    private static readonly string[] BlockingLayoutWarningFragments =
    [
        "超出画布",
        "超出父容器",
        "超出部分将被裁剪",
        "超出容器高度",
        "流式布局内容宽度",
        "流式布局内容高度",
    ];

    private static readonly HashSet<string> StreamingProtocolElements = new(StringComparer.Ordinal)
    {
        "Remove",
        "get_slide_state",
    };

    /// <summary>
    /// Initializes a validator that uses the production WPF SlideML rendering pipeline.
    /// </summary>
    public CoursewareThemeSlideMlValidator()
    {
    }

    /// <inheritdoc />
    public async Task<CoursewareThemeValidationResult> ValidateAsync(
        CoursewareTheme theme,
        SlideDocumentContext documentContext,
        IReadOnlySet<string> availableResourceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(documentContext);
        ArgumentNullException.ThrowIfNull(availableResourceIds);

        var errors = new List<string>();
        await ValidateDocumentAsync("CoverPageSlideMl", theme.CoverPageSlideMl, documentContext, availableResourceIds, errors, cancellationToken).ConfigureAwait(false);
        await ValidateDocumentAsync("ContentPageSlideMl", theme.ContentPageSlideMl, documentContext, availableResourceIds, errors, cancellationToken).ConfigureAwait(false);
        return new CoursewareThemeValidationResult { Errors = errors };
    }

    private async Task ValidateDocumentAsync(
        string fieldName,
        string slideMl,
        SlideDocumentContext documentContext,
        IReadOnlySet<string> availableResourceIds,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(slideMl))
        {
            return;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(slideMl, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException exception)
        {
            errors.Add($"{fieldName}: XML 无效：{exception.Message}");
            return;
        }

        var structureErrors = new List<string>();
        ValidateDocumentBoundary(document, structureErrors);
        if (document.Root is XElement root)
        {
            ValidateElement(root, parentSchema: null, availableResourceIds, structureErrors);
        }

        if (structureErrors.Count > 0)
        {
            errors.AddRange(structureErrors.Select(error => $"{fieldName}: {error}"));
            return;
        }

        await ValidateRenderedDocumentAsync(fieldName, slideMl, documentContext, errors, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateDocumentBoundary(XDocument document, List<string> errors)
    {
        if (document.Declaration is null)
        {
            errors.Add("必须包含 XML 声明。");
        }

        var invalidOuterNodes = document.Nodes()
            .Where(node => node is not XElement && node is not XText text || node is XText && !string.IsNullOrWhiteSpace(((XText)node).Value))
            .ToArray();
        if (invalidOuterNodes.Length > 0 || document.Elements().Count() != 1)
        {
            errors.Add("XML 声明之外只能包含唯一的 Page 根元素和格式空白，不能包含根外注释、处理指令、文本或其他节点。");
        }

        if (document.Root is null || !string.Equals(document.Root.Name.LocalName, "Page", StringComparison.Ordinal) || document.Root.Name.Namespace != XNamespace.None)
        {
            errors.Add("唯一根元素必须是不带命名空间的 Page。");
        }
    }

    private static void ValidateElement(
        XElement element,
        SlideMlElementSchema? parentSchema,
        IReadOnlySet<string> availableResourceIds,
        List<string> errors)
    {
        var elementName = element.Name.LocalName;
        if (element.Name.Namespace != XNamespace.None)
        {
            errors.Add($"元素 {elementName} 不允许使用 XML 命名空间。{GetLineSuffix(element)}");
        }

        if (StreamingProtocolElements.Contains(elementName))
        {
            errors.Add($"完整文档不允许流式协议元素 {elementName}。{GetLineSuffix(element)}");
            return;
        }

        var schema = SlideMlCompleteDocumentSchema.FindElement(elementName);
        if (schema is null)
        {
            errors.Add($"未知标签 {elementName}。{GetLineSuffix(element)}");
            return;
        }

        if (parentSchema is not null && !parentSchema.AllowedChildren.Contains(elementName, StringComparer.Ordinal))
        {
            errors.Add($"{elementName} 不能作为 {parentSchema.Name} 的直接子元素。{GetLineSuffix(element)}");
        }

        foreach (var attribute in element.Attributes())
        {
            var attributeName = attribute.Name.LocalName;
            if (StreamingProtocolAttributes.Contains(attributeName))
            {
                errors.Add($"完整文档不允许流式协议属性 {attributeName}。{GetLineSuffix(attribute)}");
                continue;
            }

            if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !schema.AllowedAttributes.Contains(attributeName, StringComparer.Ordinal))
            {
                errors.Add($"{elementName} 不支持属性 {attributeName}。{GetLineSuffix(attribute)}");
            }
        }

        if (string.Equals(elementName, "Image", StringComparison.Ordinal))
        {
            var source = (string?)element.Attribute("Source");
            if (!string.IsNullOrWhiteSpace(source) && !availableResourceIds.Contains(source))
            {
                errors.Add($"Image.Source 资源 {source} 不在可用资源 ID 中。{GetLineSuffix(element)}");
            }
        }

        if (string.Equals(elementName, "TextElement", StringComparison.Ordinal))
        {
            ValidateVisibleText(element, errors);
        }

        foreach (var child in element.Elements())
        {
            ValidateElement(child, schema, availableResourceIds, errors);
        }
    }

    private static void ValidateVisibleText(XElement element, List<string> errors)
    {
        var opacityText = (string?)element.Attribute("Opacity");
        if (double.TryParse(opacityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity) && opacity <= 0)
        {
            errors.Add($"TextElement 的 Opacity 必须大于 0，否则文本不可见。{GetLineSuffix(element)}");
        }

        var fontSizeText = (string?)element.Attribute("FontSize");
        if (double.TryParse(fontSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize) && fontSize <= 0)
        {
            errors.Add($"TextElement 的 FontSize 必须大于 0，否则文本不可见。{GetLineSuffix(element)}");
        }

        var foreground = (string?)element.Attribute("Foreground");
        if (foreground is { Length: 9 } && foreground.StartsWith('#') && string.Equals(foreground[1..3], "00", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"TextElement 的 Foreground 完全透明，文本不可见。{GetLineSuffix(element)}");
        }

        var effectiveFontSize = double.TryParse(fontSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFontSize)
            ? parsedFontSize
            : 16;
        var heightText = (string?)element.Attribute("Height");
        if (double.TryParse(heightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var fixedHeight)
            && effectiveFontSize > 0
            && fixedHeight + 0.1 < SlideMlLayoutEngine.CalculateDefaultTextLineHeight(effectiveFontSize))
        {
            errors.Add($"TextElement 固定高度 {fixedHeight.ToString(CultureInfo.InvariantCulture)} 小于单行文本所需高度，文本将被裁剪。{GetLineSuffix(element)}");
        }
    }

    private static string GetLineSuffix(XObject node)
    {
        var lineInfo = (IXmlLineInfo)node;
        return lineInfo.HasLineInfo()
            ? $"位置：第 {lineInfo.LineNumber} 行，第 {lineInfo.LinePosition} 列。"
            : string.Empty;
    }

    private async Task ValidateRenderedDocumentAsync(
        string fieldName,
        string slideMl,
        SlideDocumentContext documentContext,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            var pipeline = CreateProductionRenderPipeline(documentContext);
            var result = await pipeline.RenderAsync(slideMl, cancellationToken).ConfigureAwait(false);
            errors.AddRange(result.Errors.Select(error => $"{fieldName}: {error}"));
            errors.AddRange(result.Warnings
                .Where(IsBlockingLayoutWarning)
                .Select(warning => $"{fieldName}: {warning}"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or System.Windows.Media.InvalidWmpVersionException)
        {
            errors.Add($"{fieldName}: SlideML 真实渲染失败：{exception}");
        }
    }

    private static bool IsBlockingLayoutWarning(string warning)
    {
        return BlockingLayoutWarningFragments.Any(fragment => warning.Contains(fragment, StringComparison.Ordinal));
    }

    private static ISlideMlRenderPipeline CreateProductionRenderPipeline(SlideDocumentContext documentContext)
    {
        var context = new SlideMlPipelineContext(documentContext);
        return new SlideMlRenderPipeline(
            new SlideMlLayoutEngine(),
            new WpfSlideMlRenderEngine(enableClip: true),
            global::PptxGenerator.WpfDispatcher.BackgroundInstance,
            context);
    }
}