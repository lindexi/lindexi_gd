using Microsoft.Extensions.AI;
using AgentLib;
using AgentLib.Core;
using System.Xml.Linq;

using PptxGenerator;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// 流式生成状态延续功能（方案 C）的单元测试。
/// 验证 SlideStreamingPipeline.ResetExtractor 保留合并器状态、
/// 重试和跨轮对话复用 Pipeline/Context、以及 BuildErrorFeedback 措辞。
/// </summary>
[TestClass]
public sealed class StreamingStateContinuationTests
{
    // ───────── Section 1：ResetExtractor 单元测试 ─────────

    /// <summary>
    /// 先 ProcessIncrementalTextAsync 合并片段，再 ResetExtractor，
    /// 验证 CurrentMergedXml 仍然保留之前的合并结果。
    /// </summary>
    [TestMethod(DisplayName = "ResetExtractor 保留合并器 DOM 状态")]
    public async Task ResetExtractor_PreservesMergerState()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        await pipeline.ProcessIncrementalTextAsync(
            """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page>""",
            context);

        var mergedBeforeReset = pipeline.CurrentMergedXml;
        Assert.IsFalse(string.IsNullOrEmpty(mergedBeforeReset), "合并后应有 XML 输出");

        // Act
        pipeline.ResetExtractor();

