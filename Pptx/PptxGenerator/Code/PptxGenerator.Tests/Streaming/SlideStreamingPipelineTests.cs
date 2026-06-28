using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideStreamingPipelineTests
{
    [TestMethod]
    public void ProcessIncrementalText_SingleFragment_TriggersFragmentReceived()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        pipeline.ProcessIncrementalText("<Page/>", context);

        // Assert
        Assert.AreEqual(1, fragments.Count);
        Assert.AreEqual("<Page/>", fragments[0]);
    }

    [TestMethod]
    public void ProcessIncrementalText_PartialFragment_NoEvent()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        pipeline.ProcessIncrementalText("<Panel Id=\"header\">", context);

        // Assert
        Assert.AreEqual(0, fragments.Count);
    }

    [TestMethod]
    public void ProcessIncrementalText_MultipleFragments_AllTriggered()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // Act
        pipeline.ProcessIncrementalText("<Page><Panel Id=\"a\"/></Page><Rect Id=\"b\"/>", context);

        // Assert
        Assert.AreEqual(2, fragments.Count);
    }

    [TestMethod]
    public void ProcessIncrementalText_MergedXml_UpdatedAfterFragment()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act
        pipeline.ProcessIncrementalText("<Page/>", context);

        // Assert
        Assert.IsTrue(pipeline.CurrentMergedXml.Contains("<Page"));
    }

    [TestMethod]
    public async Task ProcessStreamEndAsync_FlushesRemaining_RendersMergedXml()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedResults = new List<SlideStreamRenderResult>();
        pipeline.Rendered += renderedResults.Add;

        // Act
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r1\"/></Page>", context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert
        Assert.AreEqual(1, renderedResults.Count);
    }

    [TestMethod]
    public async Task ProcessStreamEndAsync_EmptyStream_NoRender()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act
        await pipeline.ProcessStreamEndAsync(context);

        // Assert
        Assert.AreEqual(0, renderedCount);
    }

    [TestMethod]
    public void Reset_ClearsState()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        pipeline.ProcessIncrementalText("<Page/>", context);

        // Act
        pipeline.Reset();

        // Assert
        Assert.IsTrue(string.IsNullOrEmpty(pipeline.CurrentMergedXml));
    }

    [TestMethod]
    public void ProcessIncrementalText_NullText_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act
        var act = () => pipeline.ProcessIncrementalText(null!, context);

        // Assert
        Assert.ThrowsExactly<ArgumentNullException>(act);
    }

    [TestMethod]
    public void ProcessIncrementalText_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act
        var act = () => pipeline.ProcessIncrementalText("text", null!);

        // Assert
        Assert.ThrowsExactly<ArgumentNullException>(act);
    }

    [TestMethod]
    public void ProcessIncrementalText_FragmentMergedIntoDom()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act
        pipeline.ProcessIncrementalText("<Page><Panel Id=\"p1\"><Rect Id=\"r1\"/></Panel></Page>", context);
        pipeline.ProcessIncrementalText("<Panel Id=\"p1\"><Rect Id=\"r2\"/></Panel>", context);

        // Assert
        Assert.IsTrue(pipeline.CurrentMergedXml.Contains("r1"));
        Assert.IsTrue(pipeline.CurrentMergedXml.Contains("r2"));
    }

    private static SlideStreamingPipeline CreatePipeline()
    {
        var renderResult = new SlideMlRenderResult
        {
            InputXml = "<Page/>",
            OutputXml = "<Page/>",
            Warnings = Array.Empty<string>(),
            Errors = Array.Empty<string>(),
            PreviewImage = new FakePreviewImage(),
        };
        var fakePipeline = new FakeRenderPipeline(renderResult);
        var dispatcher = new FakeMainThreadDispatcher();
        var promptProvider = new SlideMlPromptProvider();
        return new SlideStreamingPipeline(promptProvider, fakePipeline, dispatcher);
    }
}
