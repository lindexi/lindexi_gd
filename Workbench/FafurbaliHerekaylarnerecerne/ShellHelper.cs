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

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("000214F9-0000-0000-C000-000000000046")]
[ComImport]
internal unsafe partial interface IShellLinkW
{
    void GetPath(char* pszFile, int cchMaxPath, WIN32_FIND_DATAW* pfd, SLGP fFlags);

    void GetIDList(out IntPtr ppidl);

    void SetIDList(IntPtr pidl);

    void GetDescription(char* pszFile, int cchMaxName);

    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetWorkingDirectory(char* pszDir, int cchMaxPath);

    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    void GetArguments(char* pszArgs, int cchMaxPath);

    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    short GetHotKey();

    void SetHotKey(short wHotKey);

    uint GetShowCmd();

    void SetShowCmd(uint iShowCmd);

    void GetIconLocation(char* pszIconPath, int cchIconPath, out int piIcon);

    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

    void Resolve(IntPtr hwnd, uint fFlags);

    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

[BestFitMapping(false)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct WIN32_FIND_DATAW
{
    public FileAttributes dwFileAttributes;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
    public int nFileSizeHigh;
    public int nFileSizeLow;
    public int dwReserved0;

    public int dwReserved1;

    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public fixed char cFileName[260];

    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public fixed char cAlternateFileName[14];
}

[Flags]
internal enum SLGP
{
    SHORTPATH = 1,
    UNCPRIORITY = 2,
    RAWPATH = 4,
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0000010b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public partial interface IPersistFile
{
    // IPersist portion
    void GetClassID(out Guid pClassID);

    // IPersistFile portion
    [PreserveSig]
    int IsDirty();

    void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
    void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
    void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}