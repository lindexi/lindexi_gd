using System;

namespace WhefallralajaHubeanerelair;

internal static class ComObjectCreator
{
    public static object CreateInstanceLicense(Guid clsid, Guid iid, string licenseKey)
    {
        object ppvObj = (object) null;
        Guid guid = typeof(UnsafeNativeMethods.IClassFactory2).GUID;
        var classFactory2 = UnsafeNativeMethods.CoGetClassObject(ref clsid, 1U, IntPtr.Zero, ref guid);

        classFactory2.CreateInstanceLic(IntPtr.Zero, IntPtr.Zero, ref iid, licenseKey, out ppvObj);
        return ppvObj;
    }
}