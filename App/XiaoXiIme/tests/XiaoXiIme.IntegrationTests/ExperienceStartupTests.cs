using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(CandidateWindowScreenshotCollection))]
public class ExperienceStartupTests(CandidateWindowScreenshotFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenStartingWithoutProductArgumentsThenCreatesExperienceWindow(bool explicitExperience)
    {
        await fixture.Session.Dispatch(() =>
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = explicitExperience ? ["--experience"] : [] };
            var app = new App
            {
                ApplicationLifetime = lifetime,
                ExperienceSettingsPath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ExperienceStartupTests", Guid.NewGuid().ToString("N"), "settings.json")
            };
            try
            {
                app.OnFrameworkInitializationCompleted();
                Assert.Equal("DictionaryDebugWindow", lifetime.MainWindow?.GetType().Name);
            }
            finally
            {
                lifetime.MainWindow?.Close();

            }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenNonUiWriterOwnsExperienceProfileThenStartupShowsRecoverableFeedback()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ExperienceStartupTests", Guid.NewGuid().ToString("N"), "settings.json");
        using var writer = new XiaoXiIme.ImeHost.ProductSettingsStore(path);
        await fixture.Session.Dispatch(() =>
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            var app = new App { ApplicationLifetime = lifetime, ExperienceSettingsPath = path };
            try
            {
                app.OnFrameworkInitializationCompleted();
                Assert.Equal("ExperienceStartupError", lifetime.MainWindow?.Name);
            }
            finally { lifetime.MainWindow?.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenSameProfileIsOpenedTwiceInProcessThenExperienceWindowIsReused()
    {
        await fixture.Session.Dispatch(() =>
        {
            var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ExperienceStartupTests", Guid.NewGuid().ToString("N"), "settings.json");
            var first = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            var second = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            try
            {
                new App { ApplicationLifetime = first, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
                new App { ApplicationLifetime = second, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
                Assert.Same(first.MainWindow, second.MainWindow);
            }
            finally { first.MainWindow?.Close(); second.MainWindow?.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenDifferentProfilesStartThenWindowsAreIndependent()
    {
        await fixture.Session.Dispatch(() =>
        {
            var first = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            var second = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            try
            {
                new App { ApplicationLifetime = first, ExperienceSettingsPath = NewProfile() }.OnFrameworkInitializationCompleted();
                new App { ApplicationLifetime = second, ExperienceSettingsPath = NewProfile() }.OnFrameworkInitializationCompleted();
                Assert.NotSame(first.MainWindow, second.MainWindow);
            }
            finally { first.MainWindow?.Close(); second.MainWindow?.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenExperienceClosesThenProfileWriterCanReopen()
    {
        var path = NewProfile();
        await fixture.Session.Dispatch(() =>
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            new App { ApplicationLifetime = lifetime, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
            lifetime.MainWindow!.Close();
        }, CancellationToken.None);
        using var writer = new XiaoXiIme.ImeHost.ProductSettingsStore(path);
        Assert.Equal(0, writer.Settings.Revision);
    }

    [Fact]
    public async Task WhenExperienceOwnsProfileThenAnotherStoreCannotWrite()
    {
        var path = NewProfile();
        await fixture.Session.Dispatch(() =>
        {
            var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            try
            {
                new App { ApplicationLifetime = lifetime, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
                Assert.Throws<IOException>(() => new XiaoXiIme.ImeHost.ProductSettingsStore(path));
            }
            finally { lifetime.MainWindow?.Close(); }
        }, CancellationToken.None);
    }

    [Fact]
    public async Task WhenConflictingWriterExitsThenFailedProfileCanStartAgain()
    {
        var path = NewProfile();
        await fixture.Session.Dispatch(() =>
        {
            var failed = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            var reopened = new ClassicDesktopStyleApplicationLifetime { Args = [] };
            try
            {
                using (var writer = new XiaoXiIme.ImeHost.ProductSettingsStore(path))
                {
                    new App { ApplicationLifetime = failed, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
                    failed.MainWindow?.Close();
                }
                new App { ApplicationLifetime = reopened, ExperienceSettingsPath = path }.OnFrameworkInitializationCompleted();
                Assert.Equal("DictionaryDebugWindow", reopened.MainWindow?.GetType().Name);
            }
            finally { failed.MainWindow?.Close(); reopened.MainWindow?.Close(); }
        }, CancellationToken.None);
    }

    private static string NewProfile() => Path.Combine(Path.GetTempPath(), "XiaoXiIme.ExperienceStartupTests", Guid.NewGuid().ToString("N"), "settings.json");

    [Fact]
    public void WhenCwdAndLegacyEnvironmentArePollutedThenDefaultPackageRemainsApplicationRelative()
    {
        var cwd = Environment.CurrentDirectory;
        var names = new[] { "XIAOXIIME_INPUT_SCHEME", "XIAOXIIME_DEBUG_PACKAGE", "XIAOXIIME_DEBUG_SETTINGS_PATH" };
        var previous = names.Select(Environment.GetEnvironmentVariable).ToArray();
        var unrelated = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Experience unrelated cwd", Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            Environment.CurrentDirectory = unrelated;
            Environment.SetEnvironmentVariable(names[0], "xiaoheDoublePinyin");
            Environment.SetEnvironmentVariable(names[1], Path.Combine(unrelated, "wrong package"));
            Environment.SetEnvironmentVariable(names[2], Path.Combine(unrelated, "wrong settings.json"));
            Assert.Equal(Path.Combine(AppContext.BaseDirectory, DictionaryPackageLocations.FullPinyinPackageDirectoryName),
                DictionaryPackageLocations.ResolvePackageDirectory());
        }
        finally
        {
            Environment.CurrentDirectory = cwd;
            for (var index = 0; index < names.Length; index++)
                Environment.SetEnvironmentVariable(names[index], previous[index]);
        }
    }
}
