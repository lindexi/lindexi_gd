using AgentLib.Model;
using Microsoft.Extensions.AI;
using PrismCodingAgent.AvaloniaShell.Localizations;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class StartupLifetimeTests
{
    [DataTestMethod]
    [DataRow(false, "Task")]
    [DataRow(false, "Chat")]
    [DataRow(false, "Message")]
    [DataRow(true, "Task")]
    [DataRow(true, "Chat")]
    [DataRow(true, "Message")]
    public async Task WhenStartupFailsThenOwnedViewModelsStopReceivingLanguageChanges(bool hasMainViewModel, string observedObject)
    {
        string originalLanguage = Lang.Current.IetfLanguageTag;
        Lang.SetCurrent("zh-Hans");
        MainViewModel? mainViewModel = hasMainViewModel ? new MainViewModel() : null;
        WorkTaskItemViewModel task = mainViewModel?.ActiveWorkTask
            ?? new WorkTaskItemViewModel("Task", new ChatViewModel(), new SessionListViewModel());
        var message = new MessageItemViewModel(new CopilotChatMessage(ChatRole.Assistant, "Reply"));
        task.Chat.Messages.Add(message);
        ViewModelBase observed = observedObject switch
        {
            "Task" => task,
            "Chat" => task.Chat,
            "Message" => message,
            _ => throw new ArgumentOutOfRangeException(nameof(observedObject)),
        };
        int notifications = 0;
        observed.PropertyChanged += (_, _) => notifications++;

        try
        {
            await App.DisposeStartupStateAsync(mainViewModel, [task], []);
            notifications = 0;

            Lang.SetCurrent("en");

            Assert.AreEqual(0, notifications);
        }
        finally
        {
            // 断言失败也不能让全局语言事件持有测试创建的对象。
            task.Detach();
            task.Chat.Dispose();
            task.Sessions.Dispose();
            Lang.SetCurrent(originalLanguage);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WhenStartupFailsThenOwnedChatMessagesAreCleared(bool hasMainViewModel)
    {
        MainViewModel? mainViewModel = hasMainViewModel ? new MainViewModel() : null;
        WorkTaskItemViewModel task = mainViewModel?.ActiveWorkTask
            ?? new WorkTaskItemViewModel("Task", new ChatViewModel(), new SessionListViewModel());
        task.Chat.Messages.Add(new MessageItemViewModel(new CopilotChatMessage(ChatRole.Assistant, "Reply")));

        try
        {
            await App.DisposeStartupStateAsync(mainViewModel, [task], []);

            Assert.IsEmpty(task.Chat.Messages);
        }
        finally
        {
            task.Detach();
            task.Chat.Dispose();
            task.Sessions.Dispose();
        }
    }
}
