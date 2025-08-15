using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

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