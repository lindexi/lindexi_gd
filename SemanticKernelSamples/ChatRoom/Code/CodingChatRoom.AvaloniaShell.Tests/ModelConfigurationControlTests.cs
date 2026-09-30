using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ModelConfigurationControlTests
{
    [TestMethod]
    public void ConnectionFieldsShouldShareOneRow()
    {
        var view = new ModelServiceConnectionView { DataContext = new ProviderSettingsViewModel() };
        var window = new Window { Content = view, Width = 720, Height = 300 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var inputs = view.GetVisualDescendants().OfType<TextBox>().ToArray();
            var first = inputs[0].TranslatePoint(default, view) ?? throw new InvalidOperationException();
            var second = inputs[1].TranslatePoint(default, view) ?? throw new InvalidOperationException();
            Assert.AreEqual(first.Y, second.Y, 1);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void SettingsCatalogShouldHaveEnabledFetchCommand()
    {
        var view = new OpenAIModelCatalogView { DataContext = new ProviderSettingsViewModel { EndPoint = "https://example.com/v1" } };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            var button = view.GetVisualDescendants().OfType<Button>().First();
            Assert.IsTrue(button.Command is not null && button.IsEnabled);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    public void CatalogFetchButtonShouldBindToCatalogCommand()
    {
        using var catalog = new ModelCatalogViewModel(new ProviderSettingsViewModel());
        var view = new OpenAIModelCatalogView { DataContext = catalog };
        var window = new Window { Content = view };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.AreSame(catalog.LoadCommand, view.GetVisualDescendants().OfType<Button>().First().Command);
        }
        finally { window.Close(); }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ConnectionInputTextShouldBeVerticallyCentered(bool isPassword)
    {
        var view = new ModelServiceConnectionView
        {
            DataContext = new ProviderSettingsViewModel { EndPoint = "https://example.com/v1", ApiKey = "test-key" },
        };
        var window = new Window { Content = view, Width = 720, Height = 300 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var input = view.GetVisualDescendants().OfType<TextBox>().Single(box => (box.PasswordChar != default(char)) == isPassword);
            var presenter = input.GetVisualDescendants().OfType<TextPresenter>().Single();
            var position = presenter.TranslatePoint(default, input) ?? throw new InvalidOperationException();
            Assert.AreEqual(input.Bounds.Height / 2, position.Y + presenter.Bounds.Height / 2, 1.0);
        }
        finally { window.Close(); }
    }
}
