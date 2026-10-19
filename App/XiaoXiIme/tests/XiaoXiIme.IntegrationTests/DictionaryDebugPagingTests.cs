using System.Reflection;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(CandidateWindowScreenshotCollection))]
public class DictionaryDebugPagingTests(CandidateWindowScreenshotFixture fixture)
{
    [Theory]
    [InlineData("DictionaryDebugPreviousPage")]
    [InlineData("DictionaryDebugNextPage")]
    public async Task WhenDebugWindowIsCreatedThenPagingControlHasStableAutomationId(string name)
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            try
            {
                Assert.Equal(name, AutomationProperties.GetAutomationId(Button(window, name)));
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, false, true)]
    [InlineData(2, true, false)]
    public async Task WhenPageIsAtBoundaryThenOnlyValidPagingControlsAreEnabled(int page, bool previous, bool next)
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            try
            {
                if (page > 0)
                {
                    var session = CreateSession();
                    session.ProcessKey(Key.N);
                    if (page == 2) session.ProcessKey(Key.PageDown);
                    Invoke(window, "PublishSession", session);
                }
                Assert.Equal((previous, next), (Button(window, "DictionaryDebugPreviousPage").IsEnabled, Button(window, "DictionaryDebugNextPage").IsEnabled));
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenPagingControlsAreClickedThenSessionPagesAndInputCaretOutputArePreserved()
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            window.Show();
            try
            {
                var session = CreateSession();
                session.ProcessKey(Key.N);
                session.ProcessKey(Key.Enter);
                session.ProcessKey(Key.N);
                session.ProcessKey(Key.Left);
                Invoke(window, "PublishSession", session);
                var output = window.GetLogicalDescendants().OfType<TextBox>().Single(c => c.Name == "DebugOutput");
                output.Text = session.CommittedText;
                output.CaretIndex = 0;
                Button(window, "DictionaryDebugNextPage").RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
                var nextPage = session.Candidates.CurrentPage;
                Button(window, "DictionaryDebugPreviousPage").RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
                var caret = typeof(DictionaryDebugSession).GetProperty("CompositionCaret", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session);
                Assert.Equal((2, 1, "n", 0, "n", 0, true, false), (nextPage, session.Candidates.CurrentPage, session.Candidates.CompositionText, (int)caret!, session.CommittedText, output.CaretIndex, output.IsKeyboardFocusWithin, Button(window, "DictionaryDebugNextPage").Focusable));
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenSettingsAreActiveThenPagingIsDisabledAndResumeRestoresBoundaryState()
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            window.Show();
            try
            {
                var session = CreateSession();
                session.ProcessKey(Key.N);
                Invoke(window, "PublishSession", session);
                Invoke(window, "SuspendSettings");
                var suspended = Button(window, "DictionaryDebugNextPage").IsEnabled;
                Button(window, "DictionaryDebugNextPage").RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
                Invoke(window, "ResumeSettings");
                Assert.Equal((false, 1, true), (suspended, session.Candidates.CurrentPage, Button(window, "DictionaryDebugNextPage").IsEnabled));
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenSettingsSuspendInputThenTypingCannotChangeComposition()
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            window.Show();
            try
            {
                var session = CreateSession();
                session.ProcessKey(Key.N);
                Invoke(window, "PublishSession", session);
                Invoke(window, "SuspendSettings");
                window.RaiseEvent(new KeyEventArgs { RoutedEvent = global::Avalonia.Input.InputElement.KeyDownEvent, Key = Key.I });
                Assert.Equal("n", session.Candidates.CompositionText);
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenMainWindowClosesThenSettingsResumeDoesNotReopenCandidates()
    {
        await fixture.Session.Dispatch(() =>
        {
            var window = CreateWindow();
            window.Show();
            var session = CreateSession();
            session.ProcessKey(Key.N);
            Invoke(window, "PublishSession", session);
            Invoke(window, "SuspendSettings");
            var candidates = (Window)window.GetType().GetField("candidates", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(window)!;
            window.Close();
            Invoke(window, "ResumeSettings");
            Assert.False(candidates.IsVisible);
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenSettingsCloseDuringPreparationThenInputRemainsSuspendedUntilCommit()
    {
        await fixture.Session.Dispatch<bool>(async () =>
        {
            var window = CreateWindow();
            window.Show();
            try
            {
                var session = CreateSession();
                session.ProcessKey(Key.N);
                Invoke(window, "PublishSession", session);
                var (settingsWindow, save) = BeginSettingsSave(window);
                settingsWindow.Close();
                window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.I });
                var readingDuringPreparation = session.Candidates.CompositionText;
                await save;
                Assert.Equal("n", readingDuringPreparation);
                return true;
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenMainClosesDuringPreparationThenOwnedSettingsCloseAndSaveIsCancelled()
    {
        await fixture.Session.Dispatch<bool>(async () =>
        {
            var window = CreateWindow();
            window.Show();
            try
            {
                var session = CreateSession();
                session.ProcessKey(Key.N);
                Invoke(window, "PublishSession", session);
                var (settingsWindow, save) = BeginSettingsSave(window);
                window.Close();
                var saved = await save;
                Assert.Equal((false, false), (settingsWindow.IsVisible, saved));
                return true;
            }
            finally { window.Close(); }
        }, CancellationToken.None);
    }

    private static (Window Window, Task<bool> Save) BeginSettingsSave(Window owner)
    {
        var settings = owner.GetType().GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
        var type = settings.GetType();
        type.GetMethod("SetScheme", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings, ["fullPinyin"]);
        Button(owner, "DictionaryDebugSettings").RaiseEvent(new RoutedEventArgs(global::Avalonia.Controls.Button.ClickEvent));
        var target = (Window)type.GetField("window", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(settings)!;
        var baseline = (XiaoXiIme.ImeIpc.ImeProductSettingsResponse)type.GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings, null)!;
        var save = (Task<bool>)type.GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings,
            [baseline, baseline.Settings with { FuzzyRuleMask = 5 }, target])!;
        return (target, save);
    }

    private static Window CreateWindow() => (Window)Activator.CreateInstance(
        typeof(App).Assembly.GetType("XiaoXiIme.ImeUi.Avalonia.DictionaryDebugWindow")!,
        BindingFlags.NonPublic | BindingFlags.Instance, null,
        [Path.Combine(Path.GetTempPath(), "DebugPaging", Guid.NewGuid().ToString("N"), "settings.json"), false, null], null)!;

    private static Button Button(Window window, string name) => window.GetLogicalDescendants().OfType<Button>().Single(b => b.Name == name);

    private static void Invoke(Window window, string name, params object[] args) =>
        window.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, args);

    private static DictionaryDebugSession CreateSession()
    {
        var path = Path.Combine(Path.GetTempPath(), "DebugPagingPackage", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(Enumerable.Range(1, 12).Select(i => new PhoneticDictionaryEntry("candidate" + i, "ni", 100 - i)), path);
        return new DictionaryDebugSession(DictionaryPackageLoader.Load(path));
    }
}
