using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace FafurbaliHerekaylarnerecerne;

internal static partial class ShellHelper
{
    const uint CLSCTX_ALL = 23;
    const int STGM_READ = 0x00000000;

    public static unsafe string GetLinkTargetPath(FileInfo linkFile)
    {
        Guid classIdShellLink = new Guid("00021401-0000-0000-C000-000000000046");
        var riIdIShellLinkW = typeof(IShellLinkW).GUID;

        var hr = CoCreateInstance(
            in classIdShellLink,
            IntPtr.Zero,
            CLSCTX_ALL,
            in riIdIShellLinkW,
            out var ppv);

        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        IShellLinkW shellLink = ComInterfaceMarshaller<IShellLinkW>.ConvertToManaged((void*)ppv)!;

        IPersistFile persistFile = ComInterfaceMarshaller<IPersistFile>.ConvertToManaged((void*)ppv)!;

        persistFile.Load(linkFile.FullName, STGM_READ);

        char[] buffer = new char[260];
        WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();

        fixed (char* pszFile = buffer)
        {
            shellLink.GetPath(pszFile, buffer.Length, &data, SLGP.UNCPRIORITY);
        }

        return new string(buffer);
    }

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(
        in Guid rclsid,
        nint pUnkOuter,
        uint dwClsContext,
        in Guid riid,
        out nint ppv);
}