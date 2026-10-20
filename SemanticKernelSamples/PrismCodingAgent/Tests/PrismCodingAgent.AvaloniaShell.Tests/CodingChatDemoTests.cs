using AgentLib.Model;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Localizations;
using PrismCodingAgent.AvaloniaShell.Views;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class CodingChatDemoTests
{
    [DataTestMethod]
    [DataRow("--demo-zh", "zh-Hans")]
    [DataRow("--DEMO-EN", "en")]
    [DataRow("--demo-zh=false", null)]
    [DataRow("--demo-en=false", null)]
    public void DemoArgumentsShouldMatchExactly(string argument, string? language)
    {
        Assert.AreEqual(language, CodingChatStartupOptions.FromArguments([argument]).DemoLanguage);
    }

    [DataTestMethod]
    [DataRow("zh-Hans")]
    [DataRow("en")]
    public async Task DemoShouldHaveSixTasksWithTwoWorking(string language)
    {
        Lang.SetCurrent(language);
        await using var main = CodingChatDemoFactory.Create();
        CollectionAssert.AreEqual(new[] { true, true, false, false, false, false },
            main.WorkTasks.Select(task => task.IsWorking).ToArray());
    }

    [TestMethod]
    public async Task DemoShouldSelectExactlyOneIdleTask()
    {
        await using var main = CodingChatDemoFactory.Create();
        Assert.AreSame(main.ActiveWorkTask, main.WorkTasks.Single(task => task.IsActive && !task.IsWorking));
    }

    [TestMethod]
    public async Task SwitchingTasksShouldKeepChatsWithoutRuntimeOrSend()
    {
        await using var main = CodingChatDemoFactory.Create();
        var chats = main.WorkTasks.Select(task => task.Chat).ToArray();
        foreach (var task in main.WorkTasks)
        {
            main.ActivateWorkTaskCommand.Execute(task);
            Assert.IsTrue(ReferenceEquals(main.ChatViewModel, task.Chat)
                && task.Runtime is null && !task.Chat.SendCommand.CanExecute(null)
                && !task.Chat.StopCommand.CanExecute(null)
                && !task.Chat.CompressConversationCommand.CanExecute(null));
        }
        CollectionAssert.AreEqual(chats, main.WorkTasks.Select(task => task.Chat).ToArray());
    }

    [DataTestMethod]
    [DataRow("zh-Hans", "第1-160行", "第1-100行", "应用中英文本地化")]
    [DataRow("en", "Lines 1–160", "Lines 1–100", "Localize the application")]
    public async Task DemoShouldRenderRealToolTemplatesInBothLanguages(
        string language, string testLines, string settingsLines, string selectedName)
    {
        {
            Lang.SetCurrent(language);
            await using var main = CodingChatDemoFactory.Create();
            var window = new MainWindow { DataContext = main, Height = 1160 };
            try
            {
                window.Show();
                window.UpdateLayout();
                Assert.IsTrue(main.IsDemo);
                Assert.AreEqual(selectedName, main.ActiveWorkTask.DisplayName);
                Assert.IsFalse(window.GetVisualDescendants().OfType<TextBlock>()
                    .Any(block => block.Text == Lang.Current.ModelSetupTitle && block.IsEffectivelyVisible));
                var assistant = main.ChatViewModel.Messages.Last();
                CollectionAssert.AreEqual(new[] { "ReadFileLines", "ReadFileLines" },
                    assistant.MessageItems.OfType<CopilotChatToolItem>().Select(tool => tool.ToolName).ToArray());
                Assert.HasCount(2, assistant.MessageItems.OfType<CopilotChatSubAgentItem>().ToArray());
                var chat = window.GetVisualDescendants().OfType<ChatView>().Single();
                var headers = chat.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "ReadFileLines", "LocalizationSettingsTests.cs",
                    testLines, "Settings/CodingChatShellSettings.cs", settingsLines, "InvokeCodingSubAgent" }, headers);
                Assert.IsTrue(chat.GetVisualDescendants().OfType<Expander>().Count() >= 4);
                using var bitmap = window.CaptureRenderedFrame()
                    ?? throw new InvalidOperationException("Demo window did not render a frame.");
                string screenshotDirectory = Path.Combine(AppContext.BaseDirectory, "TestResults", "Screenshots");
                Directory.CreateDirectory(screenshotDirectory);
                bitmap.Save(Path.Combine(screenshotDirectory, $"coding-chat-demo-{language}.png"),
                    PngBitmapEncoderOptions.Default);
            }
            finally
            {
                window.Close();
                Lang.SetCurrent("zh-Hans");
            }
        }
    }
}
