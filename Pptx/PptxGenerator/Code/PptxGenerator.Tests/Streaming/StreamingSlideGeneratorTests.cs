using System.Xml.Linq;

using Microsoft.Extensions.AI;

using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Streaming;
using PptxGenerator.Tests.Rendering;

namespace PptxGenerator.Tests.Streaming;

/// <summary>
/// <see cref="StreamingSlideGenerator.GenerateAsync"/> 上层流式生成入口的单元测试。
/// </summary>
[TestClass]
public sealed class StreamingSlideGeneratorTests
{
    /// <summary>
    /// 验证模型输出中先给出 Page 骨架，随后输出错误闭合标签片段时，
    /// 第一轮应检测错误并触发重试，第二轮继续合并修正后的内容。
    /// </summary>
    [TestMethod(DisplayName = "GenerateAsync 上层链路：MainContainer 错误闭合片段触发重试后继续合并")]
    public async Task GenerateAsync_MainContainerFragmentClosedByIdName_ThenRetryCanContinueMerge()
    {
        // Arrange
        var output =
            """
            我先分析页面结构，然后从骨架开始逐步构建。

            <Page Background="#B2FAFAFA">
              <Image Id="BgImage" X="0" Y="0" Width="1280" Height="720" Source="img_1" Opacity="0.40"/>
              <Panel Id="MainContainer" X="20" Y="20" Width="1240" Height="680">
              </Panel>
            </Page>

            <Panel Id="MainContainer">
              <Rect Id="OverlayShape" X="0" Y="0" Width="1240" Height="680" Fill="#FFFFFF" Opacity="0.55" CornerRadius="16"/>
              <Image Id="TopRightIcon" X="1055" Y="27" Width="134" Height="43" Source="img_2" Opacity="0.85"/>
              <Panel Id="LeftContent" X="60" Y="80" Width="620" Height="560">
              </Panel>
              <Image Id="RightImage" X="730" Y="110" Width="470" Height="480" Source="img_3" Stretch="Uniform"/>
              <Panel Id="TaskLabelGroup" X="930" Y="30" Width="280" Height="56">
                <Rect Id="TaskLabelBg" X="0" Y="0" Width="280" Height="56" Fill="#E8F4FD" CornerRadius="28"/>
                <TextElement Id="TaskLabelText" X="20" Y="10" Width="240" Height="36" Text="学习任务" FontSize="22" IsBold="True" Foreground="#2C7BE5" TextAlignment="Center"/>
              </Panel>
            </MainContainer>

            现在让我细化左侧内容区域。文本.1是标题，文本.2和文本.3是正文问题。

            <Panel Id="LeftContent">
              <TextElement Id="TitleText" X="0" Y="0" Width="620" Text="交流如何进行场面描写" FontSize="40" IsBold="True" Foreground="#1A365D" FontName="宋体"/>
              <TextElement Id="QuestionOne" X="0" Y="90" Width="620" FontSize="20" Foreground="#2D3748" FontName="宋体" Text="回顾课文，说说文中在描写场面时，&#xA;是怎样点面结合来写的。"/>
              <TextElement Id="QuestionTwo" X="0" Y="210" Width="620" FontSize="20" Foreground="#2D3748" FontName="宋体" Text="结合自己的习作，说说自己是如何&#xA;运用点面结合的方法来写的。"/>
            </Panel>

            先看看目前的渲染效果。
            """;

        var correctedOutput =
            """
            <Panel Id="MainContainer">
              <Rect Id="OverlayShape" X="0" Y="0" Width="1240" Height="680" Fill="#FFFFFF" Opacity="0.55" CornerRadius="16"/>
              <Image Id="TopRightIcon" X="1055" Y="27" Width="134" Height="43" Source="img_2" Opacity="0.85"/>
              <Panel Id="LeftContent" X="60" Y="80" Width="620" Height="560">
              </Panel>
              <Image Id="RightImage" X="730" Y="110" Width="470" Height="480" Source="img_3" Stretch="Uniform"/>
              <Panel Id="TaskLabelGroup" X="930" Y="30" Width="280" Height="56">
                <Rect Id="TaskLabelBg" X="0" Y="0" Width="280" Height="56" Fill="#E8F4FD" CornerRadius="28"/>
                <TextElement Id="TaskLabelText" X="20" Y="10" Width="240" Height="36" Text="学习任务" FontSize="22" IsBold="True" Foreground="#2C7BE5" TextAlignment="Center"/>
              </Panel>
            </Panel>

            <Panel Id="LeftContent">
              <TextElement Id="TitleText" X="0" Y="0" Width="620" Text="交流如何进行场面描写" FontSize="40" IsBold="True" Foreground="#1A365D" FontName="宋体"/>
              <TextElement Id="QuestionOne" X="0" Y="90" Width="620" FontSize="20" Foreground="#2D3748" FontName="宋体" Text="回顾课文，说说文中在描写场面时，&#xA;是怎样点面结合来写的。"/>
              <TextElement Id="QuestionTwo" X="0" Y="210" Width="620" FontSize="20" Foreground="#2D3748" FontName="宋体" Text="结合自己的习作，说说自己是如何&#xA;运用点面结合的方法来写的。"/>
            </Panel>
            """;

        var (chatManager, _, recorder) =
            SlideStreamingTestHelper.CreateChatManagerWithSequentialTextsAndRecorder(
                output, correctedOutput);

        var promptProvider = new SlideMlPromptProvider();
        var dispatcher = new FakeMainThreadDispatcher();
        var streamingState = new SlideStreamingState(promptProvider,
            chatManager.SlideMlRenderTool.RenderPipeline,
            dispatcher);

        var generator = new StreamingSlideGenerator
        (
            chatManager.Pipeline.ChatManager,
            promptProvider,
            chatManager.SlideMlRenderTool,
            dispatcher
        );

        // Act
        await generator.GenerateAsync
        (
            "生成学习任务页面",
            isFirstMessage: true,
            streamingState,
            CancellationToken.None
        );

        // Assert
        Assert.AreEqual(2, recorder.StreamingCallCount, "错误闭合标签片段应触发一次重试");
        Assert.AreEqual(0, streamingState.Context.Errors.Count, "重试修正后不应留下错误");
        Assert.IsTrue(recorder.StreamingMessages.Count >= 2, "应能捕获重试反馈请求");

        var retryText = string.Join("\n", recorder.StreamingMessages[1].Select(message => message.Text));
        StringAssert.Contains(retryText, "MainContainer", "重试反馈应包含已合并的页面骨架，便于模型基于当前状态续写");
        StringAssert.Contains(retryText, "仅输出修正和后续片段", "重试反馈应要求模型只输出修正和后续片段");

        var doc = XDocument.Parse(streamingState.Pipeline.CurrentMergedXml);
        var mainContainer = doc.Root!.Elements("Panel").Single(element => element.Attribute("Id")?.Value == "MainContainer");
        var leftContent = mainContainer.Elements("Panel").Single(element => element.Attribute("Id")?.Value == "LeftContent");

        Assert.AreEqual("#B2FAFAFA", doc.Root.Attribute("Background")?.Value, "首次成功 Page 骨架应保留");
        Assert.IsNotNull(mainContainer.Elements("Rect").SingleOrDefault(element => element.Attribute("Id")?.Value == "OverlayShape"),
            "修正后的 MainContainer 内容应成功合并");
        Assert.IsNotNull(leftContent.Elements("TextElement").SingleOrDefault(element => element.Attribute("Id")?.Value == "TitleText"),
            "后续 LeftContent 文本内容应成功合并");
        Assert.IsNotNull(leftContent.Elements("TextElement").SingleOrDefault(element => element.Attribute("Id")?.Value == "QuestionOne"),
            "第一条问题文本应成功合并");
        Assert.IsNotNull(leftContent.Elements("TextElement").SingleOrDefault(element => element.Attribute("Id")?.Value == "QuestionTwo"),
            "第二条问题文本应成功合并");

        StringAssert.Contains(chatManager.RenderedXml, "TitleText", "上层渲染工具应接收到最终合并结果");
        StringAssert.Contains(chatManager.RenderedXml, "交流如何进行场面描写", "最终渲染结果应包含标题文本");
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
        var streamingState = new SlideStreamingState(promptProvider,
            chatManager.SlideMlRenderTool.RenderPipeline,
            dispatcher);

        var generator = new StreamingSlideGenerator
        (
            chatManager.Pipeline.ChatManager,
            promptProvider,
            chatManager.SlideMlRenderTool,
            dispatcher
        );

        var originalCallback = fakeChatClient.OnGetStreamingResponseAsync!;
        fakeChatClient.OnGetStreamingResponseAsync = (messages, options, cancellationToken) =>
        {
            var list = messages.ToList();
            capturedMessages.Add(list);
            return originalCallback(list, options, cancellationToken);
        };

        // Act
        await generator.GenerateAsync
        (
            "生成可复用卡片样式页面",
            isFirstMessage: true,
            streamingState,
            CancellationToken.None
        );

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
}
