using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeSlideMlRenderPipeline : ISlideMlRenderPipeline
{
    public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SlideMlRenderResult
        {
            InputXml = slideXml,
            OutputXml = slideXml,
        });
    }
}
