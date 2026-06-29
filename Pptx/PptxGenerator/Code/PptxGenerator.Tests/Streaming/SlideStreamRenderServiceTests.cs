using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Rendering;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// <see cref="SlideStreamRenderService"/> 单元测试。
/// </summary>
[TestClass]
public sealed class SlideStreamRenderServiceTests
{
    /// <summary>
    /// 构建带默认渲染结果的 FakeRenderPipeline。
    /// </summary>
    private static FakeRenderPipeline CreatePipeline(
        string outputXml = "<Page/>",
        IReadOnlyList<string>? warnings = null,
        IPreviewImage? previewImage = null)
    {
        var result = new SlideMlRenderResult
        {
            InputXml = "<Page/>",
            OutputXml = outputXml,
            Warnings = warnings ?? Array.Empty<string>(),
            Errors = Array.Empty<string>(),
            PreviewImage = previewImage ?? new FakePreviewImage(),
        };
        return new FakeRenderPipeline(result);
    }

    [TestMethod]
    public async Task RenderAsync_ValidXml_UpdatesState()
    {
        // Arrange
        const string outputXml = """<Page ActualWidth="1280" ActualHeight="720"/>""";
        var pipeline = CreatePipeline(outputXml: outputXml);
        var service = new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());

        // Act
        await service.RenderAsync("<Page/>").ConfigureAwait(false);

        // Assert
        Assert.AreEqual(outputXml, service.CurrentRenderedXml);
        Assert.IsNotNull(service.CurrentPreviewImage);
        Assert.IsEmpty(service.CurrentWarnings);
    }

    [TestMethod]
    public async Task RenderAsync_ValidXml_TriggersRenderedEvent()
    {
        // Arrange
        const string outputXml = """<Page ActualWidth="1280"/>""";
        var previewImage = new FakePreviewImage();
        var pipeline = CreatePipeline(outputXml: outputXml, previewImage: previewImage);
        var service = new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());

        SlideStreamRenderResult? eventResult = null;
        service.Rendered += result => eventResult = result;

        // Act
        await service.RenderAsync("<Page/>").ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(eventResult, "Rendered 事件应被触发");
        Assert.AreEqual(outputXml, eventResult!.OutputXml);
        Assert.AreSame(previewImage, eventResult.PreviewImage);
    }

    [TestMethod]
    public async Task RenderAsync_WithWarnings_StoresWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        var pipeline = CreatePipeline(warnings: warnings);
        var service = new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());

        // Act
        await service.RenderAsync("<Page/>").ConfigureAwait(false);

        // Assert
        Assert.HasCount(2, service.CurrentWarnings);
        Assert.AreEqual("Warning 1", service.CurrentWarnings[0]);
        Assert.AreEqual("Warning 2", service.CurrentWarnings[1]);
    }

    [TestMethod]
    public async Task RenderAsync_RenderThrows_ExceptionPropagated()
    {
        // Arrange
        var pipeline = new FakeRenderPipeline(new InvalidOperationException("render failed"));
        var service = new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await service.RenderAsync("<Page/>").ConfigureAwait(false));
    }

    [TestMethod]
    public async Task RenderAsync_CancellationToken_Propagated()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());
        using var cts = new CancellationTokenSource();

        // Act
        await service.RenderAsync("<Page/>", cts.Token).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(cts.Token, pipeline.LastCancellationToken);
    }

    [TestMethod]
    public void Constructor_NullRenderPipeline_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SlideStreamRenderService(null!, new FakeMainThreadDispatcher()));
    }

    [TestMethod]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SlideStreamRenderService(pipeline, null!));
    }
}
