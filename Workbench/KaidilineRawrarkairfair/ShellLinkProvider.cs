using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace KaidilineRawrarkairfair;

internal static class ShellLinkProvider
{
    public static unsafe IShellLinkW* CreateShellLink()
    {
        IShellLinkW* link = CreateCom<IShellLinkW>(CLSID_IShellLinkW);
        return link;
    }

    private static readonly Guid CLSID_IShellLinkW = new Guid("00021401-0000-0000-C000-000000000046");

    private static unsafe T* CreateCom<T>(in Guid clsid)
        where T : unmanaged
    {
        int hr = PInvoke.CoCreateInstance<T>(in clsid, /* No aggregation */ null, CLSCTX.CLSCTX_INPROC_SERVER, out var ptr);
        Marshal.ThrowExceptionForHR(hr);

        return ptr;
    }
}
