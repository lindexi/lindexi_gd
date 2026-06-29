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

    private static SlideStreamRenderService CreateService(
        FakeRenderPipeline pipeline,
        TimeSpan? minRenderInterval = null)
    {
        return new SlideStreamRenderService(pipeline, new FakeMainThreadDispatcher(), minRenderInterval);
    }

    [TestMethod(DisplayName = "TryRender：首次调用立即渲染并更新状态")]
    public async Task TryRenderAsync_FirstCall_RendersImmediately()
    {
        // Arrange
        const string outputXml = """<Page ActualWidth="1280" ActualHeight="720"/>""";
        var pipeline = CreatePipeline(outputXml: outputXml);
        var service = CreateService(pipeline);

        // Act
        var rendered = await service.TryRenderAsync("<Page/>");

        // Assert
        Assert.IsTrue(rendered);
        Assert.AreEqual(outputXml, service.CurrentRenderedXml);
        Assert.IsNotNull(service.CurrentPreviewImage);
        Assert.IsEmpty(service.CurrentWarnings);
    }

    [TestMethod(DisplayName = "TryRender：间隔内第二次调用被节流跳过")]
    public async Task TryRenderAsync_WithinInterval_SkipsRender()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = CreateService(pipeline, minRenderInterval: TimeSpan.FromSeconds(10));

        // Act：首次渲染成功
        var first = await service.TryRenderAsync("<Page/>");
        // 第二次应在节流期内被跳过
        var second = await service.TryRenderAsync("<Page/>");

        // Assert
        Assert.IsTrue(first);
        Assert.IsFalse(second);
    }

    [TestMethod(DisplayName = "TryRender：超过间隔后可以再次渲染")]
    public async Task TryRenderAsync_AfterInterval_Renders()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = CreateService(pipeline, minRenderInterval: TimeSpan.Zero);

        // Act
        var first = await service.TryRenderAsync("<Page/>");
        var second = await service.TryRenderAsync("<Page/>");

        // Assert
        Assert.IsTrue(first);
        Assert.IsTrue(second);
    }

    [TestMethod(DisplayName = "TryRender：空或空白 XML 不渲染")]
    public async Task TryRenderAsync_EmptyXml_ReturnsFalse()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = CreateService(pipeline);

        // Act
        var rendered = await service.TryRenderAsync("");

        // Assert
        Assert.IsFalse(rendered);
    }

    [TestMethod(DisplayName = "FinalRender：忽略节流直接渲染")]
    public async Task FinalRenderAsync_IgnoresThrottle()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var service = CreateService(pipeline, minRenderInterval: TimeSpan.FromSeconds(10));

        // 先触发一次渲染以设置上次渲染时间
        await service.TryRenderAsync("<Page/>");

        // Act：FinalRender 忽略节流
        await service.FinalRenderAsync("<Page/>");

        // Assert：不会抛异常，说明渲染成功
        Assert.IsNotNull(service.CurrentPreviewImage);
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
    public async Task TryRenderAsync_ValidXml_TriggersRenderedEvent()
    {
        // Arrange
        const string outputXml = """<Page ActualWidth="1280"/>""";
        var previewImage = new FakePreviewImage();
        var pipeline = CreatePipeline(outputXml: outputXml, previewImage: previewImage);
        var service = CreateService(pipeline);

        SlideStreamRenderResult? eventResult = null;
        service.Rendered += result => eventResult = result;

        // Act
        await service.TryRenderAsync("<Page/>");

        // Assert
        Assert.IsNotNull(eventResult, "Rendered 事件应被触发");
        Assert.AreEqual(outputXml, eventResult!.OutputXml);
        Assert.AreSame(previewImage, eventResult.PreviewImage);
    }

    [TestMethod(DisplayName = "TryRender：带警告时正确存储")]
    public async Task TryRenderAsync_WithWarnings_StoresWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        var pipeline = CreatePipeline(warnings: warnings);
        var service = CreateService(pipeline);

        // Act
        await service.TryRenderAsync("<Page/>");

        // Assert
        Assert.HasCount(2, service.CurrentWarnings);
        Assert.AreEqual("Warning 1", service.CurrentWarnings[0]);
        Assert.AreEqual("Warning 2", service.CurrentWarnings[1]);
    }

    [TestMethod(DisplayName = "渲染异常正确传播")]
    public async Task TryRenderAsync_RenderThrows_ExceptionPropagated()
    {
        // Arrange
        var pipeline = new FakeRenderPipeline(new InvalidOperationException("render failed"));
        var service = CreateService(pipeline);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await service.TryRenderAsync("<Page/>"));
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
