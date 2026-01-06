using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LecewaljemFeachojawhi.EdidReader;

public class RawEdid
{
    public RawEdid(byte[] data)
    {
        _data = data;
    }

    public byte[] GetRawData() => _data;

    private readonly byte[] _data;

    public EdidDetail GetDetail()
    {
        return EdidDetail.Parse(_data);
    }

    public override string ToString()
    {
        var i = 0;
        var builder = new StringBuilder();
        foreach (var b in _data)
        {
            builder.Append(b.ToString("X2")).Append(" ");
            i++;
            if (i % 16 == 0)
            {
                builder.AppendLine();
            }
        }
        return builder.ToString();
    }

    public static RawEdid[] Get()
    {
        return EdidUtil.GetEDID();
    }

    private static class EdidUtil
    {
        #region EDID Method

        public static RawEdid[] GetEDID()
        {
            var lsi = new List<RawEdid>();
            var pGuid = Marshal.AllocHGlobal(Marshal.SizeOf(GUID_CLASS_MONITOR));
            Marshal.StructureToPtr(GUID_CLASS_MONITOR, pGuid, false);
            var hDevInfo = SetupDiGetClassDevsEx(
                pGuid,
                null,
                nint.Zero,
                DIGCF_PRESENT,
                nint.Zero,
                null,
                nint.Zero);

            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            uint dev = 0;

            string DeviceID;
            var bFoundDevice = false;
            while (EnumDisplayDevices(null, dev, ref dd, 0) && !bFoundDevice)
            {
                var ddMon = new DISPLAY_DEVICE();
                ddMon.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                uint devMon = 0;

                while (EnumDisplayDevices(dd.DeviceName, devMon, ref ddMon, 0) && !bFoundDevice)
                {
                    if ((ddMon.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0 &&
                        (ddMon.StateFlags & DisplayDeviceStateFlags.MirroringDriver) == 0)
                    {
                        bFoundDevice = GetActualEDID(out DeviceID, lsi);
                    }
                    devMon++;

                    ddMon = new DISPLAY_DEVICE();
                    ddMon.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                }

                dd = new DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                dev++;
            }

            return lsi.ToArray();
        }

        const int DICS_FLAG_GLOBAL = 0x00000001;
        const int DIREG_DEV = 0x00000001;
        const int KEY_READ = 0x20019;

        private static bool GetActualEDID(out string DeviceID, List<RawEdid> lsi)
        {
            var pGuid = Marshal.AllocHGlobal(Marshal.SizeOf(GUID_CLASS_MONITOR));
            Marshal.StructureToPtr(GUID_CLASS_MONITOR, pGuid, false);
            var hDevInfo = SetupDiGetClassDevsEx(
                pGuid,
                null,
                nint.Zero,
                DIGCF_PRESENT,
                nint.Zero,
                null,
                nint.Zero);

            DeviceID = string.Empty;

            if (null == hDevInfo)
            {
                Marshal.FreeHGlobal(pGuid);
                return false;
            }

            for (var i = 0; Marshal.GetLastWin32Error() != ERROR_NO_MORE_ITEMS; ++i)
            {
                var devInfoData = new SP_DEVINFO_DATA();
                devInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

                if (SetupDiEnumDeviceInfo(hDevInfo, i, ref devInfoData) > 0)
                {
                    var hDevRegKey = SetupDiOpenDevRegKey(
                        hDevInfo,
                        ref devInfoData,
                        DICS_FLAG_GLOBAL,
                        0,
                        DIREG_DEV,
                        KEY_READ);

                    if (hDevRegKey == null)
                        continue;

                    var si = PullEDID(hDevRegKey);
                    if (si != null)
                    {
                        lsi.Add(si);
                    }
                    RegCloseKey(hDevRegKey);
                }
            }

            Marshal.FreeHGlobal(pGuid);

            return true;
        }

        private const int ERROR_SUCCESS = 0;

        private static RawEdid PullEDID(nuint hDevRegKey)
        {
            RawEdid si = null;
            var valueName = new StringBuilder(128);
            uint ActualValueNameLength = 128;

            var EDIdata = new byte[1024];
            var pEDIdata = Marshal.AllocHGlobal(EDIdata.Length);
            Marshal.Copy(EDIdata, 0, pEDIdata, EDIdata.Length);

            var size = 1024;
            for (uint i = 0, retValue = ERROR_SUCCESS; retValue != ERROR_NO_MORE_ITEMS; i++)
            {
                retValue = RegEnumValue(
                    hDevRegKey, i,
                    valueName, ref ActualValueNameLength,
                    nint.Zero, nint.Zero, pEDIdata, ref size); // EDIdata, pSize);

                var data = valueName.ToString();
                if (retValue != ERROR_SUCCESS || !data.Contains("EDID"))
                    continue;

                if (size < 1)
                    continue;

                var actualData = new byte[size];
                Marshal.Copy(pEDIdata, actualData, 0, size);
                si = new RawEdid(actualData);
            }

            Marshal.FreeHGlobal(pEDIdata);
            return si;
        }

        #endregion

        #region Windows API

        static readonly Guid GUID_CLASS_MONITOR = new Guid(0x4d36e96e, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);

        const int DIGCF_PRESENT = 0x00000002;
        const int ERROR_NO_MORE_ITEMS = 259;

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern uint RegEnumValue(
            nuint hKey,
            uint dwIndex,
            StringBuilder lpValueName,
            ref uint lpcValueName,
            nint lpReserved,
            nint lpType,
            nint lpData,
            ref int lpcbData);

        [Flags()]
        private enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,

            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,

            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,

            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,

            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,

            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public nint Reserved;
        }

        [DllImport("setupapi.dll")]
        private static extern nint SetupDiGetClassDevsEx(nint ClassGuid,
            [MarshalAs(UnmanagedType.LPStr)] string enumerator,
            nint hwndParent, int Flags, nint DeviceInfoSet,
            [MarshalAs(UnmanagedType.LPStr)] string MachineName, nint Reserved);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern int SetupDiEnumDeviceInfo(nint DeviceInfoSet,
            int MemberIndex, ref SP_DEVINFO_DATA DeviceInterfaceData);

        [DllImport("Setupapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nuint SetupDiOpenDevRegKey(
            nint hDeviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            int scope,
            int hwProfile,
            int parameterRegistryValueKind,
            int samDesired);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(
            nuint hKey);

        #endregion
    }
}