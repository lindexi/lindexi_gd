using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 测试用渲染管道 Fake 实现。
/// </summary>
public sealed class FakeRenderPipeline : ISlideMlRenderPipeline
{
    private readonly SlideMlRenderResult? _result;
    private readonly Exception? _exception;

    /// <summary>
    /// 最近一次调用传入的 CancellationToken。
    /// </summary>
    public CancellationToken LastCancellationToken { get; private set; }

    /// <summary>
    /// 最近一次调用传入的 XML。
    /// </summary>
    public string LastInputXml { get; private set; } = string.Empty;

    /// <summary>
    /// RenderAsync 调用次数。
    /// </summary>
    public int RenderCallCount { get; private set; }

    public FakeRenderPipeline(SlideMlRenderResult result)
    {
        _result = result;
    }

    public FakeRenderPipeline(Exception exception)
    {
        _exception = exception;
    }

    /// <inheritdoc />
    public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        LastInputXml = slideXml;
        LastCancellationToken = cancellationToken;
        RenderCallCount++;

        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_result!);
    }
}
