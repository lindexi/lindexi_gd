using System.Runtime.InteropServices;

namespace JayhallchocojejalNabinojajarher.Devices;

/// <summary>
/// 设备探测器
/// </summary>
public static class DeviceDetector
{
    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint SPDRP_DEVICEDESC = 0x00000000;
    private const uint SPDRP_HARDWAREID = 0x00000001;
    private const uint SPDRP_DRIVER = 0x00000009;
    private const uint ERROR_INSUFFICIENT_BUFFER = 0x7A;
    private static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr) (-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public UIntPtr Reserved;
    }

    [DllImport("SetupAPI.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetClassDevsW", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs([In] in Guid ClassGuid, [In] string Enumerator, [In] IntPtr hwndParent, [In] uint Flags);

    [DllImport("SetupAPI.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiEnumDeviceInfo", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo([In] IntPtr DeviceInfoSet, [In] uint MemberIndex, [In][Out] ref SP_DEVINFO_DATA DeviceInfoData);

    [DllImport("SetupAPI.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetDeviceRegistryPropertyW", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceRegistryProperty([In] IntPtr DeviceInfoSet, [In] in SP_DEVINFO_DATA DeviceInfoData,
        [In] uint Property, [Out] out uint PropertyRegDataType, [In] IntPtr PropertyBuffer, [In] uint PropertyBufferSize, [Out] out uint RequiredSize);

    [DllImport("SetupAPI.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiGetDeviceInstanceIdW", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInstanceId([In] IntPtr DeviceInfoSet, [In] in SP_DEVINFO_DATA DeviceInfoData, [In] IntPtr DeviceInstanceId,
        [In] uint DeviceInstanceIdSize, [Out] out uint RequiredSize);

    [DllImport("SetupAPI.dll", CharSet = CharSet.Unicode, EntryPoint = "SetupDiDestroyDeviceInfoList", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList([In] IntPtr DeviceInfoSet);

    /// <summary>
    /// 获取系统当前存在的设备信息
    /// </summary>
    /// <param name="deviceSetupClass">
    /// 设备安装程序类 见 <see cref="DeviceSetupClasses"/>
    /// </param>
    /// <param name="enumerator">
    /// PnP 枚举器 ID， 例如 PCI, USB, PCMCIA, SCSI。
    /// 不需要指定特定 PnP 枚举器时则为 <see langword="null"/>。
    /// </param>
    /// <returns>
    /// 设备信息列表
    /// </returns>
    public static List<DeviceInfo> GetPresentDeviceInfos(Guid deviceSetupClass, string enumerator)
    {
        return GetPresentDeviceInfoEnumerable(deviceSetupClass, enumerator).ToList();
    }

    /// <summary>
    /// 获取系统当前存在的设备信息
    /// </summary>
    /// <param name="deviceSetupClass">
    /// 设备安装程序类 见 <see cref="DeviceSetupClasses"/>
    /// </param>
    /// <param name="enumerator">
    /// PnP 枚举器 ID， 例如 PCI, USB, PCMCIA, SCSI。
    /// 不需要指定特定 PnP 枚举器时则为 <see langword="null"/>。
    /// </param>
    /// <returns>
    /// 设备信息列表
    /// </returns>
    public static IEnumerable<DeviceInfo> GetPresentDeviceInfoEnumerable(Guid deviceSetupClass, string enumerator = null)
    {
        var devinfo = SetupDiGetClassDevs(deviceSetupClass, enumerator, IntPtr.Zero, DIGCF_PRESENT);
        if (devinfo != INVALID_HANDLE_VALUE)
        {
            try
            {
                var index = 0u;
                var deviceInfoData = new SP_DEVINFO_DATA { cbSize = (uint) Marshal.SizeOf(typeof(SP_DEVINFO_DATA)) };
                while (SetupDiEnumDeviceInfo(devinfo, index, ref deviceInfoData))
                {
                    yield return new DeviceInfo(GetDescription(devinfo, deviceInfoData), GetDeviceInstanceId(devinfo, deviceInfoData),
                        GetDriverKey(devinfo, deviceInfoData), GetHardwareIds(devinfo, deviceInfoData));
                    index++;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(devinfo);
            }
        }
    }

    private static unsafe string GetDescription(IntPtr devinfo, in SP_DEVINFO_DATA deviceInfoData)
    {
        SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_DEVICEDESC, out _, IntPtr.Zero, 0, out var size);
        var status = Marshal.GetLastWin32Error();
        if (status == ERROR_INSUFFICIENT_BUFFER)
        {
            var buffer = stackalloc byte[(int) size];
            if (SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_DEVICEDESC, out _, (IntPtr) buffer, size, out _))
            {
                return new string((char*) buffer);
            }
        }
        return string.Empty;
    }

    private static unsafe string GetDeviceInstanceId(IntPtr devinfo, in SP_DEVINFO_DATA deviceInfoData)
    {
        SetupDiGetDeviceInstanceId(devinfo, deviceInfoData, IntPtr.Zero, 0, out var size);
        var status = Marshal.GetLastWin32Error();
        if (status == ERROR_INSUFFICIENT_BUFFER)
        {
            var buffer = stackalloc char[(int) size];
            if (SetupDiGetDeviceInstanceId(devinfo, deviceInfoData, (IntPtr) buffer, size, out _))
            {
                return new string(buffer);
            }
        }
        return string.Empty;
    }

    private static unsafe IList<string> GetHardwareIds(IntPtr devinfo, in SP_DEVINFO_DATA deviceInfoData)
    {
        SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_HARDWAREID, out _, IntPtr.Zero, 0, out var size);
        var status = Marshal.GetLastWin32Error();
        var result = new List<string>();
        if (status == ERROR_INSUFFICIENT_BUFFER)
        {
            var buffer = stackalloc byte[(int) size];
            if (SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_HARDWAREID, out _, (IntPtr) buffer, size, out _))
            {
                var length = 0;
                while (*(char*) buffer != '\0' && size / sizeof(char) >= length)
                {
                    var s = new string((char*) buffer);
                    length += s.Length + 1;
                    buffer += (s.Length + 1) * sizeof(char);
                    result.Add(s);
                }
            }
        }
        return result;
    }

    private static unsafe string GetDriverKey(IntPtr devinfo, in SP_DEVINFO_DATA deviceInfoData)
    {
        SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_DRIVER, out _, IntPtr.Zero, 0, out var size);
        var status = Marshal.GetLastWin32Error();
        if (status == ERROR_INSUFFICIENT_BUFFER)
        {
            var buffer = stackalloc byte[(int) size];
            if (SetupDiGetDeviceRegistryProperty(devinfo, deviceInfoData, SPDRP_DRIVER, out _, (IntPtr) buffer, size, out _))
            {
                return new string((char*) buffer);
            }
        }
        return string.Empty;
    }
}
