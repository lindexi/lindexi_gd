using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RekafagigerjuNebecocaici;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var displayInfo = GetDisplayInfoList().FirstOrDefault();
        MonitorNameTextBlock.Text = displayInfo?.Name ?? "Null";
    }

    private static IReadOnlyList<DisplayInfo> GetDisplayInfoList()
    {
        var (pathInfos, modeInfos) = GetActivePathInfoAndModeInfo();

        var displayInfoList = new List<DisplayInfo>();

        foreach (var displayConfigPathInfo in pathInfos)
        {
            if (!displayConfigPathInfo.flags.HasFlag(DISPLAYCONFIG_PATH_INFOFlags.DISPLAYCONFIG_PATH_ACTIVE))
            {
                continue;
            }

            var sourceMode = modeInfos[displayConfigPathInfo.sourceInfo.modeInfoIdx].sourceMode;
            var targetMode = modeInfos[displayConfigPathInfo.targetInfo.modeInfoIdx].targetMode;
            _ = targetMode;

            var displayInfo = new DisplayInfo
            {
                Width = sourceMode.width,
                Height = sourceMode.height,
                Left = sourceMode.position.x,
                Top = sourceMode.position.y,
                Name = GetName(displayConfigPathInfo.targetInfo),
            };
            displayInfoList.Add(displayInfo);
        }

        return displayInfoList;
    }

    private static unsafe string? GetName(DISPLAYCONFIG_PATH_TARGET_INFO targetInfo)
    {
        string? name = null;

        DISPLAYCONFIG_TARGET_DEVICE_NAME displayConfigTargetDeviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
        displayConfigTargetDeviceName.header.type =
            DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        displayConfigTargetDeviceName.header.adapterId = targetInfo.adapterId;
        displayConfigTargetDeviceName.header.id = targetInfo.id;
        displayConfigTargetDeviceName.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);

        var result = DisplayConfigGetDeviceInfo((IntPtr)(&displayConfigTargetDeviceName));

        if (result == ERROR_SUCCESS)
        {
            name = new string((char*)(&(displayConfigTargetDeviceName.monitorFriendlyDeviceName)));
        }

        return name;
    }

    private const int ERROR_SUCCESS = 0;

    /// <summary>The data area passed to a system call is too small.</summary>
    private const int ERROR_INSUFFICIENT_BUFFER = 122; // 0x0000007A

    private static (DISPLAYCONFIG_PATH_INFO[] pathInfos, DISPLAYCONFIG_MODE_INFO[] modeInfos)
        GetActivePathInfoAndModeInfo()
    {
        DISPLAYCONFIG_PATH_INFO[] pathInfos;
        DISPLAYCONFIG_MODE_INFO[] modeInfos;
        while (true)
        {
            var result = GetDisplayConfigBufferSizes(QueryDisplayConfigFlags.QDC_ONLY_ACTIVE_PATHS,
                out var pathArrayLength, out var modeArrayLength);
            if (result != ERROR_SUCCESS)
            {
                throw new Win32Exception((int)result);
            }

            pathInfos = new DISPLAYCONFIG_PATH_INFO[pathArrayLength];
            modeInfos = new DISPLAYCONFIG_MODE_INFO[modeArrayLength];
            result = QueryDisplayConfig(QueryDisplayConfigFlags.QDC_ONLY_ACTIVE_PATHS, ref pathArrayLength, pathInfos,
                ref modeArrayLength, modeInfos, IntPtr.Zero);
            if (result == ERROR_SUCCESS)
            {
                break;
            }
            else if (result == ERROR_INSUFFICIENT_BUFFER)
            {
                continue;
            }
            else
            {
                throw new Win32Exception((int)result);
            }
        }

        return (pathInfos, modeInfos);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetDisplayConfigBufferSizes",
        ExactSpelling = true, SetLastError = true)]
    private static extern uint GetDisplayConfigBufferSizes([In] QueryDisplayConfigFlags flags,
        [Out] out int numPathArrayElements, [Out] out int numModeInfoArrayElements);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "QueryDisplayConfig", ExactSpelling = true,
        SetLastError = true)]
    private static extern uint QueryDisplayConfig([In] QueryDisplayConfigFlags flags,
        [In] [Out] ref int numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, [In] [Out] ref int numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray, [In] IntPtr currentTopologyId);


    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DisplayConfigGetDeviceInfo", ExactSpelling = true,
        SetLastError = true)]
    public static extern uint DisplayConfigGetDeviceInfo([In] IntPtr requestPacket);

    [Flags]
    private enum QueryDisplayConfigFlags : uint
    {
        QDC_ALL_PATHS = 0x00000001,
        QDC_ONLY_ACTIVE_PATHS = 0x00000002,
        QDC_DATABASE_CURRENT = 0x00000004,
        QDC_VIRTUAL_MODE_AWARE = 0x00000010,
        QDC_INCLUDE_HMD = 0x00000020,
        QDC_VIRTUAL_REFRESH_RATE_AWARE = 0x00000040,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public DISPLAYCONFIG_PATH_INFOFlags flags;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)] public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public long adapterId;
        [FieldOffset(16)] public DISPLAYCONFIG_TARGET_MODE targetMode;
        [FieldOffset(16)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
    }

    private enum DISPLAYCONFIG_MODE_INFO_TYPE
    {
        DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1,
        DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public long adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public long adapterId;
        public uint id;
        public uint modeInfoIdx;
        public int outputTechnology;
        public int rotation;
        public int scaling;
        public int refreshRate;
        public int scanLineOrdering;
        public bool targetAvailable;
        public int statusFlags;
    }


    [Flags]
    private enum DISPLAYCONFIG_PATH_INFOFlags : uint
    {
        DISPLAYCONFIG_PATH_ACTIVE = 0x00000001,
        DISPLAYCONFIG_PATH_SUPPORT_VIRTUAL_MODE = 0x00000008,
        DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE = 0x00000010,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_MODE
    {
        public int width;
        public int height;
        public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
        public POINTL position;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public ulong hSyncFreq;
        public ulong vSyncFreq;
        public DISPLAYCONFIG_2DREGION activeSize;
        public DISPLAYCONFIG_2DREGION totalSize;
        public uint videoStandard;
        public uint scanLineOrdering;
    }

    private enum DISPLAYCONFIG_PIXELFORMAT
    {
        DISPLAYCONFIG_PIXELFORMAT_8BPP = 1,
        DISPLAYCONFIG_PIXELFORMAT_16BPP = 2,
        DISPLAYCONFIG_PIXELFORMAT_24BPP = 3,
        DISPLAYCONFIG_PIXELFORMAT_32BPP = 4,
        DISPLAYCONFIG_PIXELFORMAT_NONGDI = 5,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
        public uint size;
        public long adapterId;
        public uint id;
    }

    private enum DISPLAYCONFIG_DEVICE_INFO_TYPE
    {
        DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
        DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
        DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
        DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
        DISPLAYCONFIG_DEVICE_INFO_GET_SUPPORT_VIRTUAL_RESOLUTION = 7,
        DISPLAYCONFIG_DEVICE_INFO_SET_SUPPORT_VIRTUAL_RESOLUTION = 8,
        DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9,
        DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10,
        DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11,
        DISPLAYCONFIG_DEVICE_INFO_GET_MONITOR_SPECIALIZATION,
        DISPLAYCONFIG_DEVICE_INFO_SET_MONITOR_SPECIALIZATION,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 64 * sizeof(char))]
    private struct ByValStringStructForSize64
    {
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 128 * sizeof(char))]
    private struct ByValStringStructForSize128
    {
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_TARGET_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public uint flags;
        public int outputTechnology;
        public ushort edidManufactureId;
        public ushort edidProductCodeId;
        public uint connectorInstance;
        public ByValStringStructForSize64 monitorFriendlyDeviceName;
        public ByValStringStructForSize128 monitorDevicePath;
    }
}

record DisplayInfo
{
    public int Width { get; init; }

    public int Height { get; init; }

    public int Left { get; init; }

    public int Top { get; init; }

    public string? Name { get; init; }
}