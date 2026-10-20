using System.Linq;
using AgentLib.Model;
using PrismCodingAgent.AvaloniaShell.Localizations;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Infrastructure;

internal static class CodingChatDemoFactory
{
    internal static MainViewModel Create()
    {
        var text = Lang.Current;
        WorkTaskItemViewModel[] tasks = text.DemoTaskNames.ToString().Split('|')
            .Select((name, index) => new WorkTaskItemViewModel(name,
                new ChatViewModel(index < 2, @"C:\Projects\PrismCodingAgent", text.DemoModel),
                new SessionListViewModel()))
            .ToArray();

        foreach (var task in tasks)
        {
            task.Chat.Messages.Add(new MessageItemViewModel(CopilotChatMessage.CreateUser(text.DemoUser)));
            var assistant = CopilotChatMessage.CreateAssistant(text.DemoIntroduction, false);
            assistant.MessageItems.Add(new CopilotChatToolItem("demo-tests", "ReadFileLines",
                "LocalizationSettingsTests.cs:1-160", text.DemoReview,
                new ToolCallPresentation("LocalizationSettingsTests.cs", text.DemoLinesTests,
                    "Tests/PrismCodingAgent.AvaloniaShell.Tests/LocalizationSettingsTests.cs")));
            assistant.MessageItems.Add(new CopilotChatTextItem(text.DemoProgress));
            var review = new CopilotChatSubAgentItem("demo-review", "InvokeCodingSubAgent", text.DemoReview, text.DemoResult);
            assistant.MessageItems.Add(review);
            assistant.MessageItems.Add(new CopilotChatToolItem("demo-settings", "ReadFileLines",
                "Settings/CodingChatShellSettings.cs:1-100", text.DemoResult,
                new ToolCallPresentation("Settings/CodingChatShellSettings.cs", text.DemoLinesSettings,
                    "Services/Settings/CodingChatShellSettings.cs")));
            var tests = new CopilotChatSubAgentItem("demo-verify", "InvokeCodingSubAgent", text.DemoReview, text.DemoResult);
            assistant.MessageItems.Add(tests);
            assistant.MessageItems.Add(new CopilotChatTextItem(text.DemoResult));
            task.Chat.Messages.Add(new MessageItemViewModel(assistant));
        }

        return MainViewModel.CreateDemo(tasks);
    }
}
