using System.Globalization;
using System.Reflection;

using CodingChatRoom.AvaloniaShell.Converters;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

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
        Assert.IsFalse(viewModel.CanCompressConversation);
        Assert.IsFalse(viewModel.CompressConversationCommand.CanExecute(null));
        Assert.IsFalse(viewModel.StopCommand.CanExecute(null));
    }

    [TestMethod(DisplayName = "聊天视图应包含压缩对话按钮")]
    [Timeout(5000)]
    public void ChatViewShouldContainCompressConversationButton()
    {
        FieldInfo? buttonField = typeof(ChatView).GetField(
            "CompressConversationButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(buttonField);
    }

    [TestMethod(DisplayName = "窗口标题应显示已提交的工作路径")]
    [Timeout(5000)]
    public void WorkspaceTitleShouldIncludeCommittedWorkspacePath()
    {
        var converter = new WorkspaceTitleConverter();

        object title = converter.Convert(
            @"C:\Code\Demo",
            typeof(string),
            "CodingChatRoom 编程助手",
            CultureInfo.InvariantCulture);

        Assert.AreEqual(@"CodingChatRoom 编程助手 - C:\Code\Demo", title);
    }

    [TestMethod(DisplayName = "未提交工作路径时窗口标题应只显示应用名称")]
    [Timeout(5000)]
    public void WorkspaceTitleShouldUseApplicationTitleWhenPathIsEmpty()
    {
        var converter = new WorkspaceTitleConverter();

        object title = converter.Convert(
            null,
            typeof(string),
            "CodingChatRoom 编程助手",
            CultureInfo.InvariantCulture);

        Assert.AreEqual("CodingChatRoom 编程助手", title);
    }
}
