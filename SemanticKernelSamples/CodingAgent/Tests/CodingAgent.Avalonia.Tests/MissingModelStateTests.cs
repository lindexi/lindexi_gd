using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CodingChatRoom.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class MissingModelStateTests
{
    [TestMethod]
    public void ModelSetupShouldScrollEntirePaddedContentIntoView()
    {
        var window = new MainWindow { DataContext = new MainViewModel(), Height = 720 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var scroll = window.GetVisualDescendants().OfType<ScrollViewer>()
                .Single(item => item.Name == "ModelSetupScrollViewer");
            var content = (Border)scroll.Content!;
            var settings = new SettingsViewModel();
            settings.Providers.Single().Models.Add(new ModelSettingsViewModel { ModelName = "test-model" });
            content.DataContext = settings;
            window.UpdateLayout();
            scroll.ScrollToEnd();
            window.UpdateLayout();
            var editor = content.GetVisualDescendants().OfType<ModelSettingsView>().Single();
            var addService = editor.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Command == settings.AddProviderCommand);
            var position = addService.TranslatePoint(default, scroll)
                ?? throw new InvalidOperationException("Button position unavailable.");
            Assert.IsTrue(position.Y >= 0 && position.Y + addService.Bounds.Height <= scroll.Viewport.Height + 1);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void NewSettingsShouldNotCreatePlaceholderModel()
    {
        var settings = new SettingsViewModel();
        Assert.IsEmpty(settings.Providers.Single().Models);
    }

    [TestMethod]
    public void LoadingEmptyServiceShouldNotCreatePlaceholderModel()
    {
        var provider = ProviderSettingsViewModel.FromConfiguration(
            new OpenAIProtocolLanguageModelConfiguration("https://example.com/v1", ""));
        Assert.IsEmpty(provider.Models);
    }

    [TestMethod]
    public void EmptyServiceShouldNotDisplayModelCapabilityEditor()
    {
        var view = new ModelSettingsView { DataContext = new SettingsViewModel() };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.IsFalse(view.GetVisualDescendants().OfType<NumericUpDown>().Any());
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void SharedModelEditorShouldNotContainPreferredModelInput()
    {
        var model = new SettingsViewModel { PrimaryModel = "unique-preferred-model" };
        var view = new ModelSettingsView { DataContext = model };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.IsFalse(view.GetVisualDescendants().OfType<TextBox>().Any(input => input.Text == model.PrimaryModel));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void FullSettingsShouldRetainPreferredModelInput()
    {
        var model = new SettingsViewModel { PrimaryModel = "unique-preferred-model" };
        var view = new SettingsView { DataContext = model };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.IsTrue(view.GetVisualDescendants().OfType<TextBox>().Any(input => input.Text == model.PrimaryModel));
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void LastModelShouldBeRemovable()
    {
        var provider = new ProviderSettingsViewModel();
        var model = new ModelSettingsViewModel { Provider = "test", ModelName = "test-model" };
        provider.Models.Add(model);
        provider.RemoveModelCommand.Execute(model);
        Assert.IsEmpty(provider.Models);
    }

    [TestMethod]
    public void MissingModelShouldDisableOnlyChatContainer()
    {
        var model = new MainViewModel();
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            window.UpdateLayout();
            var chat = window.GetVisualDescendants().OfType<ChatView>().Single();
            Assert.IsFalse(chat.IsEffectivelyEnabled);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void MissingModelShouldLeaveArchiveNavigationEnabled()
    {
        var window = new MainWindow { DataContext = new MainViewModel() };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.IsTrue(window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "ArchiveButton").IsEffectivelyEnabled);
        }
        finally { window.Close(); }
    }
}
