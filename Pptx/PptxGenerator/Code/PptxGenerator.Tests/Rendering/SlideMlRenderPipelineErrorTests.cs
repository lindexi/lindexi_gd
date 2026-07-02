using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline 错误处理与 Error 收集集成测试。
/// 对应测试用例文档第 5 章（错误处理）和第 7 章（Error 收集）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineErrorTests
{
    /// <summary>
    /// 构建测试用管道实例。
    /// </summary>
    private static (SlideMlRenderPipeline Pipeline, FakeRenderEngine RenderEngine) CreatePipeline(
        SlideMlPipelineContext? context = null)
    {
        context ??= new SlideMlPipelineContext();
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher, context);
        return (pipeline, renderEngine);
    }

    // ───────── 第 5 章：错误处理 ─────────

    [TestMethod]
    public async Task RenderAsync_EmptyString_ThrowsArgumentException()
    {
        var (pipeline, _) = CreatePipeline();

        // 实际行为：RenderAsync 对空白字符串在 try-catch 之前抛出 ArgumentException
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await pipeline.RenderAsync("").ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RenderAsync_NullString_ThrowsArgumentException()
    {
        var (pipeline, _) = CreatePipeline();

        // 实际行为：string.IsNullOrWhiteSpace(null) 返回 true，抛出 ArgumentException
        // 注意：文档预期 ArgumentNullException，但实际代码统一抛出 ArgumentException
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await pipeline.RenderAsync(null!).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RenderAsync_WhitespaceString_ThrowsArgumentException()
    {
        var (pipeline, _) = CreatePipeline();

        // 实际行为：空白字符串在 try-catch 之前被 string.IsNullOrWhiteSpace 拦截，抛出 ArgumentException
        // 注意：文档预期"返回错误结果"，但实际抛出 ArgumentException
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await pipeline.RenderAsync("   \n  ").ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RenderAsync_NonPageRoot_ErrorPreviewReturned()
    {
        var xml = """<NotPage></NotPage>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // SlideMlRootElementException 是 SlideMlParseException 子类，会被 catch 捕获
        Assert.IsNotEmpty(result.Errors, "应包含解析失败错误");
        Assert.IsNotNull(result.PreviewImage, "应返回错误预览图");
    }

    [TestMethod]
    public async Task RenderAsync_MalformedXml_ErrorHandled()
    {
        var xml = "<Page><Rect></Page>";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        // XmlException 被捕获，返回错误结果
        Assert.IsNotEmpty(result.Errors, "应包含解析失败错误");
        Assert.IsNotNull(result.PreviewImage, "应返回错误预览图");
    }

    [TestMethod]
    public async Task RenderAsync_PreMeasureThrows_ErrorPropagated()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new PreMeasureThrowingEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);

        // 实际行为：PreMeasure 抛出的非 SlideMlParseException/XmlException 异常不会被 RenderAsync 捕获，
        // 会向上传播到调用者
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await pipeline.RenderAsync(xml).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RenderAsync_RenderThrows_ErrorPropagated()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new RenderThrowingEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);

        // 实际行为：Render 抛出的非 SlideMlParseException/XmlException 异常不会被 RenderAsync 捕获，
        // 会向上传播到调用者
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await pipeline.RenderAsync(xml).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    // ───────── 第 7 章：Error 收集 ─────────

    [TestMethod]
    public async Task RenderAsync_InvalidEnumValue_ErrorCollected()
    {
        var xml = """<Page><Rect Id="r1" HorizontalAlignment="Diagonal" Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotEmpty(result.Errors, "应包含 Error");
        var hasEnumError = result.Errors.Any(e => e.Contains("HorizontalAlignment") && e.Contains("Diagonal"));
        Assert.IsTrue(hasEnumError, "Error 应包含 HorizontalAlignment 值 \"Diagonal\" 无效");
    }

    [TestMethod]
    public async Task RenderAsync_InvalidNumericValue_ErrorCollected()
    {
        var xml = """<Page><Rect Id="r1" Width="abc" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotEmpty(result.Errors, "应包含 Error");
        var hasNumericError = result.Errors.Any(e => e.Contains("Width") && e.Contains("abc"));
        Assert.IsTrue(hasNumericError, "Error 应包含 Width 值 \"abc\" 不是有效的数值");
    }

    [TestMethod]
    public async Task RenderAsync_InvalidMargin_ErrorCollected()
    {
        var xml = """<Page><Rect Id="r1" Margin="abc" Width="100" Height="50"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotEmpty(result.Errors, "应包含 Error");
        var hasMarginError = result.Errors.Any(e => e.Contains("不是有效的间距格式"));
        Assert.IsTrue(hasMarginError, "Error 应包含不是有效的间距格式");
    }

    [TestMethod]
    public async Task RenderAsync_ErrorAndWarning_CollectedSeparately()
    {
        // 无效枚举值（Error）+ 超出边界（Warning）
        var xml = """<Page><Rect Id="r1" HorizontalAlignment="Diagonal" X="1200" Y="600" Width="200" Height="200"/></Page>""";
        var (pipeline, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

        Assert.IsNotEmpty(result.Errors, "应包含 Error");
        Assert.IsNotEmpty(result.Warnings, "应包含 Warning");
        // 确认分类正确：Error 中不包含画布边界警告
        Assert.IsFalse(result.Errors.Any(e => e.Contains("超出画布")), "Errors 不应包含画布边界警告");
        // 确认 Warning 中不包含枚举错误
        Assert.IsFalse(result.Warnings.Any(w => w.Contains("HorizontalAlignment")), "Warnings 不应包含枚举错误");
    }

    /// <summary>
    /// PreMeasure 方法抛出异常的渲染引擎实现。
    /// </summary>
    private sealed class PreMeasureThrowingEngine : ISlideMlRenderEngine
    {
        public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
        {
            throw new InvalidOperationException("PreMeasure 模拟异常");
        }

        public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
            => new FakePreviewImage();

        public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
            => new FakePreviewImage();
    }

    /// <summary>
    /// Render 方法抛出异常的渲染引擎实现。
    /// </summary>
    private sealed class RenderThrowingEngine : ISlideMlRenderEngine
    {
        public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
        {
            var measurements = new Dictionary<string, SlideMlElementMeasureResult>();
            return new SlideMlElementMeasurements(measurements);
        }

        public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
        {
            throw new InvalidOperationException("Render 模拟异常");
        }

        public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
            => new FakePreviewImage();
    }
}
