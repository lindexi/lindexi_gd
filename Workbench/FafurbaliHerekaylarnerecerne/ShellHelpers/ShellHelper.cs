using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;


namespace FafurbaliHerekaylarnerecerne;

public unsafe class ShellLinkComObject
{
    public ShellLinkComObject()
    {
        Guid classIdShellLink = new Guid("00021401-0000-0000-C000-000000000046");
        var riIdIShellLinkW = typeof(IShellLinkW).GUID;

        var hr = PInvoke.CoCreateInstance(
            in classIdShellLink,
            IntPtr.Zero,
            PInvoke.CLSCTX_ALL,
            in riIdIShellLinkW,
            out var ppv);

        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        Ppv = ppv;
    }

    public IntPtr Ppv { get; }

    public IShellLinkW AsIShellLinkW()=>As<IShellLinkW>()!;
    public IPersistFile AsIPersistFile() => As<IPersistFile>()!;

    public T? As<T>() => ComInterfaceMarshaller<T>.ConvertToManaged((void*) Ppv);
}

internal static class PInvoke
{
    public const uint CLSCTX_ALL = 23;

    public const int STGM_READ = 0x00000000;

    [DllImport("ole32.dll")]
    internal static extern int CoCreateInstance(
        in Guid rclsid,
        nint pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        out nint ppv);
}

public static partial class ShellHelper
{
    const int STGM_READ = 0x00000000;

    public static unsafe string GetLinkTargetPath(FileInfo linkFile)
    {
        var shellLinkComObject = new ShellLinkComObject();

        IShellLinkW shellLink = shellLinkComObject.AsIShellLinkW();

        IPersistFile persistFile = shellLinkComObject.AsIPersistFile();

        persistFile.Load(linkFile.FullName, STGM_READ);

        char[] buffer = new char[260];
        WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();

        fixed (char* pszFile = buffer)
        {
            shellLink.GetPath(pszFile, buffer.Length, &data, SLGP.UNCPRIORITY);
        }

        return new string(buffer);
    }
}
