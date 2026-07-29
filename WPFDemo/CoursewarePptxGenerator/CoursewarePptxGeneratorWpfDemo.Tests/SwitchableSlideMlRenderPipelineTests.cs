using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class SwitchableSlideMlRenderPipelineTests
{
    [TestMethod]
    public async Task RenderAsync_UsesLocalPipelineWhenMcpIsDisabled()
    {
        var localPipeline = new FakeSlideMlRenderPipeline();
        var pipeline = new SwitchableSlideMlRenderPipeline(localPipeline);

        var result = await pipeline.RenderAsync("<Page/>");

        Assert.IsFalse(pipeline.IsMcpEnabled);
        Assert.AreEqual("<Page/>", result.OutputXml);
        CollectionAssert.AreEqual(new[] { "<Page/>" }, localPipeline.RenderedSlideXml);
    }

    [TestMethod]
    public async Task TryEnableMcpAsync_WithEmptyUrlAtomicallyKeepsLocalPipeline()
    {
        var localPipeline = new FakeSlideMlRenderPipeline();
        var pipeline = new SwitchableSlideMlRenderPipeline(localPipeline);

        var enabled = await pipeline.TryEnableMcpAsync(null);
        var result = await pipeline.RenderAsync("<Page Background=\"#FFFFFF\"/>");

        Assert.IsFalse(enabled);
        Assert.IsFalse(pipeline.IsMcpEnabled);
        Assert.AreEqual("<Page Background=\"#FFFFFF\"/>", result.OutputXml);
    }

    [TestMethod]
    public async Task TryEnableMcpAsync_HonorsPreCanceledToken()
    {
        var pipeline = new SwitchableSlideMlRenderPipeline(new FakeSlideMlRenderPipeline());
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipeline.TryEnableMcpAsync("http://127.0.0.1:1/mcp", cancellationTokenSource.Token));

        Assert.IsFalse(pipeline.IsMcpEnabled);
    }
}
