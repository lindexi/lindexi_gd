using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XiaoXiIme.TsfModule;

public static unsafe class TsfExports
{
    [UnmanagedCallersOnly(EntryPoint = "DllCanUnloadNow", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllCanUnloadNow()
    {
        return TsfUnmanagedBoundary.Invoke(static () =>
            TsfComServer.CanUnload ? TsfHResults.S_OK : TsfHResults.S_FALSE);
    }

    [UnmanagedCallersOnly(EntryPoint = "DllGetClassObject", CallConvs = [typeof(CallConvStdcall)])]
    public static int DllGetClassObject(Guid* classId, Guid* interfaceId, nint* result)
    {
        return TsfUnmanagedBoundary.Invoke(() =>
        {
            if (classId is null || interfaceId is null || result is null)
            {
                return TsfHResults.E_POINTER;
            }

            *result = 0;
            if (*classId != TsfRegistration.ClassId)
            {
                return TsfHResults.CLASS_E_CLASSNOTAVAILABLE;
            }

            return TsfComServer.TryCreateClassFactory(*interfaceId, result);
        });
    }
}
