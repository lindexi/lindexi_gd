using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using PrismCodingAgent.AvaloniaShell.Converters;
using PrismCodingAgent.AvaloniaShell.ViewModels;
using ChatView = PrismCodingAgent.AvaloniaShell.Views.ChatView;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class ShellControlStyleTests
{
    [TestMethod]
    public void HistoryIconShouldAlignWithReasoningSelectorBottom()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 980, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var history = view.FindControl<Button>("TaskHistoryButton") ?? throw new InvalidOperationException("History missing.");
            var reasoning = view.FindControl<ComboBox>("ReasoningEffortSelector") ?? throw new InvalidOperationException("Reasoning missing.");
            var historyPosition = history.TranslatePoint(default, view) ?? throw new InvalidOperationException("Position missing.");
            var reasoningPosition = reasoning.TranslatePoint(default, view) ?? throw new InvalidOperationException("Position missing.");
            Assert.AreEqual(reasoningPosition.Y + reasoning.Bounds.Height, historyPosition.Y + history.Bounds.Height, 0.5);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void NewSessionShouldFollowLanguageServerButton()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 980, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var create = view.FindControl<Button>("NewSessionButton") ?? throw new InvalidOperationException("New session missing.");
            var lsp = view.FindControl<Button>("StopLanguageServerButton") ?? throw new InvalidOperationException("LSP missing.");
            Assert.IsGreaterThanOrEqualTo(lsp.Bounds.Right, create.Bounds.Left);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(":pointerover")]
    [DataRow(":pressed")]
    public void OrdinaryButtonsShouldNotInheritDangerTextColor(string state)
    {
        var normal = new StateButton { Content = "History" };
        var danger = new StateButton { Content = "Delete", Classes = { "Danger" } };
        var window = new Window { Content = new StackPanel { Children = { normal, danger } } };
        try
        {
            window.Show();
            normal.SetState(state, true);
            var presenter = normal.GetVisualDescendants().OfType<ContentPresenter>().First(item => item.Name == "PART_ContentPresenter");
            var brush = presenter.Foreground as ISolidColorBrush
                ?? throw new InvalidOperationException("Foreground is not a solid brush.");
            Assert.AreEqual(brush.Color.R, brush.Color.G);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void MainWindowShouldRequestMicaWithOpaqueFallback()
    {
        var window = new PrismCodingAgent.AvaloniaShell.MainWindow();
        try
        {
            CollectionAssert.AreEqual(new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None }, window.TransparencyLevelHint.ToArray());
            Assert.AreEqual(Colors.White, ((ISolidColorBrush) window.TransparencyBackgroundFallback).Color);
            var background = window.Background as ISolidColorBrush
                ?? throw new InvalidOperationException("Window background is not a solid brush.");
            Assert.AreEqual(Colors.White, background.Color);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void MicaTransparencyShouldUseTransparentWindowBackground()
    {
        var converter = new WindowTransparencyBackgroundConverter();

        var background = (ISolidColorBrush) converter.Convert(WindowTransparencyLevel.Mica, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.AreEqual(Colors.Transparent, background.Color);
    }

    [TestMethod]
    [DataRow(":pointerover")]
    [DataRow(":pressed")]
    [DataRow(":disabled")]
    public void PrimaryButtonShouldUseFluentAccentStates(string state)
    {
        var actual = new StateButton { Content = "Send", Classes = { "Primary", "accent" } };
        var reference = new StateButton { Content = "Send", Classes = { "accent" } };
        var window = new Window { Content = new StackPanel { Children = { actual, reference } } };
        try
        {
            window.Show();
            actual.SetState(state, true);
            reference.SetState(state, true);
            Assert.AreEqual(GetColors(reference), GetColors(actual));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow("Flat")]
    [DataRow("Icon")]
    [DataRow("Navigation")]
    [DataRow("TaskSelect")]
    public void LightweightButtonsShouldUseFluentHoverFeedback(string style)
    {
        var actual = new StateButton { Content = "Action", Classes = { style } };
        var reference = new StateButton { Content = "Action" };
        var window = new Window { Content = new StackPanel { Children = { actual, reference } } };
        try
        {
            window.Show();
            actual.SetState(":pointerover", true);
            reference.SetState(":pointerover", true);
            Assert.AreEqual(GetColors(reference), GetColors(actual));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void HoveredIconShouldInheritFluentPresenterForeground()
    {
        var icon = new PathIcon();
        var button = new StateButton { Content = icon, Classes = { "Primary", "accent" } };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.SetState(":pointerover", true);
            Assert.AreEqual(GetColors(button).Foreground, icon.Foreground?.ToString());
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void ButtonShouldAcceptKeyboardNavigationFocus()
    {
        var button = new StateButton { Content = "Action" };
        var window = new Window { Content = button };
        try
        {
            window.Show();
            button.Focus(Avalonia.Input.NavigationMethod.Tab);
            Assert.IsTrue(button.IsFocused);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void ComposerActionsShouldFitAtMinimumWindowWidth()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var checkbox = view.FindControl<CheckBox>("EnableDotNetRunCheckBox") ?? throw new InvalidOperationException("Checkbox missing.");
            var button = view.FindControl<Button>("CompressConversationButton") ?? throw new InvalidOperationException("Button missing.");
            Point checkboxOrigin = checkbox.TranslatePoint(default, view) ?? throw new InvalidOperationException("Checkbox position missing.");
            Point buttonOrigin = button.TranslatePoint(default, view) ?? throw new InvalidOperationException("Button position missing.");
            Assert.IsLessThanOrEqualTo(buttonOrigin.X, checkboxOrigin.X + checkbox.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void ComposerPanelShouldRemainCompactWithoutAttachments()
    {
        var view = new ChatView { DataContext = new ChatViewModel() };
        var window = new Window { Width = 1016, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var panel = view.FindControl<Border>("ComposerPanel") ?? throw new InvalidOperationException("Composer missing.");
            Assert.IsLessThanOrEqualTo(150, panel.Bounds.Height, $"Composer height: {panel.Bounds.Height}");
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(640)]
    [DataRow(1160)]
    public void MessageContentShouldFitInsideScrollViewport(int width)
    {
        using var chat = new ChatViewModel();
        chat.Messages.Add(new MessageItemViewModel(AgentLib.Model.CopilotChatMessage.CreateAssistant(
            string.Concat(Enumerable.Repeat("Long message text should wrap inside the viewport. ", 40)), isPresetInfo: false)));
        var view = new ChatView { DataContext = chat };
        var window = new Window { Width = width, Height = 620, Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var scroll = view.FindControl<ScrollViewer>("MessagesScrollViewer")
                ?? throw new InvalidOperationException("Message scroll missing.");
            var items = view.FindControl<ItemsControl>("MessagesItemsControl")
                ?? throw new InvalidOperationException("Message items missing.");
            var presenter = scroll.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
            var origin = items.TranslatePoint(default, presenter)
                ?? throw new InvalidOperationException("Message position missing.");

            var viewOrigin = items.TranslatePoint(default, view)
                ?? throw new InvalidOperationException("Message position missing.");
            var output = items.GetVisualDescendants().OfType<TextBox>()
                .Single(textBox => textBox.Classes.Contains("ChatOutput"));
            var outputOrigin = output.TranslatePoint(default, presenter)
                ?? throw new InvalidOperationException("Assistant output position missing.");
            var textPresenter = output.GetVisualDescendants().OfType<TextPresenter>().Single();
            var innerScroll = output.GetVisualDescendants().OfType<ScrollViewer>().Single();
            Console.WriteLine($"Output={output.Bounds.Width}, text={textPresenter.Bounds.Width}, layout={textPresenter.TextLayout.Width}, inner viewport={innerScroll.Viewport.Width}, extent={innerScroll.Extent.Width}, horizontal={innerScroll.HorizontalScrollBarVisibility}");
            Assert.IsTrue(textPresenter.TextLayout.Width <= innerScroll.Viewport.Width + 0.5,
                $"Text layout={textPresenter.TextLayout.Width}, inner viewport={innerScroll.Viewport.Width}");
            Assert.IsTrue(origin.X >= 32 && origin.X + items.Bounds.Width <= presenter.Bounds.Width - 16 + 0.5
                && outputOrigin.X + output.Bounds.Width <= presenter.Bounds.Width - 16 + 0.5
                && scroll.Margin == default(Thickness)
                && viewOrigin.X >= 32 && viewOrigin.X + items.Bounds.Width <= view.Bounds.Width - 16 + 0.5,
                $"Content x={origin.X}, width={items.Bounds.Width}, viewport={presenter.Bounds.Width}, view x={viewOrigin.X}");
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void PresetGreetingShouldUseWelcomeLayout()
    {
        using var chat = new ChatViewModel();
        chat.Messages.Add(new MessageItemViewModel(AgentLib.Model.CopilotChatMessage.CreateAssistant("Greeting", isPresetInfo: true)));
        Assert.IsTrue(chat.ShowWelcome);
    }

    [TestMethod]
    public void RealAssistantMessageShouldNotBeHiddenByWelcomeLayout()
    {
        using var chat = new ChatViewModel();
        chat.Messages.Add(new MessageItemViewModel(AgentLib.Model.CopilotChatMessage.CreateAssistant("Answer", isPresetInfo: false)));
        Assert.IsFalse(chat.ShowWelcome);
    }

    private static (string? Background, string? Foreground) GetColors(Button button)
    {
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First(item => item.Name == "PART_ContentPresenter");
        return (presenter.Background?.ToString(), presenter.Foreground?.ToString());
    }

    private sealed class StateButton : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);
        internal void SetState(string state, bool value) => PseudoClasses.Set(state, value);
    }
}
