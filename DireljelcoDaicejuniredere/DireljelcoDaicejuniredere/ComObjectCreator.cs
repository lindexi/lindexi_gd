namespace DireljelcoDaicejuniredere;

internal static class ComObjectCreator
{
    public static object CreateInstanceLicense(Guid clsid, Guid iid, string licenseKey)
    {
        object ppvObj = (object) null;
        Guid guid = typeof(UnsafeNativeMethods.IClassFactory2).GUID;
        UnsafeNativeMethods.CoGetClassObject(ref clsid, 1U, IntPtr.Zero, ref guid).CreateInstanceLic(IntPtr.Zero, IntPtr.Zero, ref iid, licenseKey, out ppvObj);
        return ppvObj;
    }
}