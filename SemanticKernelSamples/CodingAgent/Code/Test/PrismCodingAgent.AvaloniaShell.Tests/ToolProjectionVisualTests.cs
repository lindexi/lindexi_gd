using AgentLib.Model;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using Microsoft.Extensions.AI;
using PrismCodingAgent.AvaloniaShell.ViewModels;
using PrismCodingAgent.AvaloniaShell.Views;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[TestClass]
public sealed class ToolProjectionVisualTests
{
    [TestMethod]
    public async Task ToolDetailsShouldUseStructuredTemplatesWithoutResettingCollapse()
    {
        await AvaloniaTestClassAttribute.Session.Dispatch(() =>
        {
            var view = new ChatView();
            var tool = new CopilotChatToolItem("call", "inspect", "input", "returned")
            {
                Details = CopilotChatMessage.CreateAssistant("detail text", false)
            };
            tool.Details.AppendReasoning("detail reasoning");
            tool.Details.AppendFunctionCall(new FunctionCallContent("inner", "read", new Dictionary<string, object?>()));
            var items = new ItemsControl { ItemsSource = new[] { tool } };
            view.Content = items;
            var window = new Window { Content = view };
            try
            {
                window.Show();
                window.UpdateLayout();
                var expander = items.GetVisualDescendants().OfType<Expander>().Single();
                expander.IsExpanded = true;
                window.UpdateLayout();
                Assert.IsTrue(items.GetVisualDescendants().OfType<TextBox>().Any(text => text.Text == "detail text"));
                Assert.HasCount(3, items.GetVisualDescendants().OfType<Expander>().ToArray());
                expander.IsExpanded = false;
                tool.Details.AppendText(" continued");
                window.UpdateLayout();
                Assert.IsFalse(expander.IsExpanded);
            }
            finally
            {
                window.Close();
            }
            return true;
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task SubAgentShouldRenderNestedToolsAndSubAgents()
    {
        await AvaloniaTestClassAttribute.Session.Dispatch(() =>
        {
            var subAgent = new CopilotChatSubAgentItem("call", "history", "input");
            var nested = new CopilotChatSubAgentItem("nested", "nested agent", "nested input");
            nested.MessageItems.Add(new CopilotChatTextItem("nested text"));
            subAgent.MessageItems.Add(nested);
            subAgent.MessageItems.Add(new CopilotChatToolItem("tool", "read", "tool input"));
            var view = new ChatView { Content = new ItemsControl { ItemsSource = new[] { subAgent } } };
            var window = new Window { Content = view };
            try
            {
                window.Show();
                window.UpdateLayout();
                Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>().Any(text => text.Text == "nested text"));
                Assert.HasCount(3, view.GetVisualDescendants().OfType<Expander>().ToArray());
            }
            finally
            {
                window.Close();
            }
            return true;
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task ApprovalShouldMatchBeforeOrdinaryToolTemplate()
    {
        await AvaloniaTestClassAttribute.Session.Dispatch(() =>
        {
            var view = new ChatView();
            var approval = new CopilotChatApprovalToolItem("approval", "write", "input");
            Control card = view.FindDataTemplate(approval)!.Build(approval)!;
            Assert.IsTrue(Assert.IsInstanceOfType<Border>(card).Classes.Contains("ApprovalPanel"));
            return true;
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task ImageShouldMatchNativeImageTemplate()
    {
        await AvaloniaTestClassAttribute.Session.Dispatch(() =>
        {
            var view = new ChatView();
            var image = new CopilotChatImageItem(BinaryData.FromBytes([1, 2, 3]), "image/png");
            var button = Assert.IsInstanceOfType<Button>(view.FindDataTemplate(image)!.Build(image));
            Assert.IsInstanceOfType<BinaryImage>(button.Content);
            return true;
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task UserAndAssistantTextShouldKeepDifferentStyles()
    {
        await AvaloniaTestClassAttribute.Session.Dispatch(() =>
        {
            using var model = new ChatViewModel();
            model.Messages.Add(new MessageItemViewModel(new CopilotChatMessage(ChatRole.User, "user text")));
            model.Messages.Add(new MessageItemViewModel(new CopilotChatMessage(ChatRole.Assistant, "assistant text")));
            var view = new ChatView { DataContext = model };
            var window = new Window { Content = view, Width = 1000, Height = 800 };
            try
            {
                window.Show();
                window.UpdateLayout();
                var texts = view.GetVisualDescendants().OfType<TextBox>().Where(text => text.IsEffectivelyVisible).ToArray();
                Assert.IsTrue(texts.Single(text => text.Text == "user text").Classes.Contains("UserChatOutput"));
                Assert.IsFalse(texts.Single(text => text.Text == "assistant text").Classes.Contains("UserChatOutput"));
            }
            finally
            {
                window.Close();
            }
            return true;
        }, CancellationToken.None);
    }
}
