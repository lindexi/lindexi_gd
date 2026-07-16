using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using CoursewarePptxGeneratorWpfDemo.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using CoursewarePptxGeneratorWpfDemo.Views;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CoursewareAnalysisChatViewTests
{
    [TestMethod(DisplayName = "共享 Copilot 消息流应统一显示思考内容和工具调用")]
    [Timeout(60_000)]
    public void SharedMessageStreamShouldRenderReasoningAndToolCall()
    {
        RunOnStaThreadAsync(SharedMessageStreamShouldRenderReasoningAndToolCallAsync).GetAwaiter().GetResult();
    }

    [TestMethod(DisplayName = "主题分析会话应完整显示用户输入和 Copilot 输出")]
    [Timeout(60_000)]
    public void CopilotOutputShouldRenderAsVisibleTextInAnalysisView()
    {
        RunOnStaThreadAsync(CopilotOutputShouldRenderAsVisibleTextInAnalysisViewAsync).GetAwaiter().GetResult();
    }

    [TestMethod(DisplayName = "主题分析 Tab 应在分析时锁定对话并在完成后自动显示结果")]
    [Timeout(60_000)]
    public void AnalysisTabsShouldLockConversationAndSelectCompletedResult()
    {
        RunOnStaThreadAsync(AnalysisTabsShouldLockConversationAndSelectCompletedResultAsync).GetAwaiter().GetResult();
    }

    private static async Task SharedMessageStreamShouldRenderReasoningAndToolCallAsync()
    {
        const string reasoningText = "正在分析课件的内容层级。";
        const string toolInputText = "提交主题结构";
        const string toolOutputText = "主题结构校验通过";
        EnsureApplicationResources();
        var reasoningItem = new CopilotChatReasoningItem(reasoningText);
        var toolItem = new CopilotChatToolItem("submit-theme", "submit_courseware_theme", toolInputText, toolOutputText);
        var messageStream = new CopilotMessageStream();
        var templateSelector = messageStream.Resources["ChatMessageItemTemplateSelector"] as CopilotChatMessageItemTemplateSelector;
        Assert.IsNotNull(templateSelector, "共享控件应内聚消息项模板选择器。");
        var reasoningContent = CreateTemplateContent(templateSelector.SelectTemplate(reasoningItem, messageStream), reasoningItem);
        var toolContent = CreateTemplateContent(templateSelector.SelectTemplate(toolItem, messageStream), toolItem);
        var templateContent = new StackPanel();
        templateContent.Children.Add(reasoningContent);
        templateContent.Children.Add(toolContent);
        var window = new Window
        {
            Width = 520,
            Height = 720,
            Content = templateContent,
        };

        try
        {
            window.Show();
            await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle).Task;

            var chatListBox = messageStream.FindName("MessageListBox") as ListBox;
            Assert.IsNotNull(chatListBox, "共享 Copilot 消息流应生成消息列表。");
            Assert.AreEqual(new Thickness(8), chatListBox.Padding, "共享控件应内聚并统一消息列表 Padding。");
            var visibleTexts = FindVisualChildren<TextBlock>(templateContent)
                .Select(textBlock => textBlock.Text)
                .ToList();

            CollectionAssert.IsSubsetOf(
                new[] { reasoningText, toolInputText, toolOutputText },
                visibleTexts,
                "统一消息模板应完整显示思考内容以及工具调用的输入和输出。");
        }
        finally
        {
            window.Close();
        }
    }

    private static FrameworkElement CreateTemplateContent(DataTemplate? template, object dataContext)
    {
        Assert.IsNotNull(template, $"共享控件应为 {dataContext.GetType().Name} 提供模板。");
        var content = template.LoadContent() as FrameworkElement;
        Assert.IsNotNull(content, "共享消息项模板应生成可视内容。");
        content.DataContext = dataContext;
        return content;
    }

    private static async Task CopilotOutputShouldRenderAsVisibleTextInAnalysisViewAsync()
    {
        const string assistantText = "建议采用清晰、理性且适合课堂投影的统一视觉方向。";
        EnsureApplicationResources();
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "#### 内容\n```\n测试标题\n测试内容\n```")
            .Build();
        var responseGate = new StreamingResponseGate(assistantText);
        var agent = new CopilotCoursewareThemeAgent(
            new GatedCopilotChatManagerFactory(responseGate),
            new CoursewareThemeValidator());
        var analysisService = new CoursewareThemeAnalysisService(
            new CoursewareAnalysisInputBuilder(),
            agent);
        var viewModel = new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new DispatcherViewModelDispatcher(Dispatcher.CurrentDispatcher),
            analysisService);
        var view = new CoursewareAnalysisView
        {
            DataContext = viewModel,
        };
        var window = new Window
        {
            Width = 1360,
            Height = 820,
            Content = view,
        };

        try
        {
            window.Show();
            var openTask = viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
            await responseGate.TextEmitted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await PumpDispatcherUntilAsync(
                window,
                () => viewModel.AnalysisChatMessages.Any(message => message.Content.Contains(assistantText, StringComparison.Ordinal)));

            Assert.HasCount(2, viewModel.AnalysisChatMessages);
            Assert.AreEqual(ChatRole.User, viewModel.AnalysisChatMessages[0].Role);
            Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.AnalysisChatMessages[0].Content));
            Assert.AreEqual(ChatRole.Assistant, viewModel.AnalysisChatMessages[1].Role);
            var messageStream = view.FindName("AnalysisChatMessageStream") as CopilotMessageStream;
            Assert.IsNotNull(messageStream, "主题分析页应使用共享 Copilot 消息流控件。\n");
            var chatListBox = FindVisualChildren<ListBox>(messageStream).SingleOrDefault();
            Assert.IsNotNull(chatListBox, "共享 Copilot 消息流应生成消息列表。\n");
            chatListBox.UpdateLayout();
            var userMessageContainer = chatListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
            Assert.IsNotNull(userMessageContainer, "用户消息应生成可视容器。\n");
            var assistantMessageContainer = chatListBox.ItemContainerGenerator.ContainerFromIndex(1) as ListBoxItem;
            Assert.IsNotNull(assistantMessageContainer, "Copilot 消息应生成可视容器。\n");
            var userVisibleTexts = FindVisualChildren<TextBlock>(userMessageContainer)
                .Where(textBlock => textBlock.IsVisible)
                .Select(textBlock => textBlock.Text)
                .ToList();
            var assistantVisibleTexts = FindVisualChildren<TextBlock>(assistantMessageContainer)
                .Where(textBlock => textBlock.IsVisible)
                .Select(textBlock => textBlock.Text)
                .ToList();

            Assert.IsTrue(
                userVisibleTexts.Any(text => text.Contains(viewModel.AnalysisChatMessages[0].Content, StringComparison.Ordinal)),
                "课件分析输入应作为用户消息显示。\n");
            Assert.IsTrue(
                assistantVisibleTexts.Any(text => text.Contains(assistantText, StringComparison.Ordinal)),
                "Copilot 可读输出应出现在分析页的可见 TextBlock 中。\n");
            Assert.IsNull(
                FindVisualAncestor<ScrollViewer>(messageStream),
                "主题分析消息流外层不应再嵌套页面级滚动条。\n");
            var messageBubble = FindVisualChildren<Border>(assistantMessageContainer)
                .FirstOrDefault(border => border.MaxWidth > 0 && border.CornerRadius == new CornerRadius(16));
            Assert.IsNotNull(messageBubble, "Copilot 消息应使用聊天气泡。\n");
            Assert.AreEqual(860d, messageBubble.MaxWidth, "宽屏主题分析区域应使用更宽的消息气泡。\n");

            responseGate.Release.TrySetResult();
            await openTask;
        }
        finally
        {
            responseGate.Release.TrySetResult();
            window.Close();
        }
    }

    private static async Task AnalysisTabsShouldLockConversationAndSelectCompletedResultAsync()
    {
        EnsureApplicationResources();
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "#### 内容\n```\n测试标题\n测试内容\n```")
            .Build();
        var analysisStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseAnalysis = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var analysisService = new FakeCoursewareThemeAnalysisService(async (inputPackage, _, _, cancellationToken) =>
        {
            analysisStarted.TrySetResult();
            await releaseAnalysis.Task.WaitAsync(cancellationToken);
            return FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(inputPackage);
        });
        var viewModel = new CoursewareWorkspaceViewModel(
            new CoursewareFolderLoader(),
            new DispatcherViewModelDispatcher(Dispatcher.CurrentDispatcher),
            analysisService);
        var view = new CoursewareAnalysisView
        {
            DataContext = viewModel,
        };
        var window = new Window
        {
            Width = 1360,
            Height = 820,
            Content = view,
        };

        try
        {
            window.Show();
            var openTask = viewModel.OpenCoursewareFolderAsync(exportDirectory.FullName);
            await analysisStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await PumpDispatcherUntilAsync(window, () => viewModel.IsAnalyzingTheme);

            var tabControl = view.FindName("AnalysisTabControl") as TabControl;
            var resultTab = view.FindName("AnalysisThemeResultTab") as TabItem;
            Assert.IsNotNull(tabControl, "主题分析工作区应包含 TabControl。");
            Assert.IsNotNull(resultTab, "主题分析工作区应包含结果 Tab。");
            Assert.AreEqual(0, tabControl.SelectedIndex, "分析期间应保持显示分析对话。");
            Assert.IsFalse(resultTab.IsEnabled, "分析期间结果 Tab 应不可用。");

            releaseAnalysis.TrySetResult();
            await openTask;
            await PumpDispatcherUntilAsync(
                window,
                () => viewModel.IsAnalysisReady && resultTab.IsEnabled && tabControl.SelectedIndex == 1);

            Assert.AreEqual(1, tabControl.SelectedIndex, "分析完成后应自动显示主题结果。");
            Assert.IsTrue(resultTab.IsEnabled, "分析完成后结果 Tab 应可用。");

            tabControl.SelectedIndex = 0;
            await PumpDispatcherUntilAsync(
                window,
                () => viewModel.SelectedAnalysisTab == CoursewareAnalysisTab.Conversation);

            Assert.AreEqual(CoursewareAnalysisTab.Conversation, viewModel.SelectedAnalysisTab);
        }
        finally
        {
            releaseAnalysis.TrySetResult();
            window.Close();
        }
    }

    private static T? FindVisualAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent is not null)
        {
            if (parent is T result)
            {
                return result;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T result)
            {
                yield return result;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static void EnsureApplicationResources()
    {
        var application = Application.Current;
        if (application?.Resources.Contains("AnalysisPageBackgroundBrush") == true)
        {
            return;
        }

        application ??= new App();
        ((App) application).InitializeComponent();
    }

    private static async Task PumpDispatcherUntilAsync(Window window, Func<bool> condition)
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= timeout)
            {
                Assert.Fail("等待主题分析 Copilot 输出显示超时。");
            }

            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task;
            await Task.Delay(20);
        }

        await window.Dispatcher.InvokeAsync(() => window.UpdateLayout(), DispatcherPriority.ApplicationIdle).Task;
    }

    private static Task RunOnStaThreadAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            _ = action().ContinueWith(task =>
            {
                if (task.Exception is not null)
                {
                    taskCompletionSource.TrySetException(task.Exception.InnerExceptions);
                }
                else if (task.IsCanceled)
                {
                    taskCompletionSource.TrySetCanceled();
                }
                else
                {
                    taskCompletionSource.TrySetResult();
                }

                dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return taskCompletionSource.Task;
    }

    private sealed class GatedCopilotChatManagerFactory(StreamingResponseGate responseGate) : ICopilotChatManagerFactory
    {
        public Task<CopilotChatManager> CreateAsync(
            AgentWorkload workload,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fakeChatClient = new FakeChatClient
            {
                OnGetStreamingResponseAsync = (_, _, token) => responseGate.StreamAsync(token),
            };
            var chatManager = new CopilotChatManager();
            chatManager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider(fakeChatClient));
            return Task.FromResult(chatManager);
        }
    }

    private sealed class DispatcherViewModelDispatcher(Dispatcher dispatcher) : IViewModelDispatcher
    {
        public async Task InvokeAsync(Func<Task> action)
        {
            var task = await dispatcher.InvokeAsync(action).Task;
            await task;
        }

        public async Task InvokeAsync(Action action)
        {
            await dispatcher.InvokeAsync(action).Task;
        }
    }

    private sealed class StreamingResponseGate(string text)
    {
        public TaskCompletionSource TextEmitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async IAsyncEnumerable<ChatResponseUpdate> StreamAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            TextEmitted.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }
}
