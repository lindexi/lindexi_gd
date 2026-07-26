using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record NativeImeLoadProbeResult(
    string ImePath,
    bool LoadSucceeded,
    int LoadErrorCode,
    string? LoadErrorMessage,
    IReadOnlyDictionary<string, bool> RequiredExports,
    bool AllRequiredExportsFound);

internal static class NativeImeLoadProbe
{
    private const uint LoadLibrarySearchDllLoadDir = 0x00000100;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private static readonly string[] RequiredExports =
    [
        "ImeInquire",
        "ImeConfigure",
        "ImeConversionList",
        "ImeDestroy",
        "ImeEscape",
        "ImeProcessKey",
        "ImeSelect",
        "ImeSetActiveContext",
        "ImeSetCompositionString",
        "ImeToAsciiEx",
        "NotifyIME",
    ];

    public static int Run(NativeImeLoadProbeOptions options, TextWriter output, TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(options.ImeFile))
        {
            error.WriteLine(JsonSerializer.Serialize(new { error = "The native IME load probe requires a file path." }));
            return 2;
        }

        var result = Probe(Path.GetFullPath(options.ImeFile));
        output.WriteLine(JsonSerializer.Serialize(result));
        output.Flush();
        return result.LoadSucceeded && result.AllRequiredExportsFound ? 0 : 1;
    }

    internal static NativeImeLoadProbeResult Probe(string imePath)
    {
        var exports = RequiredExports.ToDictionary(name => name, _ => false, StringComparer.Ordinal);
        var module = LoadLibraryEx(imePath, 0, LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32);
        if (module == 0)
        {
            var errorCode = Marshal.GetLastPInvokeError();
            return new NativeImeLoadProbeResult(
                imePath,
                false,
                errorCode,
                errorCode == 0 ? null : new Win32Exception(errorCode).Message,
                exports,
                false);
        }

        try
        {
            foreach (var export in RequiredExports)
            {
                exports[export] = GetProcAddress(module, export) != 0;
            }
            return new NativeImeLoadProbeResult(imePath, true, 0, null, exports, exports.Values.All(found => found));
        }
        finally
        {
            FreeLibrary(module);
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string fileName, nint file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern nint GetProcAddress(nint module, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(nint module);
}