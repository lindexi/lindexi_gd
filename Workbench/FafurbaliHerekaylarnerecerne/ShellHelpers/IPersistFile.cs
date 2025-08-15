using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FafurbaliHerekaylarnerecerne;

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