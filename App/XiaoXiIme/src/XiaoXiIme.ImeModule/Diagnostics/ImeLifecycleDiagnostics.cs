using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static unsafe class ImeLifecycleDiagnostics
{
    private static readonly Lock SyncRoot = new();

    // The failure reporter itself must never throw back into the native caller.
    internal static void RecordBoundaryFailure(string source, Exception exception) => RecordFailure(source, exception);

    internal static void RecordFailure(string source, Exception? exception = null, int errorCode = 0)
    {
        try
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Path.IsPathFullyQualified(localData))
            {
                return;
            }

            var directory = Path.Combine(localData, "XiaoXiIme", "Logs");
            Directory.CreateDirectory(directory);
            lock (SyncRoot)
            {
                using var stream = new FileStream(Path.Combine(directory, "ime-boundary.log"), FileMode.Append,
                    FileAccess.Write, FileShare.Read);
                if (stream.Length >= 256 * 1024)
                {
                    return;
                }

                using var writer = new StreamWriter(stream);
                writer.WriteLine($"{DateTimeOffset.UtcNow:O} PID={Environment.ProcessId} {source} " +
                                 $"{exception?.GetType().FullName} Code=0x{(exception?.HResult ?? errorCode):X8}\n{exception?.StackTrace}");
            }
        }
        catch (Exception)
        {
            // Do not report logger failures through another potentially throwing sink.
        }
    }

    internal static void Record(string source, nint himc, int parameter, string phase)
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("XIAOXIIME_LIFECYCLE_LOG");
            if (string.IsNullOrWhiteSpace(path) || !OperatingSystem.IsWindows())
            {
                return;
            }

            nint hwnd = 0;
            uint compositionLength = 0;
            var context = (InputContext*)Imm32Methods.ImmLockIMC(new HImc(himc));
            if (context != null)
            {
                try
                {
                    hwnd = context->Hwnd.Value;
                    var composition = (CompositionString*)Imm32Methods.ImmLockIMCC(context->HCompStr);
                    if (composition != null)
                    {
                        try
                        {
                            compositionLength = composition->CompStrLength;
                        }
                        finally
                        {
                            Imm32Methods.ImmUnlockIMCC(context->HCompStr);
                        }
                    }
                }
                finally
                {
                    Imm32Methods.ImmUnlockIMC(new HImc(himc));
                }
            }

            var session = ImeInputIdentity.Resolve(new HImc(himc));
            var snapshot = ImeModuleRuntime.GetSnapshotForTesting(session);
            var line =
                $"{DateTimeOffset.UtcNow:O}|{source}|{phase}|parameter={parameter}|himc=0x{himc:X}|hwnd=0x{hwnd:X}|focus=0x{GetFocus():X}|foreground=0x{GetForegroundWindow():X}|session={session}|immLength={compositionLength}|cachedComposing={snapshot.IsComposing}|cachedReadingLength={snapshot.Composition.Reading.Length}\n";
            lock (SyncRoot)
            {
                File.AppendAllText(path, line);
            }
        }
        // Optional diagnostics must not turn an expected environment or file failure into an export failure.
        catch (Exception)
        {
            // Diagnostics must not change the result of an IMM32 operation.
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}