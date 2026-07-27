using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    public List<string> RenderedSlideXml { get; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];

    public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RenderedSlideXml.Add(slideXml);
        return Task.FromResult(new SlideMlRenderResult
        {
            InputXml = slideXml,
            OutputXml = slideXml,
            Warnings = Warnings,
            Errors = Errors,
        });
    }
}
