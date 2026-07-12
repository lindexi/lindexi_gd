using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace XiaoXiIme.Cli;

internal sealed class WindowsImeInstaller : IImeInstaller
{
    public ImeInstallationResult Install(string imeFilePath, string displayName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ImeInstallationResult(false, "IME installation is supported only on Windows.");
        }

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            return new ImeInstallationResult(false, "IME installation requires an elevated administrator process.");
        }

        var keyboardLayout = ImmInstallIME(imeFilePath, displayName);
        if (keyboardLayout == 0)
        {
            var errorCode = Marshal.GetLastPInvokeError();
            var errorMessage = errorCode == 0
                ? "Windows did not provide an error code. Verify the IME exports, architecture, signature, and file location."
                : new Win32Exception(errorCode).Message;
            return new ImeInstallationResult(false, $"ImmInstallIME failed with Win32 error {errorCode}: {errorMessage}");
        }

        return new ImeInstallationResult(true, $"XiaoXi IME installed. HKL/layout id: 0x{unchecked((ulong) keyboardLayout):X}");
    }

    [DllImport("imm32.dll", EntryPoint = "ImmInstallIMEW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint ImmInstallIME(string imeFileName, string layoutText);
}
