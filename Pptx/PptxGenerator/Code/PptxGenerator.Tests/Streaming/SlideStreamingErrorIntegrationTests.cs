using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式输出错误恢复与中断-纠错机制的集成测试。
/// </summary>
[TestClass]
public sealed class SlideStreamingErrorIntegrationTests
{
    private static SlideStreamingPipeline CreatePipeline()
    {
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var renderPipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher);
        var promptProvider = new SlideMlPromptProvider();
        return new SlideStreamingPipeline(promptProvider, renderPipeline, dispatcher);
    }

    // ───────── 错误恢复场景 ─────────

    [TestMethod]
    public async Task ErrorRecovery_MalformedXmlFragment_SkippedAndContinues()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act — 先输出有效片段
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>", context);

        // 输出格式错误的 XML 片段（XElement.Parse 会失败：标签嵌套错误）
        // Panel 标签内嵌不匹配的子标签，XElement.Parse 将抛出异常
        pipeline.ProcessIncrementalText("<Panel Id=\"bad\"><Rect Id=\"inner\"></Panel></Rect>", context);

        // 再输出有效片段（包裹在 Page 中以合并到 DOM）
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r2\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Page>", context);

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "应至少有 1 条错误");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1", "有效片段 r1 应保留");
        Assert.AreEqual(1, renderedCount, "应只渲染 1 次（流结束时）");
    }

    [TestMethod]
    public async Task ErrorRecovery_MissingIdElement_SkippedWithError()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 顶层 Rect 片段没有 Id
        pipeline.ProcessIncrementalText("<Page/>", context);
        pipeline.ProcessIncrementalText("<Rect Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "顶层片段缺少 Id 应产生错误");
    }

    [TestMethod]
    public async Task ErrorRecovery_DuplicateIdInSameFragment_SkipsEntireFragment()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 同一片段内 Panel 和 Rect 都叫 dup
        pipeline.ProcessIncrementalText("<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>", context);
        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "重复 Id 应产生错误");
    }

    [TestMethod]
    public async Task ErrorRecovery_IncompleteXmlStaysInBuffer_WarnedAtStreamEnd()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();

        // Act — 不完整的 XML 保留在缓冲区
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>", context);
        pipeline.ProcessIncrementalText("<Panel Id=\"incomplete\"", context);

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert — 缓冲区残留应产生 Warning
        Assert.IsTrue(context.Warnings.Count > 0, "缓冲区残留应产生警告");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1", "有效片段应保留");
    }

    // ───────── 中断-纠错机制场景 ─────────

    [TestMethod]
    public void Interruption_TolerableErrorsBelowThreshold_ContinuesStreaming()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);

        // Act
        controller.StartRound();
        var first = controller.ReportTolerableError("err1");
        var second = controller.ReportTolerableError("err2");

        // Assert
        Assert.IsFalse(first);
        Assert.IsFalse(second);
        Assert.IsFalse(controller.IsInterruptionRequested);
        Assert.IsFalse(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void Interruption_TolerableErrorsAtThreshold_TriggersCancellation()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);

        // Act
        controller.StartRound();
        controller.ReportTolerableError("err1");
        controller.ReportTolerableError("err2");
        var third = controller.ReportTolerableError("err3");

        // Assert
        Assert.IsTrue(third);
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void Interruption_FatalError_ImmediatelyCancels()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController();

        // Act
        controller.StartRound();
        controller.ReportFatalError("fatal");

        // Assert
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void Interruption_ResetErrorCount_PreventsThresholdTrigger()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);

        // Act
        controller.StartRound();
        controller.ReportTolerableError("err1");
        controller.ReportTolerableError("err2");
        controller.ResetErrorCount();

        var firstAfterReset = controller.ReportTolerableError("err1");
        var secondAfterReset = controller.ReportTolerableError("err2");

        // Assert
        Assert.IsFalse(firstAfterReset);
        Assert.IsFalse(secondAfterReset);
        Assert.IsFalse(controller.IsInterruptionRequested);
    }

    [TestMethod]
    public void Interruption_CanRetry_RetryRoundIncrements()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);

        // Act & Assert — 第 1 轮
        controller.StartRound();
        Assert.AreEqual(0, controller.RetryRound);
        Assert.IsTrue(controller.CanRetry());

        // 第 2 轮
        controller.StartRound();
        Assert.AreEqual(1, controller.RetryRound);
        Assert.IsTrue(controller.CanRetry());

        // 第 3 轮 — 达到最大重试
        controller.StartRound();
        Assert.AreEqual(2, controller.RetryRound);
        Assert.IsFalse(controller.CanRetry());
        Assert.IsTrue(controller.MaxRetriesReached);
    }

    // ───────── 端到端中断恢复场景 ─────────

    [TestMethod]
    public async Task EndToEnd_ErrorThenRecovery_ContinuesAndRenders()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 3, maxRetries: 2);
        var renderedCount = 0;
        pipeline.Rendered += _ => renderedCount++;

        // Act
        controller.StartRound();

        // 逐 token 输出有效片段
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>", context);

        // 模拟 2 个可容错错误（未达阈值 3）
        controller.ReportTolerableError("bad xml 1");
        controller.ReportTolerableError("bad xml 2");

        // 验证未触发中断
        Assert.IsFalse(controller.IsInterruptionRequested, "2 个错误不应触发中断");

        // 输出另一个有效片段（包裹在 Page 中以合并到 DOM）
        pipeline.ProcessIncrementalText("<Page><Rect Id=\"r2\" Width=\"50\" Height=\"30\" Fill=\"#00FF00\"/></Page>", context);

        // 成功片段重置错误计数
        controller.ResetErrorCount();

        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, renderedCount, "应渲染 1 次");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r2");
    }

    [TestMethod]
    public async Task EndToEnd_FatalErrorDuringStream_TriggersInterruption()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var controller = new SlideStreamInterruptionController();
        var streamCompleted = false;
        var streamCompletedXml = string.Empty;
        pipeline.StreamCompleted += xml =>
        {
            streamCompleted = true;
            streamCompletedXml = xml;
        };

        // Act
        controller.StartRound();
        pipeline.ProcessIncrementalText("<Page/>", context);

        // 模拟致命错误
        controller.ReportFatalError("fatal error");

        // Assert — 中断已触发
        Assert.IsTrue(controller.IsInterruptionRequested);
        Assert.IsTrue(controller.Token.IsCancellationRequested);

        // 即使中断也完成流处理
        await pipeline.ProcessStreamEndAsync(context).ConfigureAwait(false);

        Assert.IsTrue(streamCompleted, "StreamCompleted 应被触发");
        StringAssert.Contains(streamCompletedXml, "Page");
    }

    [TestMethod]
    public void EndToEnd_MaxRetriesReached_NoMoreRetries()
    {
        // Arrange
        var controller = new SlideStreamInterruptionController(maxConsecutiveErrors: 1, maxRetries: 2);

        // Act & Assert — 第 1 轮
        controller.StartRound();
        var interrupted1 = controller.ReportTolerableError("err");
        Assert.IsTrue(interrupted1, "1 >= 1 应触发中断");
        Assert.IsTrue(controller.CanRetry(), "第 1 次重试");

        // 第 2 轮
        controller.StartRound();
        var interrupted2 = controller.ReportTolerableError("err");
        Assert.IsTrue(interrupted2);
        Assert.IsTrue(controller.CanRetry(), "第 2 次重试");

        // 第 3 轮 — 达到最大重试
        controller.StartRound();
        var interrupted3 = controller.ReportTolerableError("err");
        Assert.IsTrue(interrupted3);
        Assert.IsFalse(controller.CanRetry(), "达到最大重试次数");
        Assert.IsTrue(controller.MaxRetriesReached);
    }

    [TestMethod]
    public async Task EndToEnd_RenderErrorInPipeline_ExceptionPropagated()
    {
        // Arrange — 使用 FakeRenderPipeline 抛出异常
        var fakePipeline = new FakeRenderPipeline(new InvalidOperationException("render failed"));
        var dispatcher = new FakeMainThreadDispatcher();
        var promptProvider = new SlideMlPromptProvider();
        var pipeline = new SlideStreamingPipeline(promptProvider, fakePipeline, dispatcher);
        var context = new SlideMlPipelineContext();

        // Act
        pipeline.ProcessIncrementalText("<Page/>", context);

        // Assert — 渲染时应抛出异常
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => pipeline.ProcessStreamEndAsync(context)).ConfigureAwait(false);
    }
}