        // Assert
        Assert.AreEqual(mergedBeforeReset, pipeline.CurrentMergedXml,
            "ResetExtractor 不应影响合并器 DOM 状态");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "r1 元素应仍然保留在合并后的 XML 中");
    }

    /// <summary>
    /// 先 ProcessIncrementalTextAsync 传入不完整片段（残留于提取器缓冲区），
    /// 再 ResetExtractor，验证后续追加文本不会提取到之前残留的内容。
    /// </summary>
    [TestMethod(DisplayName = "ResetExtractor 清空提取器缓冲区残留")]
    public async Task ResetExtractor_ClearsExtractorBuffer()
    {
        // Arrange
        var pipeline = CreatePipeline();
        var context = new SlideMlPipelineContext();
        var fragments = new List<string>();
        pipeline.FragmentReceived += fragments.Add;

        // 传入不完整片段：开始标签未闭合，提取器缓冲区会残留
        await pipeline.ProcessIncrementalTextAsync("<Page><Rect Id=\"r1", context);
        Assert.IsEmpty(fragments, "不完整片段不应触发 FragmentReceived");

        // Act — 重置提取器
        pipeline.ResetExtractor();

        // 追加之前不完整片段的后续部分，如果缓冲区未清空则可能拼接成完整片段
        await pipeline.ProcessIncrementalTextAsync(
            """ Width="100" Height="50" Fill="#FF0000"/></Page>""", context);

        // Assert — 重置后残留内容已清空，后续追加的文本自身不构成完整片段
        Assert.IsEmpty(fragments,
            "ResetExtractor 后残留内容已清空，后续追加不应提取到之前的残留片段");
    }

    // ───────── Section 2：状态延续集成测试（直接使用 SlideStreamingState）─────────

    /// <summary>
    /// 模拟第一轮出错重试场景。第一段文本包含成功片段（r1）和错误片段（UnknownElement），
    /// 第二段输出修正片段（r2）。通过 SlideStreamingState 直接模拟
    /// StreamingSlideGenerator.GenerateAsync 的重试流程：
    /// Context.Reset() + Pipeline.ResetExtractor()，然后处理第二轮文本。
    /// 验证最终合并 XML 同时包含 r1 和 r2，说明重试时合并器 DOM 被保留。
    /// </summary>
    [TestMethod(DisplayName = "重试保留 Pipeline 状态：第一轮成功片段在重试后仍然保留")]
    public async Task RetryPreservesPipelineState()
    {
        // Arrange — 使用 SlideStreamingState 模拟重试流程
        var (state, renderPipeline) = CreateStreamingState();

        var firstRoundText =
            """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page><UnknownElement Id="x"/>""";
        var secondRoundText =
            """<Page><Rect Id="r2" Width="200" Height="100" Fill="#00FF00"/></Page>""";

        var pipeline = state.Pipeline;
        var context = state.Context;

        // Act — 第一轮：处理包含成功片段和错误片段的文本
        await pipeline.ProcessIncrementalTextAsync(firstRoundText, context);

        // Assert — 第一轮应有错误（UnknownElement），且 r1 已成功合并
        Assert.IsTrue(context.Errors.Count > 0, "第一轮应有错误（UnknownElement）");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "第一轮应成功合并 r1 元素");

        // Act — 模拟重试前重置：Context.Reset 清空诊断信息，Pipeline.ResetExtractor 清空提取器
        context.Reset();
        pipeline.ResetExtractor();

        // Assert — 重置后合并器状态保留
        Assert.AreEqual(0, context.Errors.Count, "重置后错误应清空");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "ResetExtractor 后 r1 应保留在合并器 DOM 中");

        // Act — 第二轮：处理修正片段
        await pipeline.ProcessIncrementalTextAsync(secondRoundText, context);

        // Assert — 第二轮无错误，且 r1 和 r2 都在合并结果中
        Assert.AreEqual(0, context.Errors.Count, "第二轮不应有错误");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "重试后应保留第一轮成功合并的 r1 元素");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r2",
            "重试后应包含第二轮修正的 r2 元素");

        // Act — 流结束渲染
        await pipeline.ProcessStreamEndAsync(context);

        // Assert — 最终渲染输入包含 r1 和 r2
        Assert.IsTrue(renderPipeline.RenderCallCount > 0, "应有渲染调用");
        StringAssert.Contains(renderPipeline.LastInputXml, "r1",
            "最终渲染输入应包含 r1");
        StringAssert.Contains(renderPipeline.LastInputXml, "r2",
            "最终渲染输入应包含 r2");
    }

    /// <summary>
    /// 模拟跨轮对话。第一轮发送"生成页面"得到完整 XML（含 r1），
    /// 第二轮发送"修改"得到增量片段（r2）。通过 SlideStreamingState 直接模拟
    /// SlideGenerationPipeline 跨轮复用 _streamingState 的行为：
    /// 两轮之间不重置状态（isFirstMessage=false 时 _streamingState 保留）。
    /// 验证第二轮的修改在已有状态上增量合并，而非从零开始。
    /// </summary>
    [TestMethod(DisplayName = "跨轮对话从上一轮状态增量合并")]
    public async Task CrossTurnContinuesFromPreviousState()
    {
        // Arrange
        var (state, renderPipeline) = CreateStreamingState();

        var firstTurnXml =
            """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page>""";
        var secondTurnXml =
            """<Page><Rect Id="r2" Width="200" Height="100" Fill="#00FF00"/></Page>""";

        var pipeline = state.Pipeline;
        var context = state.Context;

        // Act — 第一轮
        await pipeline.ProcessIncrementalTextAsync(firstTurnXml, context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert — 第一轮应渲染 r1
        Assert.AreEqual(0, context.Errors.Count, "第一轮不应有错误");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "第一轮应合并 r1 元素");
        StringAssert.Contains(renderPipeline.LastInputXml, "r1",
            "第一轮渲染应包含 r1");

        // Act — 第二轮（跨轮复用同一 SlideStreamingState，不重置）
        // 模拟 isFirstMessage=false 时的行为：_streamingState 保留
        context.Reset();
        pipeline.ResetExtractor();

        await pipeline.ProcessIncrementalTextAsync(secondTurnXml, context);
        await pipeline.ProcessStreamEndAsync(context);

        // Assert — 第二轮应在第一轮基础上增量合并 r2
        Assert.AreEqual(0, context.Errors.Count, "第二轮不应有错误");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r1",
            "跨轮后 r1 应仍然保留");
        StringAssert.Contains(pipeline.CurrentMergedXml, "r2",
            "第二轮增量合并的 r2 应出现在合并结果中");
        StringAssert.Contains(renderPipeline.LastInputXml, "r1",
            "跨轮后渲染输入应包含 r1");
        StringAssert.Contains(renderPipeline.LastInputXml, "r2",
            "跨轮后渲染输入应包含 r2");
    }

    // ───────── Section 3：BuildErrorFeedback 措辞测试 ─────────

    /// <summary>
    /// 通过集成测试间接验证 BuildErrorFeedback 措辞。
    /// 第一轮输出包含未知元素的 XML，触发错误重试；
    /// 第二轮 FakeChatClient 收到的消息应包含"仅输出修正和后续片段"文本。
    /// </summary>
    [TestMethod(DisplayName = "错误反馈消息包含‘仅输出修正和后续片段’措辞")]
    public async Task ErrorFeedback_ContainsCorrectWording()
    {
        // Arrange — 第一轮包含错误片段（UnknownElement 触发 AddError → 重试）
        var firstRoundText =
            """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/></Page><UnknownElement Id="bad"/>""";
        var secondRoundText =
            """<Page><Rect Id="r1" Width="100" Height="50" Fill="#FF0000"/><Rect Id="r2" Width="50" Height="30" Fill="#00FF00"/></Page>""";

        var capturedMessages = new List<IEnumerable<ChatMessage>>();

        var (chatManager, fakeChatClient) =
            SlideStreamingTestHelper.CreateChatManagerWithSequentialTexts(
                firstRoundText, secondRoundText);

        // 拦截第二次及后续调用（重试反馈）的消息内容
        var originalCallback = fakeChatClient.OnGetStreamingResponseAsync!;
        var callIndex = 0;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, ct) =>
        {
            if (callIndex > 0)
            {
                capturedMessages.Add(messages);
            }

            callIndex++;
            return originalCallback(messages, options, ct);
        };

        // Act
        await chatManager.SendMessageAsync(
            "生成页面",
            isFirstMessage: true,
            attachPreview: false,
            useStreaming: true).ConfigureAwait(false);

        // Assert — 应捕获到至少一次重试消息
        Assert.IsTrue(capturedMessages.Count > 0,
            "应触发至少一次重试，捕获到反馈消息");

        // 将所有捕获消息的文本拼接，检查是否包含目标措辞
        var allText = string.Join("\n", capturedMessages
            .SelectMany(msgs => msgs)
            .Select(m => m.Text));

        StringAssert.Contains(allText, "仅输出修正和后续片段",
            "错误反馈消息应包含‘仅输出修正和后续片段’措辞");
    }

    /// <summary>
    /// 通过 <see cref="StreamingSlideGenerator.GenerateAsync"/> 上层入口验证首片段为悬空样式元素时，
    /// 第一轮实时渲染失败会触发重试，第二轮 Page 仍可引用第一轮暂存的样式。
    /// </summary>
    [TestMethod(DisplayName = "GenerateAsync 上层链路：首片段悬空样式触发重试后 Page 可引用样式")]
    public async Task GenerateAsync_FirstFragmentDanglingStyleElement_ThenPageCanReferenceStyle()
    {
        // Arrange
        var danglingStyleFragment =
            """<Rect Id="card-template" StyleId="card-style" Fill="#FF0000" Width="100" Height="50"/>""";
        var pageFragment =
            """<Page><Rect Id="card1" StyleFrom="card-style" X="10" Y="20"/></Page>""";

        var capturedMessages = new List<IReadOnlyList<ChatMessage>>();
        var (chatManager, fakeChatClient, recorder) =
            SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
                danglingStyleFragment, pageFragment);
        var promptProvider = new SlideMlPromptProvider();
        var dispatcher = new FakeMainThreadDispatcher();
        var streamingState = new SlideStreamingState(
            promptProvider,
            chatManager.SlideMlRenderTool.RenderPipeline,
            dispatcher);
        var generator = new StreamingSlideGenerator(
            chatManager.Pipeline.ChatManager,
            promptProvider,
            chatManager.SlideMlRenderTool,
            dispatcher);

        var originalCallback = fakeChatClient.OnGetStreamingResponseAsync!;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            capturedMessages.Add(messages.ToList());
            return originalCallback(messages, options, cancellationToken);
        };

        // Act
        await generator.GenerateAsync(
            "生成可复用卡片样式页面",
            isFirstMessage: true,
            streamingState,
            CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(2, recorder.StreamingCallCount, "首个悬空样式片段不可渲染，应触发一次重试");
        Assert.IsTrue(capturedMessages.Count >= 2, "应能捕获首次请求和重试反馈请求");
        var retryText = string.Join("\n", capturedMessages[1].Select(message => message.Text));
        StringAssert.Contains(retryText, "card-template", "重试反馈应包含第一轮已合并的悬空样式片段，便于模型基于当前状态续写");
        StringAssert.Contains(retryText, "仅输出修正和后续片段", "重试反馈应要求模型只输出后续 Page 片段");

        Assert.AreEqual(0, streamingState.Context.Errors.Count, "第二轮 Page 引用悬空样式时不应留下错误");
        var doc = XDocument.Parse(streamingState.Pipeline.CurrentMergedXml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "后续 Page 片段应成为最终可渲染根元素");
        Assert.IsFalse(doc.Root.Elements().Any(element => element.Attribute("Id")?.Value == "card-template"),
            "悬空样式元素不应进入 Page 子树参与渲染");

        var card1 = doc.Root.Elements("Rect").Single(element => element.Attribute("Id")?.Value == "card1");
        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "Page 内元素应通过 GenerateAsync 完整链路继承悬空样式 Fill");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "Page 内元素应继承悬空样式 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "Page 内元素应继承悬空样式 Height");
        Assert.AreEqual("10", card1.Attribute("X")?.Value, "自身显式属性优先保留");
        Assert.AreEqual("20", card1.Attribute("Y")?.Value, "自身显式属性优先保留");
        Assert.IsNull(card1.Attribute("StyleFrom"), "StyleFrom 应在应用后被移除");

        StringAssert.Contains(chatManager.RenderedXml, "card1", "上层渲染工具应接收到最终 Page 渲染结果");
        StringAssert.Contains(chatManager.RenderedXml, "#FF0000", "最终渲染结果应包含继承后的样式属性");
    }

    // ───────── 辅助方法 ─────────

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

    /// <summary>
    /// 创建 SlideStreamingState 和对应的 FakeRenderPipeline，
    /// 用于直接测试状态延续逻辑。
    /// </summary>
    private static (SlideStreamingState State, FakeRenderPipeline RenderPipeline) CreateStreamingState()
    {
        var renderResult = new SlideMlRenderResult
        {
            InputXml = "<Page/>",
            OutputXml = "<Page/>",
            Warnings = Array.Empty<string>(),
            Errors = Array.Empty<string>(),
            PreviewImage = new FakePreviewImage(),
        };
        var renderPipeline = new FakeRenderPipeline(renderResult);
        var dispatcher = new FakeMainThreadDispatcher();
        var promptProvider = new SlideMlPromptProvider();
        var state = new SlideStreamingState(promptProvider, renderPipeline, dispatcher);
        return (state, renderPipeline);
    }
}
