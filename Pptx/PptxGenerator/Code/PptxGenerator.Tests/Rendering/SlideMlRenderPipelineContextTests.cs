using System.Threading;
using System.Threading.Tasks;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// SlideMlRenderPipeline 上下文重置与 CancellationToken 集成测试。
/// 对应测试用例文档第 4 章（上下文重置）和第 8 章（CancellationToken）。
/// </summary>
[TestClass]
public sealed class SlideMlRenderPipelineContextTests
{
    /// <summary>
    /// 构建测试用管道实例。
    /// </summary>
    private static (SlideMlRenderPipeline Pipeline, FakeRenderEngine RenderEngine, SlideMlPipelineContext Context) CreatePipeline(
        SlideMlPipelineContext? context = null)
    {
        context ??= new SlideMlPipelineContext();
        var layoutEngine = new SlideMlLayoutEngine();
        var renderEngine = new FakeRenderEngine();
        var dispatcher = new FakeMainThreadDispatcher();
        var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher, context);
        return (pipeline, renderEngine, context);
    }

    // ───────── 第 4 章：上下文重置 ─────────

    [TestMethod]
    public async Task RenderAsync_MultipleCalls_ContextReset()
    {
        var (pipeline, _, _) = CreatePipeline();

        // 第一次调用：元素超出画布，产生 Warning
        var xml1 = """<Page><Rect Id="r1" X="1200" Y="600" Width="200" Height="200"/></Page>""";
        var result1 = await pipeline.RenderAsync(xml1).ConfigureAwait(false);
        Assert.IsNotEmpty(result1.Warnings, "第一次调用应产生 Warning");

        // 第二次调用：正常 XML，无 Warning
        var xml2 = """<Page><Rect Id="r2" X="0" Y="0" Width="100" Height="50"/></Page>""";
        var result2 = await pipeline.RenderAsync(xml2).ConfigureAwait(false);

        Assert.IsEmpty(result2.Warnings, "第二次调用不应包含第一次的 Warning");
    }

    [TestMethod]
    public async Task RenderAsync_ConsecutiveCalls_Independent()
    {
        var (pipeline, _, _) = CreatePipeline();

        var xml1 = """<Page><Rect Id="r1" X="10" Y="10" Width="100" Height="50"/></Page>""";
        var result1 = await pipeline.RenderAsync(xml1).ConfigureAwait(false);

        var xml2 = """<Page><Rect Id="r2" X="20" Y="20" Width="200" Height="100"/></Page>""";
        var result2 = await pipeline.RenderAsync(xml2).ConfigureAwait(false);

        // 两次结果各自独立
        StringAssert.Contains(result1.OutputXml, "Id=\"r1\"", "第一次结果应包含 r1");
        StringAssert.Contains(result2.OutputXml, "Id=\"r2\"", "第二次结果应包含 r2");
        Assert.DoesNotContain("Id=\"r1\"", result2.OutputXml, "第二次结果不应包含 r1");
    }

    // ───────── 第 8 章：CancellationToken ─────────

    [TestMethod]
    public async Task RenderAsync_Cancelled_CancellationPropagated()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, _, _) = CreatePipeline();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await pipeline.RenderAsync(xml, cts.Token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RenderAsync_NotCancelled_NormalExecution()
    {
        var xml = """<Page><Rect Id="r1" Width="100" Height="50"/></Page>""";
        var (pipeline, _, _) = CreatePipeline();

        var result = await pipeline.RenderAsync(xml, CancellationToken.None).ConfigureAwait(false);

        Assert.IsNotNull(result, "应正常返回结果");
        Assert.IsEmpty(result.Warnings, "不应有 Warning");
        Assert.IsEmpty(result.Errors, "不应有 Error");
    }
}
