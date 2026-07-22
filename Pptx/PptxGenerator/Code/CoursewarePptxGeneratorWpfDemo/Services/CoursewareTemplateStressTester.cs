using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using PptxGenerator;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Compiles page templates and executes real WPF SlideML rendering stress tests.
/// </summary>
public sealed class CoursewareTemplateStressTester
{
    private readonly CoursewareTemplateCompiler _compiler;
    private readonly Func<CoursewareCanvasDesignProfile, ISlideMlRenderPipeline> _pipelineFactory;

    /// <summary>
    /// Initializes a stress tester with the production WPF rendering pipeline.
    /// </summary>
    public CoursewareTemplateStressTester()
        : this(
            new CoursewareTemplateCompiler(),
            canvas => new SlideMlRenderPipeline(
                new SlideMlLayoutEngine(),
                new WpfSlideMlRenderEngine(),
                WpfDispatcher.Instance,
                new SlideMlPipelineContext(CoursewareCanvasAdapter.CreateDocumentContext(canvas.Width, canvas.Height))))
    {
    }

    /// <summary>
    /// Initializes a stress tester with an injectable render-pipeline factory.
    /// </summary>
    public CoursewareTemplateStressTester(
        CoursewareTemplateCompiler compiler,
        Func<CoursewareCanvasDesignProfile, ISlideMlRenderPipeline> pipelineFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(pipelineFactory);
        _compiler = compiler;
        _pipelineFactory = pipelineFactory;
    }

    /// <summary>
    /// Compiles and renders every template against bounded short, medium, long and special-character samples.
    /// </summary>
    public async Task<CoursewareTemplateValidationReport> ValidateAsync(
        CoursewareDesignSystem designSystem,
        CoursewareStructuredFactReport factReport,
        IReadOnlySet<string> allowedResourceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        ArgumentNullException.ThrowIfNull(factReport);
        ArgumentNullException.ThrowIfNull(allowedResourceIds);

        var samples = new List<CoursewareTemplateStressSampleResult>();
        var diagnostics = new List<CoursewareValidationDiagnostic>();
        foreach (var template in designSystem.PageTemplates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageType = designSystem.PageTypes.FirstOrDefault(item => item.PageTypeId == template.PageTypeId);
            if (pageType is null)
            {
                diagnostics.Add(Error("UnknownTemplatePageType", $"pageTemplates[{template.TemplateId}].pageTypeId", $"模板引用未知页面类型：{template.PageTypeId}"));
                continue;
            }

            foreach (var canvas in designSystem.CanvasProfiles)
            {
                foreach (var sample in CreateStressSamples(template, pageType, factReport, allowedResourceIds))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var compilation = _compiler.Compile(template, designSystem, canvas, sample.Values, allowedResourceIds);
                    var sampleDiagnostics = compilation.Diagnostics.ToList();
                    if (compilation.Success)
                    {
                        var pipeline = _pipelineFactory(canvas);
                        var renderResult = await pipeline.RenderAsync(compilation.CompiledSlideMl, cancellationToken).ConfigureAwait(false);
                        sampleDiagnostics.AddRange(renderResult.Errors.Select(error => Error("SlideMlRenderError", $"pageTemplates[{template.TemplateId}]", error)));
                        sampleDiagnostics.AddRange(renderResult.Warnings.Select(warning => Warning("SlideMlRenderWarning", $"pageTemplates[{template.TemplateId}]", warning)));
                    }

                    samples.Add(new CoursewareTemplateStressSampleResult
                    {
                        SampleId = $"{template.TemplateId}:{canvas.CanvasId}:{sample.Kind}",
                        TemplateId = template.TemplateId,
                        CanvasId = canvas.CanvasId,
                        SampleKind = sample.Kind,
                        Passed = sampleDiagnostics.All(item => item.Severity != CoursewareValidationSeverity.Error),
                        CompiledSlideMl = compilation.CompiledSlideMl,
                        Diagnostics = sampleDiagnostics,
                    });
                }
            }
        }

