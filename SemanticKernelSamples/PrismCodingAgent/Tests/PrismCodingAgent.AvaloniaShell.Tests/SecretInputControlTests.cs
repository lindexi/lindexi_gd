using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Settings;
using PrismCodingAgent.AvaloniaShell.ViewModels;
using PrismCodingAgent.AvaloniaShell.Views;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class SecretInputControlTests
{
    [TestMethod]
    [DataRow("reveal")]
    [DataRow("mask")]
    [DataRow("text")]
    [DataRow("binding")]
    [DataRow("focus")]
    [DataRow("selection")]
    [DataRow("presenter")]
    [DataRow("single")]
    [DataRow("refocus")]
    public void WhenClickingRevealThenSingleInputPreservesEditingState(string outcome)
    {
        using var settings = new SettingsViewModel();
        var provider = settings.Providers[0];
        provider.ApiKey = "original-key";
        var view = new ModelServiceConnectionView { DataContext = provider };
        var window = new Window { Content = view, Width = 720, Height = 300 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var input = view.GetVisualDescendants().OfType<SecretInput>().Single();
            var button = input.GetVisualDescendants().OfType<ToggleButton>().Single();
            input.Focus();
            input.Text = "edited-key";
            input.SelectionStart = 2;
            input.SelectionEnd = 5;
            Click(window, button);

            switch (outcome)
            {
                case "reveal": Assert.IsTrue(input.RevealPassword); break;
                case "mask":
                    Click(window, button);
                    Assert.IsFalse(input.RevealPassword);
                    break;
                case "text":
                    Click(window, button);
                    Assert.AreEqual("edited-key", input.Text);
                    break;
                case "binding":
                    Click(window, button);
                    Assert.AreEqual("edited-key", provider.ApiKey);
                    break;
                case "focus": Assert.IsTrue(input.IsFocused); break;
                case "selection": Assert.AreEqual(5, input.SelectionEnd); break;
                case "presenter":
                    Assert.IsTrue(input.GetVisualDescendants().OfType<TextPresenter>().Single().RevealPassword);
                    break;
                case "single": Assert.HasCount(1, view.GetVisualDescendants().OfType<SecretInput>()); break;
                case "refocus":
                    view.GetVisualDescendants().OfType<TextBox>().Single(box => box is not SecretInput).Focus();
                    input.Focus();
                    Click(window, button);
                    Assert.IsTrue(input.RevealPassword);
                    break;
            }
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void WhenOfficialServiceChangesThenDeepSeekInputUpdatesImmediately()
    {
        using var settings = new SettingsViewModel();
        var view = new SettingsView { DataContext = settings };
        var input = view.FindControl<SecretInput>("DeepSeekApiKeyInput")!;
        settings.Providers[0].EndPoint = "https://api.deepseek.com/v1";
        settings.Providers[0].ApiKey = "official-key";
        Dispatcher.UIThread.RunJobs();

        Assert.AreEqual("official-key", input.Text);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void WhenRevealIsCheckedThenBackgroundMeetsInputInnerRightEdge(bool rounded, bool focused)
    {
        var input = new SecretInput { Text = "test-key", Width = 300 };
        if (rounded) input.Classes.Add("Rounded");
        var window = new Window { Content = input, Width = 400, Height = 100 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var button = input.GetVisualDescendants().OfType<ToggleButton>().Single();
            button.IsChecked = true;
            if (focused) input.Focus();
            window.UpdateLayout();
            using var bitmap = window.CaptureRenderedFrame()
                ?? throw new InvalidOperationException("Headless frame unavailable.");
            string screenshotDirectory = System.IO.Path.Join(AppContext.BaseDirectory, "SecretInputScreenshots");
            System.IO.Directory.CreateDirectory(screenshotDirectory);
            bitmap.Save(System.IO.Path.Join(screenshotDirectory, $"checked-rounded-{rounded}-focused-{focused}.png"));
            var border = input.GetVisualDescendants().OfType<Border>().Single(item => item.Name == "PART_BorderElement");
            var right = button.TranslatePoint(new Point(button.Bounds.Width, 0), border)!.Value.X;
            var surface = input.GetVisualDescendants().OfType<Border>().Single(item => item.Parent is Panel && item != border);
            double surfaceRight = surface.TranslatePoint(new Point(surface.Bounds.Width, 0), border)!.Value.X;
            Assert.AreEqual(surfaceRight, right, 0.01);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public async Task WhenLoadingOfficialProviderIntoBoundViewThenKeyIsVisible()
    {
        string root = System.IO.Path.Join(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new CodingChatSettingsService(
            CodingChatRoomPaths.Create(root),
            new WindowsSandboxToolSource(false, string.Empty, string.Empty));
        await service.SaveAsync(new AgentApiManagerConfiguration
        {
            OpenAIConfigurationList = [new("https://api.deepseek.com", "loaded-test-key")],
        }, new CodingChatShellSettings());
        using var settings = new SettingsViewModel(service, static () => { });
        var view = new SettingsView { DataContext = settings };
        var window = new Window { Content = view, Width = 900, Height = 700 };
        try
        {
            window.Show();
            await settings.LoadAsync();
            await settings.LoadAsync();
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("loaded-test-key", view.FindControl<SecretInput>("DeepSeekApiKeyInput")!.Text);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void WhenBoundAutomaticKeyChangesThenProviderStillTracks()
    {
        using var settings = new SettingsViewModel();
        var view = new SettingsView { DataContext = settings };
        var window = new Window { Content = view, Width = 900, Height = 700 };
        try
        {
            window.Show();
            settings.Providers[0].EndPoint = "https://api.deepseek.com";
            settings.Providers[0].ApiKey = "first-test-key";
            Dispatcher.UIThread.RunJobs();
            settings.Providers[0].ApiKey = "second-test-key";
            Dispatcher.UIThread.RunJobs();
            var input = view.FindControl<SecretInput>("DeepSeekApiKeyInput")!;
            input.Text = string.Empty;
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual("second-test-key", input.Text);
        }
        finally { window.Close(); }
    }

    private static void Click(Window window, ToggleButton button)
    {
        window.UpdateLayout();
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }
}
