using System;
using SimpleWrite.Business.TextEditors.CommandPatterns;
using SimpleWrite.ViewModels;

namespace SimpleWrite.Business.PluginCommandPatterns;

/// <summary>
/// 插件式的命令提供器
/// </summary>
internal class PluginCommandPatternProvider(SimpleWriteMainViewModel mainViewModel)
{
    public ISidebarConversationPresenter? SidebarConversationPresenter => mainViewModel.SidebarConversationPresenter;

    public void AddPatterns(CommandPatternManager commandPatternManager)
    {
        ArgumentNullException.ThrowIfNull(commandPatternManager);
        ArgumentNullException.ThrowIfNull(mainViewModel);

        // 添加插件提供器
        commandPatternManager.AddCommandPattern(new RunCommandLineCommandPattern());
        commandPatternManager.AddCommandPattern(new OpenUrlCommandPattern());
        commandPatternManager.AddCommandPattern(new TextToBase64CommandPattern(this));
        commandPatternManager.AddCommandPattern(new Base64ToTextCommandPattern(this));
        commandPatternManager.AddCommandPattern(new TextToBinaryCommandPattern(this));
        commandPatternManager.AddCommandPattern(new BinaryToTextCommandPattern(this));
    }

    public bool CanShowSidebarConversation()
    {
        return SidebarConversationPresenter is not null;
    }

    public System.Threading.Tasks.Task ShowSidebarConversationAsync(string userText, string assistantText)
    {
        return SidebarConversationPresenter?.ShowConversationAsync(userText, assistantText)
               ?? System.Threading.Tasks.Task.CompletedTask;
    }
}