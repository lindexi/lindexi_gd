using System.Runtime.InteropServices;

namespace FafurbaliHerekaylarnerecerne;

[BestFitMapping(false)]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public unsafe struct WIN32_FIND_DATAW
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