using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ShellStructureTests
{
    [TestMethod(DisplayName = "主 ViewModel 只应组合历史会话与聊天区域")]
    [Timeout(5000)]
    public void MainViewModelShouldOnlyComposeSessionListAndChat()
    {
        var viewModel = new MainViewModel();

        Assert.IsNotNull(viewModel.SessionListViewModel);
        Assert.IsNotNull(viewModel.ChatViewModel);

        string[] publicPropertyNames = typeof(MainViewModel)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[] { nameof(MainViewModel.ChatViewModel), nameof(MainViewModel.IsBusy), nameof(MainViewModel.SessionListViewModel) },
            publicPropertyNames);
    }

    [TestMethod(DisplayName = "Shell 程序集不应引用 AgentLib.ChatRoom")]
    [Timeout(5000)]
    public void ShellAssemblyShouldNotReferenceAgentLibChatRoom()
    {
        string[] referencedAssemblies = typeof(MainViewModel).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();

        CollectionAssert.DoesNotContain(referencedAssemblies, "AgentLib.ChatRoom");
    }

    [TestMethod(DisplayName = "基础聊天命令在真实应用服务接入前应保持禁用")]
    [Timeout(5000)]
    public void PlaceholderChatCommandsShouldRemainDisabled()
    {
        var viewModel = new ChatViewModel();

        Assert.IsFalse(viewModel.CanSend);
        Assert.IsFalse(viewModel.SendCommand.CanExecute(null));
        Assert.IsFalse(viewModel.StopCommand.CanExecute(null));
    }
}
