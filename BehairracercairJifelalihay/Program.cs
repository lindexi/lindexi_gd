// See https://aka.ms/new-console-template for more information


using System.IO;
using System.Runtime.InteropServices;
using BehairracercairJifelalihay;

var file = "edid";
var result = EdidInfo.ReadEdidFromFile(file);

if (Uri.TryCreate(new Uri("http://127.0.0.1/foo/storage"),"path/f123", out var uri))
{
    // uri = http://127.0.0.1/foo/path/f123
}

if (Uri.TryCreate(new Uri("http://127.0.0.1"), "path/f123", out uri))
{
    // uri = http://127.0.0.1/path/f123
}

if (OperatingSystem.IsLinux())
{
    var readEdidInfoResult = EdidInfo.ReadFormLinux();
}
else if (OperatingSystem.IsWindows())
{
    // 通过 GetDeviceCaps 方法获取
}

while (true)
{
    Console.Read();
}

<<<<<<< HEAD
public readonly record struct EdidInfo
{
    public char ManufacturerNameChar0 { get; init; }
    public char ManufacturerNameChar1 { get; init; }
    public char ManufacturerNameChar2 { get; init; }
    public string ManufacturerName => new string([ManufacturerNameChar0, ManufacturerNameChar1, ManufacturerNameChar2]);

    public byte ManufactureWeek { get; init; }

    /// <summary>
    /// 已加上 1990 的年份
    /// </summary>
    public int ManufactureYear { get; init; }

    public byte Version { get; init; }
    public byte Revision { get; init; }

    public Version EdidVersion => new Version(Version, Revision);

    /// <summary>
    /// See Section 3.6
    /// </summary>
    public EdidBasicDisplayParameters BasicDisplayParameters { get; init; }

    private const int MinEdidDataLength = 128;

    public static ReadEdidInfoResult ReadFormLinux()
    {
        var drmFolder = "/sys/class/drm/";

        foreach (var subFolder in Directory.EnumerateDirectories(drmFolder))
        {
            var enableFile = Path.Join(subFolder, "enabled");
            if (File.Exists(enableFile))
            {
                var enabledText = File.ReadAllText(enableFile);
                // 也许里面存放的是 enabled\n 字符
                if (enabledText.StartsWith("enabled"))
                {
                    var edid = Path.Join(subFolder, "edid");
                    if (File.Exists(edid))
                    {
                        return ReadEdidFromFile(edid);
                    }
                }
            }
        }

        return ReadEdidInfoResult.Fail("找不到可用的 EDID 文件");
    }

    public static ReadEdidInfoResult ReadEdidFromFile(string edidFile)
    {
        using var fileStream = new FileStream(edidFile, FileMode.Open, FileAccess.Read, FileShare.Read,
            MinEdidDataLength, false);
        return ReadEdidFromStream(fileStream);
    }

    public static ReadEdidInfoResult ReadEdidFromStream(Stream stream)
    {
        // This document describes the basic 128-byte data structure "EDID 1.3", as well as the overall layout of the
        // data blocks that make up Enhanced EDID. 
        Span<byte> edidSpan = stackalloc byte[MinEdidDataLength * 2];
        // https://glenwing.github.io/docs/VESA-EEDID-A1.pdf
        var readLength = stream.Read(edidSpan);
        if (readLength < MinEdidDataLength)
        {
            return ReadEdidInfoResult.Fail("文件长度小于 EDID 要求最短长度");
        }

        var edidInfo = ReadEdid(edidSpan);
        return edidInfo;
    }

    public static ReadEdidInfoResult ReadEdid(Span<byte> edid)
    {
        const int minLength = 128;

        // Header
        var edidHeader = edid[..8];
        if (edidHeader is not [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00])
        {
            // 这不是一份有效的 edid 文件
            return ReadEdidInfoResult.Fail("这不是一份有效的 EDID 文件，校验 Header 失败");
        }

        // 3.11 Extension Flag and Checksum
        // This byte should be programmed such that a one-byte checksum of the entire 128-byte EDID equals 00h.
        byte checksumValue = 0;
        for (int i = 0; i < minLength; i++)
        {
            checksumValue += edid[i];
        }

        if (checksumValue != 0)
        {
            return ReadEdidInfoResult.Fail("这不是一份有效的 EDID 文件，校验 checksum 失败");
        }

        // 3.4 Vendor/Product ID: 10 bytes
        // 看起来有些离谱的格式，用两个 byte 表示三个字符
        // ID Manufacturer Name
        // EISA manufacturer IDs are issued by Microsoft. Contact by: E-mail: pnpid@microsoft.com
        var nameShort = (int) MemoryMarshal.Cast<byte, short>(edid.Slice(0x08, 2))[0];
        nameShort = ((nameShort & 0xff00) >> 8) | ((nameShort & 0x00ff) << 8);
        // 这里面是包含三个字符也是诡异的设计
        var nameChar2 = (char) ('A' + ((nameShort >> 0) & 0x1f) - 1);
        var nameChar1 = (char) ('A' + ((nameShort >> 5) & 0x1f) - 1);
        var nameChar0 = (char) ('A' + ((nameShort >> 10) & 0x1f) - 1);

        var week = edid[0x10];
        // The Year of Manufacture field is used to represent the year of the monitor’s manufacture. The value that is stored is
        // an offset from the year 1990 as derived from the following equation:
        // Value stored = (Year of manufacture - 1990)
        // Example: For a monitor manufactured in 1997 the value stored in this field would be 7.
        var manufactureYear = edid[0x11] + 1990;

        // Section 3.5 EDID Structure Version / Revision 2 bytes
        var version = edid[0x12];
        var revision = edid[0x13]; // 如 1.3 版本，那么 version == 1 且 revision == 3 的值
        // EDID structure 1.3 is introduced for the first time in this document and adds definitions for secondary GTF curve
        // coefficients. EDID 1.3 is based on the same core as all other EDID 1.x structures. EDID 1.3 is intended to be the
        // new baseline for EDID data structures. EDID 1.3 is recommended for all new monitor designs.
        //new Version(version, revision)

        // Section 3.6 Basic Display Parameters / Features 5 bytes
        // Video Input Definition
        var videoInputDefinition = edid[0x14];
        var maxHorizontalImageSize = edid[0x15];
        var maxVerticalImageSize = edid[0x16];

        // 这里的 ImageSize 其实就是屏幕的物理尺寸
        // 单位是厘米
        var monitorPhysicalWidth = new Cm(maxHorizontalImageSize);
        var monitorPhysicalHeight = new Cm(maxVerticalImageSize);

        var displayTransferCharacteristicGamma = edid[0x17];
        var featureSupport = edid[0x18];

        var edidBasicDisplayParameters = new EdidBasicDisplayParameters()
        {
            VideoInputDefinition = videoInputDefinition,
            MonitorPhysicalWidth = monitorPhysicalWidth,
            MonitorPhysicalHeight = monitorPhysicalHeight,
            DisplayTransferCharacteristicGamma = displayTransferCharacteristicGamma,
            FeatureSupport = featureSupport,
        };

        return ReadEdidInfoResult.Success(new EdidInfo()
        {
            ManufacturerNameChar0 = nameChar0,
            ManufacturerNameChar1 = nameChar1,
            ManufacturerNameChar2 = nameChar2,

            ManufactureWeek = week,
            ManufactureYear = manufactureYear,

            Version = version,
            Revision = revision,

            BasicDisplayParameters = edidBasicDisplayParameters,
        });
    }

    public readonly record struct Cm(uint Value)
    {
        public override string ToString() => $"{Value} cm";
    }
}

