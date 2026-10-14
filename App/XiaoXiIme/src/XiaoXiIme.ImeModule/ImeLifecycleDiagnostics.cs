using System.Runtime.InteropServices;
using System.Security;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static unsafe class ImeLifecycleDiagnostics
{
    private static readonly Lock SyncRoot = new();

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

            var session = ImeSessionId.FromHimc(himc);
            var snapshot = ImeModuleRuntime.GetSnapshotForTesting(session);
            var line = $"{DateTimeOffset.UtcNow:O}|{source}|{phase}|parameter={parameter}|himc=0x{himc:X}|hwnd=0x{hwnd:X}|focus=0x{GetFocus():X}|foreground=0x{GetForegroundWindow():X}|session={session}|immLength={compositionLength}|cachedComposing={snapshot.IsComposing}|cachedReadingLength={snapshot.Composition.Reading.Length}\n";
            lock (SyncRoot)
            {
                File.AppendAllText(path, line);
            }
        }
        // Optional diagnostics must not turn an expected environment or file failure into an export failure.
        catch (IOException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (UnauthorizedAccessException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (ArgumentException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (NotSupportedException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (SecurityException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (DllNotFoundException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        catch (EntryPointNotFoundException error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
