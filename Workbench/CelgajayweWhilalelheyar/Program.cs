// See https://aka.ms/new-console-template for more information

using Microsoft.Win32.SafeHandles;

using System.IO.Pipes;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

if (!OperatingSystem.IsWindows())
{
    return;
}

Guid GUID_DEVCLASS_MONITOR =
new Guid(0x4d36e96e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);

SetupDiDestroyDeviceInfoListSafeHandle safeHandle =
    SetupDiGetClassDevs(GUID_DEVCLASS_MONITOR,null,new HWND(0),SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT| SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_DEVICEINTERFACE);
bool Success = true;
uint i = 0;
while (Success)
{
    SpDeviceInterfaceData dia = new SpDeviceInterfaceData();
    dia.CbSize = (uint) Marshal.SizeOf(typeof(SpDeviceInterfaceData));
    Success = APICalls.SetupDiEnumDeviceInterfaces(safeHandle.DangerousGetHandle(), IntPtr.Zero, ref GUID_DEVCLASS_MONITOR, i,
        ref dia);
    var lastWin32Error = Marshal.GetLastWin32Error();
}

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct SpDeviceInterfaceDetailData
{
    public int CbSize;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string DevicePath;
}

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
internal struct SpDeviceInterfaceData
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    public uint CbSize;
    public Guid InterfaceClassGuid;
    public uint Flags;
    public IntPtr Reserved;
}
static class APICalls
{
    #region Constants
    public const int DigcfDeviceinterface = 16;
    public const int DigcfPresent = 2;
    public const uint FileShareRead = 1;
    public const uint FileShareWrite = 2;

    public const uint OpenExisting = 3;
    public const int FileAttributeNormal = 128;
    public const int FileFlagOverlapped = 1073741824;

    public const int ERROR_NO_MORE_ITEMS = 259;

    public const int PURGE_TXCLEAR = 0x0004;
    public const int PURGE_RXCLEAR = 0x0008;
    #endregion

    #region Methods

    //#region Kernel32
    //[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    //public static extern SafeFileHandle CreateFile(string lpFileName, FileAccessRights dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
    //#endregion

    #region SetupAPI
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/desktop/api/setupapi/nf-setupapi-setupdienumdeviceinterfaces
    /// </summary>
    [DllImport(@"setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

    [DllImport(@"setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SpDeviceInterfaceData deviceInterfaceData, ref SpDeviceInterfaceDetailData deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, ref SpDeviceInfoData deviceInfoData);
    #endregion

    #endregion
}

[StructLayout(LayoutKind.Sequential)]
internal struct SpDeviceInfoData
{
    public uint CbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public IntPtr Reserved;
}