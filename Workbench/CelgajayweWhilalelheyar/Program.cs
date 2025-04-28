// See https://aka.ms/new-console-template for more information

using Microsoft.Win32.SafeHandles;

using System.IO.Pipes;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

if (!OperatingSystem.IsWindows())
{
    return;
}

if (!OperatingSystem.IsWindowsVersionAtLeast(5))
{
    return;
}

DISPLAY_DEVICEW displayDevice = default;
displayDevice.cb = (uint) Marshal.SizeOf(typeof(DISPLAY_DEVICEW));

for (uint id = 0; EnumDisplayDevices(null, id, ref displayDevice, 0); id++)
{
    var deviceName = displayDevice.DeviceName.ToString();
    var deviceString = displayDevice.DeviceString.ToString();
    var deviceKey = displayDevice.DeviceKey.ToString();
    var deviceId = displayDevice.DeviceID.ToString();

    Console.WriteLine($"EnumDisplayDevices");
    Console.WriteLine($"DeviceName={deviceName}");
    Console.WriteLine($"DeviceString={deviceString}");
    Console.WriteLine($"DeviceKey={deviceKey}");
    Console.WriteLine($"DeviceID={deviceId}");
    Console.WriteLine();
}

Guid GUID_DEVCLASS_MONITOR =
new Guid(0xe6f07b5f, 0xee97, 0x4a90, 0xb0, 0x76, 0x33, 0xf5, 0x7b, 0xf4, 0xea, 0xa7);

SetupDiDestroyDeviceInfoListSafeHandle safeHandle =
    SetupDiGetClassDevs(GUID_DEVCLASS_MONITOR, null, new HWND(0), SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_DEVICEINTERFACE);
var hDevice = safeHandle.DangerousGetHandle();

bool Success = true;
uint i = 0;
while (Success)
{
    SP_DEVICE_INTERFACE_DATA dia = new SP_DEVICE_INTERFACE_DATA();
    dia.CbSize = (uint) Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
    Success = APICalls.SetupDiEnumDeviceInterfaces(hDevice, IntPtr.Zero, ref GUID_DEVCLASS_MONITOR, i,
        ref dia);
    var lastWin32Error = Marshal.GetLastWin32Error();

    if (Success)
    {
        var da = new SP_DEVINFO_DATA();
        da.cbSize = (uint) Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

        var didd = new SP_DEVICE_INTERFACE_DETAIL_DATA();
        didd.CbSize = 4 + Marshal.SystemDefaultCharSize;

        //Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));

        uint nRequiredSize = 0;
        uint nBytes = 256;

        if (APICalls.SetupDiGetDeviceInterfaceDetail(hDevice, ref dia, ref didd, nBytes, out nRequiredSize, ref da))
        {

        }

        lastWin32Error = Marshal.GetLastWin32Error();

        unsafe
        {
            var p = &didd.DevicePath;
            var byteOfUnicode16 = 2;
            var length = (int) nRequiredSize / byteOfUnicode16 - didd.CbSize/byteOfUnicode16;
            var path = Marshal.PtrToStringUni(new IntPtr(p), length);
            Console.WriteLine(path);
        }

    }
}

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct SP_DEVINFO_DATA
{
    public uint cbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public UIntPtr Reserved;
}


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
{
    public int CbSize;
    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public char DevicePath;
}

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
internal struct SP_DEVICE_INTERFACE_DATA
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
    public static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

    [DllImport(@"setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, ref SP_DEVINFO_DATA deviceInfoData);
    #endregion

    #endregion
}

