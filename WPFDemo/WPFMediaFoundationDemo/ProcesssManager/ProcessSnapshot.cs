using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.ProcessStatus;
using Windows.Win32.System.Threading;

namespace ProcesssManager;

internal sealed record ProcessSnapshot(uint ProcessID, long PageFileUsage, long WorkingSetSize, long PagedPoolUsage, long NonPagedPoolUsage)
{
    internal static unsafe List<ProcessSnapshot> Capture()
    {
        uint[] ids = new uint[256];
        uint needed;
        while (true)
        {
            fixed (uint* data = ids)
            {
                if (!PInvoke.EnumProcesses(data, (uint)(ids.Length * sizeof(uint)), &needed))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            if (needed < ids.Length * sizeof(uint)) break;
            ids = new uint[checked(ids.Length * 2)];
        }
        var result = new List<ProcessSnapshot>();
        for (int i = 0; i < needed / sizeof(uint); i++)
        {
            uint id = ids[i];
            if (id == 0) continue;
            var nativeHandle = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, false, id);
            using var handle = new Microsoft.Win32.SafeHandles.SafeProcessHandle((nint)nativeHandle.Value, ownsHandle: true);
            if (handle.IsInvalid)
            {
                // Protected processes and processes exiting during enumeration are expected.
                int error = Marshal.GetLastPInvokeError();
                if (error is 5 or 87) { Console.Error.WriteLine($"OpenProcess {id}: {error}"); continue; }
                throw new Win32Exception(error);
            }
            PROCESS_MEMORY_COUNTERS_EX counters = default;
            counters.cb = (uint)sizeof(PROCESS_MEMORY_COUNTERS_EX);
            if (!PInvoke.GetProcessMemoryInfo(nativeHandle, (PROCESS_MEMORY_COUNTERS*)&counters, counters.cb))
            {
                int error = Marshal.GetLastPInvokeError();
                if (error is 5 or 6 or 87 or 299) { Console.Error.WriteLine($"GetProcessMemoryInfo {id}: {error}"); continue; }
                throw new Win32Exception(error);
            }
            result.Add(new(id, (long)counters.PagefileUsage, (long)counters.WorkingSetSize, (long)counters.QuotaPagedPoolUsage, (long)counters.QuotaNonPagedPoolUsage));
        }
        return result;
    }
}
