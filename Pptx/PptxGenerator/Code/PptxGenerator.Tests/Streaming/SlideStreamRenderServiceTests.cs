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

    private static SlideStreamRenderService CreateService(FakeRenderPipeline pipeline)
    {
        return new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher());
    }

    [TestMethod(DisplayName = "FinalRender：调用后立即渲染并更新状态")]
    public async Task FinalRenderAsync_RendersImmediatelyAndUpdatesState()
    {
        // Arrange
        const string outputXml = """<Page RenderSize="1280x720"/>""";
        var pipeline = CreatePipeline(outputXml: outputXml);
        var service = CreateService(pipeline);

        // Act
        var result = await service.FinalRenderAsync("<Page/>");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(outputXml, service.CurrentRenderedXml);
        Assert.IsNotNull(service.CurrentPreviewImage);
        Assert.IsEmpty(service.CurrentWarnings);
    }

    [TestMethod(DisplayName = "FinalRender：空 XML 不渲染")]
    public async Task FinalRenderAsync_EmptyXml_DoesNotRender()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = CreateService(pipeline);
        var renderedCount = 0;
        service.Rendered += _ => renderedCount++;

        // Act
        await service.FinalRenderAsync("");

        // Assert
        Assert.AreEqual(0, renderedCount);
    }

    [TestMethod(DisplayName = "渲染成功后触发 Rendered 事件")]
    public async Task FinalRenderAsync_ValidXml_TriggersRenderedEvent()
    {
        // Arrange
        const string outputXml = """<Page RenderSize="1280"/>""";
        var previewImage = new FakePreviewImage();
        var pipeline = CreatePipeline(outputXml: outputXml, previewImage: previewImage);
        var service = CreateService(pipeline);

        SlideStreamRenderResult? eventResult = null;
        service.Rendered += result => eventResult = result;

        // Act
        await service.FinalRenderAsync("<Page/>");

        // Assert
        Assert.IsNotNull(eventResult, "Rendered 事件应被触发");
        Assert.AreEqual(outputXml, eventResult!.OutputXml);
        Assert.AreSame(previewImage, eventResult.PreviewImage);
    }

    [TestMethod(DisplayName = "FinalRender：带警告时正确存储")]
    public async Task FinalRenderAsync_WithWarnings_StoresWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        var pipeline = CreatePipeline(warnings: warnings);
        var service = CreateService(pipeline);

        // Act
        await service.FinalRenderAsync("<Page/>");

        // Assert
        Assert.HasCount(2, service.CurrentWarnings);
        Assert.AreEqual("Warning 1", service.CurrentWarnings[0]);
        Assert.AreEqual("Warning 2", service.CurrentWarnings[1]);
    }

    [TestMethod(DisplayName = "渲染异常正确传播")]
    public async Task FinalRenderAsync_RenderThrows_ExceptionPropagated()
    {
        // Arrange
        var pipeline = new FakeRenderPipeline(new InvalidOperationException("render failed"));
        var service = CreateService(pipeline);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await service.FinalRenderAsync("<Page/>"));
    }

    [TestMethod(DisplayName = "构造函数：null renderPipeline 抛出 ArgumentNullException")]
    public void Constructor_NullRenderPipeline_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SlideStreamRenderService(null!, new FakeMainThreadDispatcher()));
    }

    [TestMethod(DisplayName = "构造函数：null dispatcher 抛出 ArgumentNullException")]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = CreatePipeline();

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new SlideStreamRenderService(pipeline, null!));
    }
}
