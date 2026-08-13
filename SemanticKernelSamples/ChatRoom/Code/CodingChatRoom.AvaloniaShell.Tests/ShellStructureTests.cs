using System.Globalization;
using System.Reflection;

using CodingChatRoom.AvaloniaShell.Converters;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ShellStructureTests
{
    [TestMethod(DisplayName = "主 ViewModel 应组合历史会话、聊天与设置导航")]
    [Timeout(5000)]
    public void MainViewModelShouldComposeSessionListChatAndSettingsNavigation()
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
            new[]
            {
                nameof(MainViewModel.ChatViewModel),
                nameof(MainViewModel.IsBusy),
                nameof(MainViewModel.IsChatOpen),
                nameof(MainViewModel.IsSettingsOpen),
                nameof(MainViewModel.OpenSettingsCommand),
                nameof(MainViewModel.SessionListViewModel),
                nameof(MainViewModel.SettingsViewModel),
            },
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

    [TestMethod(DisplayName = "聊天视图应包含循环迭代勾选框")]
    [Timeout(5000)]
    public void ChatViewShouldContainLoopIterationCheckBox()
    {
        FieldInfo? checkBoxField = typeof(ChatView).GetField(
            "LoopIterationCheckBox",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(checkBoxField);
    }

    [TestMethod(DisplayName = "聊天视图应包含模型选择下拉框")]
    [Timeout(5000)]
    public void ChatViewShouldContainModelSelector()
    {
        FieldInfo? comboBoxField = typeof(ChatView).GetField(
            "ModelSelector",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(comboBoxField);
    }

    [TestMethod(DisplayName = "聊天视图应包含自动压缩勾选框")]
    [Timeout(5000)]
    public void ChatViewShouldContainAutomaticCompressionCheckBox()
    {
        FieldInfo? checkBoxField = typeof(ChatView).GetField(
            "AutomaticCompressionCheckBox",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(checkBoxField);
    }

    [TestMethod(DisplayName = "聊天视图模型默认应启用自动压缩")]
    [Timeout(5000)]
    public void ChatViewModelShouldEnableAutomaticCompressionByDefault()
    {
        var viewModel = new ChatViewModel();

        Assert.IsTrue(viewModel.IsAutomaticCompressionEnabled);
    }

    [TestMethod(DisplayName = "窗口标题应显示已提交的工作路径")]
    [Timeout(5000)]
    public void WorkspaceTitleShouldIncludeCommittedWorkspacePath()
    {
        var converter = new WorkspaceTitleConverter();

        object title = converter.Convert(
            [@"C:\Code\Demo", "修复窗口标题"],
            typeof(string),
            "CodingChatRoom 编程助手",
            CultureInfo.InvariantCulture);

        Assert.AreEqual(@"CodingChatRoom 编程助手 - C:\Code\Demo - 修复窗口标题", title);
    }

    [TestMethod(DisplayName = "未提交工作路径时窗口标题应只显示应用名称")]
    [Timeout(5000)]
    public void WorkspaceTitleShouldUseApplicationTitleWhenPathIsEmpty()
    {
        var converter = new WorkspaceTitleConverter();

        object title = converter.Convert(
            [null, null],
            typeof(string),
            "CodingChatRoom 编程助手",
            CultureInfo.InvariantCulture);

        Assert.AreEqual("CodingChatRoom 编程助手", title);
    }
}