        var passedTemplateCount = designSystem.PageTemplates.Count(template =>
            samples.Where(sample => sample.TemplateId == template.TemplateId).Any()
            && samples.Where(sample => sample.TemplateId == template.TemplateId).All(sample => sample.Passed));
        return new CoursewareTemplateValidationReport
        {
            IsValid = designSystem.PageTemplates.Count > 0
                && passedTemplateCount == designSystem.PageTemplates.Count
                && diagnostics.All(diagnostic => diagnostic.Severity != CoursewareValidationSeverity.Error),
            TemplateCount = designSystem.PageTemplates.Count,
            PassedTemplateCount = passedTemplateCount,
            Samples = samples,
            Diagnostics = diagnostics,
        };
    }

    private static IReadOnlyList<StressSample> CreateStressSamples(
        CoursewarePageTemplate template,
        CoursewarePageTypeContract pageType,
        CoursewareStructuredFactReport factReport,
        IReadOnlySet<string> allowedResourceIds)
    {
        var relatedSlides = factReport.Slides
            .Where(slide => pageType.EvidenceReferences.Any(reference => reference.SlideId == slide.SlideId))
            .OrderBy(slide => slide.TextCharacterCount)
            .ToArray();
        var lengths = relatedSlides.Select(slide => slide.TextCharacterCount).Where(length => length > 0).ToArray();
        var shortLength = lengths.FirstOrDefault(24);
        var mediumLength = lengths.Length == 0 ? 120 : lengths[lengths.Length / 2];
        var longLength = lengths.LastOrDefault(480);
        var result = new List<StressSample>
        {
            CreateSample("minimum", template.Slots, 8, allowedResourceIds),
            CreateSample("short", template.Slots, Math.Clamp(shortLength, 16, 80), allowedResourceIds),
            CreateSample("medium", template.Slots, Math.Clamp(mediumLength, 80, 240), allowedResourceIds),
            CreateSample("long", template.Slots, Math.Clamp(longLength * 2, 320, 1200), allowedResourceIds),
            CreateSpecialCharacterSample(template.Slots, allowedResourceIds),
        };
        if (template.Slots.Any(slot => IsImageSlot(slot.SlotKind)))
        {
            result.Add(CreateSample("missing-image", template.Slots, 48, new HashSet<string>(StringComparer.Ordinal)));
        }

        return result;
    }

    private static StressSample CreateSample(
        string kind,
        IReadOnlyList<CoursewareTemplateSlot> slots,
        int textLength,
        IReadOnlySet<string> allowedResourceIds)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in slots)
        {
            values[slot.SlotId] = IsImageSlot(slot.SlotKind)
                ? allowedResourceIds.FirstOrDefault() ?? string.Empty
                : CreateText(textLength, slot.SlotId);
        }

        return new StressSample(kind, values);
    }

    private static StressSample CreateSpecialCharacterSample(
        IReadOnlyList<CoursewareTemplateSlot> slots,
        IReadOnlySet<string> allowedResourceIds)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in slots)
        {
            values[slot.SlotId] = IsImageSlot(slot.SlotKind)
                ? allowedResourceIds.FirstOrDefault() ?? string.Empty
                : "中英混排 <A&B> \"引号\" 'apostrophe'\nSupercalifragilisticexpialidocious";
        }

        return new StressSample("special-characters", values);
    }

    private static string CreateText(int length, string slotId)
    {
        var seed = $"{slotId} 教学内容用于验证模板在真实长度压力下的换行、层级和安全区。";
        return string.Concat(Enumerable.Repeat(seed, Math.Max(1, (int)Math.Ceiling((double)length / seed.Length))))[..length];
    }

    private static bool IsImageSlot(string slotKind)
    {
        return slotKind.Contains("Image", StringComparison.OrdinalIgnoreCase)
            || slotKind.Contains("Asset", StringComparison.OrdinalIgnoreCase)
            || slotKind.Contains("图片", StringComparison.OrdinalIgnoreCase)
            || slotKind.Contains("素材", StringComparison.OrdinalIgnoreCase);
    }

    private static CoursewareValidationDiagnostic Error(string code, string path, string message)
    {
        return new CoursewareValidationDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Severity = CoursewareValidationSeverity.Error,
        };
    }

    private static CoursewareValidationDiagnostic Warning(string code, string path, string message)
    {
        return new CoursewareValidationDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Severity = CoursewareValidationSeverity.Warning,
        };
    }

    private sealed record StressSample(string Kind, IReadOnlyDictionary<string, string> Values);
}
