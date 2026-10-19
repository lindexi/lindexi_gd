using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PrismCodingAgent.AvaloniaShell.Localizations;
using PrismCodingAgent.AvaloniaShell.ViewModels;
using PrismCodingAgent.AvaloniaShell.Views;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class LocalizationTests
{
    private string _originalLanguage = "zh-Hans";

    [TestInitialize]
    public void RememberLanguage() => _originalLanguage = Lang.Current.IetfLanguageTag;

    [TestCleanup]
    public Task RestoreLanguageAsync() => Dispatcher.UIThread.InvokeAsync(() =>
    {
        Lang.SetCurrent(_originalLanguage);
        return Task.CompletedTask;
    });

    [TestMethod]
    public void WhenReadingDefaultThenChineseTextIsAvailable()
    {
        Assert.AreEqual("设置", Lang.Default.SettingsNavigationText);
    }

    [TestMethod]
    public void WhenSwitchingToEnglishThenTextChanges()
    {
        Lang.SetCurrent("en");

        Assert.AreEqual("Settings", Lang.Current.SettingsNavigationText);
    }

    [TestMethod]
    public void WhenSwitchingLanguageThenCurrentObjectRemainsStable()
    {
        var current = Lang.Current;
        Lang.SetCurrent("en");

        Assert.AreSame(current, Lang.Current);
    }

    [TestMethod]
    public void WhenSwitchingLanguageThenCurrentNotifiesBindings()
    {
        Lang.SetCurrent("zh-Hans");
        var current = (INotifyPropertyChanged)Lang.Current;
        int notifications = 0;
        PropertyChangedEventHandler handler = (_, _) => notifications++;
        current.PropertyChanged += handler;
        try
        {
            Lang.SetCurrent("en");

            Assert.IsGreaterThan(0, notifications);
        }
        finally
        {
            current.PropertyChanged -= handler;
        }
    }

    [TestMethod]
    public void WhenSwitchingLanguageThenExistingSettingsViewUpdates()
    {
        Lang.SetCurrent("zh-Hans");
        var window = new Window { Content = new SettingsView { DataContext = new SettingsViewModel() } };
        window.Show();
        try
        {
            var title = window.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "设置");

            Lang.SetCurrent("en");
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("Settings", title.Text);
        }
        finally
        {
            window.Close();
        }
    }
}