public readonly record struct ReadEdidInfoResult(bool IsSuccess, string ErrorMessage, EdidInfo EdidInfo)
{
    public static ReadEdidInfoResult Success(EdidInfo edidInfo) => new ReadEdidInfoResult(true, "Success", edidInfo);
    public static ReadEdidInfoResult Fail(string errorMessage) => new ReadEdidInfoResult(false, errorMessage, default);
}

public readonly struct EdidBasicDisplayParameters
{
    /// <summary>
    /// See Table 3.9 - Video Input Definition
    /// </summary>
    public byte VideoInputDefinition { get; init; }

    /// <summary>
    /// 物理屏幕宽度
    /// </summary>
    public EdidInfo.Cm MonitorPhysicalWidth { get; init; }

    /// <summary>
    /// 物理屏幕高度
    /// </summary>
    public EdidInfo.Cm MonitorPhysicalHeight { get; init; }

    public byte DisplayTransferCharacteristicGamma { get; init; }
    public byte FeatureSupport { get; init; }
=======

EdidInfo ReadEdidFromFile(string edidFile)
{
    // This document describes the basic 128-byte data structure "EDID 1.3", as well as the overall layout of the
    // data blocks that make up Enhanced EDID. 
    const int minLength = 128;
    Span<byte> edidSpan = stackalloc byte[minLength * 2];
    // https://glenwing.github.io/docs/VESA-EEDID-A1.pdf

    using (var fileStream = new FileStream(edidFile, FileMode.Open, FileAccess.Read, FileShare.Read, minLength, false))
    {
        var readLength = fileStream.Read(edidSpan);
        Debug.Assert(readLength >= minLength);
    }

    var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
    var edidInfo = ReadEdid(edidSpan);
    var result = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
    Console.WriteLine($"{result}分配");
    return edidInfo;
}

EdidInfo ReadEdid(Span<byte> span)
{
    const int minLength = 128;

    // Header
    var edidHeader = span[..8];
    if (edidHeader is not [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00])
    {
        // 这不是一份有效的 edid 文件
        throw new ArgumentException("这不是一份有效的 edid 文件，校验 Header 失败");
    }

    // 3.11 Extension Flag and Checksum
    // This byte should be programmed such that a one-byte checksum of the entire 128-byte EDID equals 00h.
    byte checksumValue = 0;
    for (int i = 0; i < minLength; i++)
    {
        checksumValue += span[i];
    }

    if (checksumValue != 0)
    {
        throw new ArgumentException("这不是一份有效的 edid 文件，校验 checksum 失败");
    }

    // 3.4 Vendor/Product ID: 10 bytes
    // 看起来有些离谱的格式，用两个 byte 表示三个字符
    // ID Manufacturer Name
    // EISA manufacturer IDs are issued by Microsoft. Contact by: E-mail: pnpid@microsoft.com
    var nameShort = (int) MemoryMarshal.Cast<byte, short>(span.Slice(0x08, 2))[0];
    nameShort = ((nameShort & 0xff00) >> 8) | ((nameShort & 0x00ff) << 8);
    // 这里面是包含三个字符也是诡异的设计
    var nameChar2 = (char) ('A' + ((nameShort >> 0) & 0x1f) - 1);
    var nameChar1 = (char) ('A' + ((nameShort >> 5) & 0x1f) - 1);
    var nameChar0 = (char) ('A' + ((nameShort >> 10) & 0x1f) - 1);
    //// 转换一下大概32个长度
    //string manufacturerName = new string([nameChar0, nameChar1, nameChar2]);
    //Console.WriteLine($"Name={manufacturerName}");

    var week = span[0x10];
    // The Year of Manufacture field is used to represent the year of the monitor’s manufacture. The value that is stored is
    // an offset from the year 1990 as derived from the following equation:
    // Value stored = (Year of manufacture - 1990)
    // Example: For a monitor manufactured in 1997 the value stored in this field would be 7.
    var manufactureYear = span[0x11] + 1990;

    // Section 3.5 EDID Structure Version / Revision 2 bytes
    var version = span[0x12];
    var revision = span[0x13]; // 如 1.3 版本，那么 version == 1 且 revision == 3 的值
    // EDID structure 1.3 is introduced for the first time in this document and adds definitions for secondary GTF curve
    // coefficients. EDID 1.3 is based on the same core as all other EDID 1.x structures. EDID 1.3 is intended to be the
    // new baseline for EDID data structures. EDID 1.3 is recommended for all new monitor designs.
    //new Version(version, revision)

    // Section 3.6 Basic Display Parameters / Features 5 bytes
    // Video Input Definition
    var videoInputDefinition = span[0x14];
    var maxHorizontalImageSize = span[0x15];
    var maxVerticalImageSize = span[0x16];

    // 这里的 ImageSize 其实就是屏幕的物理尺寸
    // 单位是厘米
    var monitorPhysicalWidth = new Cm(maxHorizontalImageSize);
    var monitorPhysicalHeight = new Cm(maxVerticalImageSize);

    //Console.WriteLine($"屏幕尺寸 {monitorPhysicalWidth} x {monitorPhysicalHeight}");

    var displayTransferCharacteristicGamma = span[0x17];
    var featureSupport = span[0x18];

    var edidBasicDisplayParameters = new EdidBasicDisplayParameters()
    {
        VideoInputDefinition = videoInputDefinition,
        MonitorPhysicalWidth = monitorPhysicalWidth,
        MonitorPhysicalHeight = monitorPhysicalHeight,
        DisplayTransferCharacteristicGamma = displayTransferCharacteristicGamma,
        FeatureSupport = featureSupport,
    };

    return new EdidInfo()
    {
        ManufacturerNameChar0 = nameChar0,
        ManufacturerNameChar1 = nameChar1,
        ManufacturerNameChar2 = nameChar2,

        ManufactureWeek = week,
        ManufactureYear = manufactureYear,

        Version = version,
        Revision = revision,

        BasicDisplayParameters = edidBasicDisplayParameters,
    };
>>>>>>> 4daa2db33d29151455df6acab1d8d61a126467e9
}