using System.Runtime.InteropServices;

namespace XiaoXiIme.SandboxRunner;

internal static unsafe class NativeImeVirtualKeyProbe
{
    private const uint VirtualKeyWithCharacter = 0x00780058;

    public static int Run(string? imePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("The native IME virtual-key probe requires Windows.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(imePath))
        {
            Console.Error.WriteLine("native-ime-vkey-probe requires the path to XiaoXiIme.ime.");
            return 2;
        }

        try
        {
            var resolvedImePath = Path.IsPathRooted(imePath)
                ? Path.GetFullPath(imePath)
                : Path.GetFullPath(imePath, AppContext.BaseDirectory);
            var module = NativeLibrary.Load(resolvedImePath);
            var reset = (delegate* unmanaged[Stdcall]<void>)NativeLibrary.GetExport(module, "XiaoXiImeResetKeystrokeDiagnostics");
            var getSnapshot = (delegate* unmanaged[Stdcall]<ImeKeystrokeDiagnosticSnapshot*, uint, int>)NativeLibrary.GetExport(module, "XiaoXiImeGetKeystrokeDiagnostics");
            var imeToAsciiEx = (delegate* unmanaged[Stdcall]<uint, uint, byte*, nint, uint, nint, uint>)NativeLibrary.GetExport(module, "ImeToAsciiEx");

            reset();
            _ = imeToAsciiEx(VirtualKeyWithCharacter, 0, null, 0, 0, 0);

            ImeKeystrokeDiagnosticSnapshot snapshot = default;
            if (getSnapshot(&snapshot, (uint)sizeof(ImeKeystrokeDiagnosticSnapshot)) == 0)
            {
                throw new InvalidOperationException("The native IME diagnostic export rejected the snapshot buffer.");
            }

            if (snapshot.LastToAsciiVirtualKey != 0x58)
            {
                throw new InvalidOperationException($"Expected normalized virtual key 0x58, but the native export recorded 0x{snapshot.LastToAsciiVirtualKey:X}.");
            }

            Console.WriteLine($"PASS native-ime-vkey-probe: RawVirtualKey=0x{VirtualKeyWithCharacter:X8}, NormalizedVirtualKey=0x{snapshot.LastToAsciiVirtualKey:X}, Calls={snapshot.ImeToAsciiExCallCount}.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImeKeystrokeDiagnosticSnapshot
    {
        public uint Version;
        public uint ImeInquireCallCount;
        public uint ImeSelectCallCount;
        public uint ImeSetActiveContextCallCount;
        public uint NotifyImeCallCount;
        public uint ImeProcessKeyCallCount;
        public uint ImeToAsciiExCallCount;
        public uint LastProcessVirtualKey;
        public uint LastProcessHandled;
        public uint LastToAsciiVirtualKey;
        public uint LastToAsciiHandled;
        public uint LastCompositionWriteSucceeded;
        public uint LastMessageCount;
        public uint LastReturnValue;
        public uint ProcessKeyHandledCount;
        public uint ToAsciiHandledCount;
        public uint LastProcessError;
        public uint LastToAsciiError;
        public uint LastToAsciiStage;
    }
}
