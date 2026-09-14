using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
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
            Assert.IsTrue(create.Bounds.Left >= lsp.Bounds.Right);
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
        var window = new MainWindow();
        try
        {
            CollectionAssert.AreEqual(new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None }, window.TransparencyLevelHint.ToArray());
            Assert.AreEqual(Colors.White, ((ISolidColorBrush) window.TransparencyBackgroundFallback).Color);
            Assert.AreEqual(Colors.White, ((ISolidColorBrush) window.Background).Color);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void MicaTransparencyShouldUseTransparentWindowBackground()
    {
        var converter = new Converters.WindowTransparencyBackgroundConverter();

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
            Assert.IsTrue(checkboxOrigin.X + checkbox.Bounds.Width <= buttonOrigin.X);
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
            Assert.IsTrue(panel.Bounds.Height <= 150, $"Composer height: {panel.Bounds.Height}");
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
