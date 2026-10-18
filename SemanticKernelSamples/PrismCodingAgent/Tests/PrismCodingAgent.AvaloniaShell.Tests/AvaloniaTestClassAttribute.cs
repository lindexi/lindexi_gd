using Avalonia;
using Avalonia.Headless;

namespace PrismCodingAgent.AvaloniaShell.Tests;

internal sealed class AvaloniaTestClassAttribute : TestClassAttribute
{
    private static HeadlessUnitTestSession? _session;

    internal static HeadlessUnitTestSession Session => _session ??= HeadlessUnitTestSession.StartNew(typeof(TestApplication));

    public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
        => new UiTestMethodAttribute(testMethodAttribute);

    private sealed class UiTestMethodAttribute(TestMethodAttribute inner) : TestMethodAttribute
    {
        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
            => Session.Dispatch(() => inner.ExecuteAsync(testMethod), CancellationToken.None);
    }

    private sealed class TestApplication
    {
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont();
    }
}
