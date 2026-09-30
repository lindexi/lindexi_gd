using Avalonia;
using Avalonia.Fonts.Inter;
using Avalonia.Headless;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(CandidateWindowScreenshotCollection))]
public class CandidateWindowScreenshotTests(CandidateWindowScreenshotFixture fixture)
{
    [Fact(Timeout = 10_000)]
    public async Task CandidateWindow_WhenRenderedHeadlesslyThenWritesDesignScreenshot()
    {
        var screenshotPath = GetScreenshotPath("candidate-window.png");

        await fixture.Session.Dispatch(() =>
        {
            var window = new CandidateWindow
            {
                DataContext = CreateState(),
            };
            window.Show();

            using var bitmap = window.CaptureRenderedFrame();
            Assert.NotNull(bitmap);
            Assert.InRange(bitmap.PixelSize.Width, 320, 760);
            Assert.InRange(bitmap.PixelSize.Height, 70, 140);

            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            bitmap.Save(screenshotPath);
            window.Close();
        }, CancellationToken.None);

        Assert.True(new FileInfo(screenshotPath).Length > 0);
    }

    private static CandidateWindowViewState CreateState()
    {
        return new CandidateWindowViewState(
            IsVisible: true,
            CompositionText: "nihao",
            Candidates:
            [
                new CandidateWindowCandidateViewModel(1, 0, "你好", "ni hao", true),
                new CandidateWindowCandidateViewModel(2, 1, "拟好", "ni hao", false),
                new CandidateWindowCandidateViewModel(3, 2, "你号", "ni hao", false),
                new CandidateWindowCandidateViewModel(4, 3, "尼豪", "ni hao", false),
                new CandidateWindowCandidateViewModel(5, 4, "倪浩", "ni hao", false),
            ],
            Selection: 0,
            PageStart: 0,
            PageSize: 5,
            CurrentPage: 1,
            TotalPages: 2,
            GuidelineText: string.Empty,
            AnchorX: 0,
            AnchorY: 0);
    }

    private static string GetScreenshotPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "data", "dictionaries")))
        {
            directory = directory.Parent;
        }

        var projectRoot = directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the XiaoXiIme project root.");
        return Path.Combine(projectRoot, "artifacts", "test-screenshots", fileName);
    }
}

[CollectionDefinition(nameof(CandidateWindowScreenshotCollection), DisableParallelization = true)]
public sealed class CandidateWindowScreenshotCollection : ICollectionFixture<CandidateWindowScreenshotFixture>;

public sealed class CandidateWindowScreenshotFixture : IDisposable
{
    public CandidateWindowScreenshotFixture()
    {
        Session = HeadlessUnitTestSession.StartNew(typeof(CandidateWindowHeadlessProgram));
    }

    public HeadlessUnitTestSession Session { get; }

    public void Dispose()
    {
        Session.Dispose();
    }
}

public static class CandidateWindowHeadlessProgram
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            })
            .WithInterFont()
            .LogToTrace();
    }
}
