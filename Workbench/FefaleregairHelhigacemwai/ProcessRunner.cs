using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using Microsoft.Win32.SafeHandles;

using static Windows.Win32.PInvoke;

namespace FefaleregairHelhigacemwai;

public static class ProcessRunner
{
    /// <summary>
    /// 降权启动
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="arguments"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    /// 实现细节请参阅 https://blog.walterlv.com/post/start-process-with-lowered-uac-privileges.html
    [SupportedOSPlatform("windows6.0.6000")] // 绝大部分方法都是 5.x 就可以用的，只有 CreateProcessWithToken 是 6.0.6000 才有的。好在 Windows Vista 就是 6.0 了。而 Win7 都到 6.1 了
    public static unsafe StartProcessWithShellProcessTokenResult StartProcessWithShellProcessToken(string fileName, string? arguments)
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
        {
            var process = Process.Start(new ProcessStartInfo(fileName, arguments ?? string.Empty)
            {
                WorkingDirectory = Path.GetDirectoryName(fileName)
            });

            if (process != null)
            {
                return new StartProcessWithShellProcessTokenResult(true, process.Id, "OK");
            }
            else
            {
                return new(false, -1, "Process.Start return null");
            }
        }

        var shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            return new(false, -1, "GetShellWindow Failed");
        }

        GetWindowThreadProcessId(shellWindow, out var processId);

        if (processId == 0)
        {
            return new(false, -1, "GetWindowThreadProcessId Failed");
        }

        SafeHandle? processHandle = null;
        SafeFileHandle? shellToken = null;
        SafeFileHandle? duplicateToken = null;
        try
        {
            processHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, processId);
            if (processHandle.IsInvalid)
            {
                var errorCode = Marshal.GetLastPInvokeError();

                return new(false, -1, $"OpenProcess Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
            }

            if (!OpenProcessToken(processHandle, (TOKEN_ACCESS_MASK) 0x02000000, out shellToken))
            {
                var errorCode = Marshal.GetLastPInvokeError();

                return new(false, -1, $"OpenProcessToken Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
            }

            if (!DuplicateTokenEx(shellToken, (TOKEN_ACCESS_MASK) 0x02000000, null,
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out duplicateToken))
            {
                var errorCode = Marshal.GetLastPInvokeError();

                return new(false, -1, $"DuplicateTokenEx Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");

            }

            var startupInfo = new STARTUPINFOW
            {
                cb = (uint) Unsafe.SizeOf<STARTUPINFOW>(),
            };
            var commandLine = $@"""{fileName}"" {arguments}";
            Span<char> lpCommandLine = stackalloc char[commandLine.Length + 1];
            commandLine.AsSpan().CopyTo(lpCommandLine);

            var result = CreateProcessWithToken(duplicateToken, 0, null, ref lpCommandLine,
                0, null, Path.GetDirectoryName(fileName)!, in startupInfo, out var information);
            if (!result)
            {
                var errorCode = Marshal.GetLastPInvokeError();

                return new(false, -1, $"CreateProcessWithTokenW Failed {errorCode}: {Marshal.GetPInvokeErrorMessage(errorCode)}");
            }
            else
            {
                CloseHandle(information.hProcess);
                CloseHandle(information.hThread);
            }

            return new(true, (int) information.dwProcessId, "OK");
        }
        finally
        {
            if (processHandle?.IsInvalid is false)
            {
                CloseHandle((HANDLE) processHandle.DangerousGetHandle());
            }

            if (shellToken?.IsInvalid is false)
            {
                CloseHandle((HANDLE) shellToken.DangerousGetHandle());
            }

            if (duplicateToken?.IsInvalid is false)
            {
                CloseHandle((HANDLE) duplicateToken.DangerousGetHandle());
            }
        }
    }
}

public readonly record struct StartProcessWithShellProcessTokenResult(bool Success, int ProcessId, string? ErrorMessage);