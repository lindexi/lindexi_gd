// See https://aka.ms/new-console-template for more information
//#define DebugAllocated

using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using EDIDParser;

using Microsoft.Win32.SafeHandles;

//ReadEdid("edid");

var drmFolder = "/sys/class/drm/";

var file = "/sys/class/drm/card0-DP-2/edid";
if (File.ReadAllBytes(file).Length > 0)
{
    Console.WriteLine($"读取成功");
}

// 经过测试，可以在 UOS 里面用 File.ReadAllBytes 读取到

{
    // 用 File.OpenRead 读取不到
    var fileStream = File.OpenRead(file);
    Console.WriteLine($"File.OpenRead {fileStream.Length}");

    // 似乎还可以强行读取试试看？
    // 那就读取试试
    var buffer = ArrayPool<byte>.Shared.Rent(256);
    try
    {
        var readLength = fileStream.Read(buffer.AsSpan());
        Console.WriteLine($"ReadLength={readLength}");
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    fileStream.Dispose();

    // 用 new FileStream 读取不到
    // 其实读取到没有长度不代表没有内容
    // Some file systems (e.g. procfs on Linux) return 0 for length even when there's content; also there are non-seekable files.
    fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
    Console.WriteLine($"new FileStream Length = {fileStream.Length}");
    buffer = ArrayPool<byte>.Shared.Rent(256);
    try
    {
        var readLength = fileStream.Read(buffer.AsSpan());
        Console.WriteLine($"ReadLength={readLength}");
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
    fileStream.Dispose();

    /*
       lrwxrwxrwx 1 root root    0 4月  22 09:58 device -> ../../card0
       -r--r--r-- 1 root root 4.0K 4月  22 09:58 dpms
       -r--r--r-- 1 root root    0 4月  22 09:58 edid
       -r--r--r-- 1 root root 4.0K 4月  22 09:58 enabled
       -r--r--r-- 1 root root 4.0K 4月  22 09:58 modes
       drwxr-xr-x 2 root root    0 4月  22 09:58 power
       -rw-r--r-- 1 root root 4.0K 4月  22 09:58 status
       lrwxrwxrwx 1 root root    0 4月  22 09:58 subsystem -> ../../../../../../class/drm
       -rw-r--r-- 1 root root 4.0K 4月  22 09:58 uevent
     */
    // 可以看到文件挂载里面显示的就是没有文件长度

    using var safeFileHandle = File.OpenHandle(file);
    fileStream = new FileStream(safeFileHandle, FileAccess.Read);
    Console.WriteLine($"File.OpenHandle Length = {fileStream.Length}");
}

Console.Read();

Console.WriteLine($"/sys/class/drm/ 存在 {Directory.Exists(drmFolder)}");

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
                var data = File.ReadAllBytes(edid);
                Console.WriteLine($"Data={data.Length}");

                ReadEdid(data);

                //Console.WriteLine($"Read edid {edid}");
                //ReadEdidFromFile(edid);
            }
        }

        Console.WriteLine($"{enabledText.Replace("\n", "\\n")}");
    }
}
// “/sys/class/drm/”文件夹的 这里的 drm 是什么的缩写或什么含义？

Console.Read();


Console.Read();

unsafe void ReadEdidFromFile(string edidFile)
{
    unsafe
    {
#if DebugAllocated
        var currentAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
        long deltaAllocatedBytes = 0;
        var lastAllocatedBytes = currentAllocatedBytes;
#endif

        Span<byte> edidSpan;
        // https://glenwing.github.io/docs/VESA-EEDID-A1.pdf
        // This document describes the basic 128-byte data structure "EDID 1.3", as well as the overall layout of the
        // data blocks that make up Enhanced EDID. 
        const int minLength = 128;

        using (var fileStream = new FileStream(edidFile, FileMode.Open, FileAccess.Read, FileShare.Read, minLength, false))
        {
            Console.WriteLine($"FileLength={fileStream.Length}");

            //LogMemoryAllocated();

            if (fileStream.Length <= minLength * 2 && false)
            {
                edidSpan = stackalloc byte[(int) fileStream.Length];
            }
            else
            {
                edidSpan = new byte[(int) fileStream.Length];
            }

            var readLength = fileStream.Read(edidSpan);
            Debug.Assert(fileStream.Length == readLength);
        }

        //LogMemoryAllocated();

        Console.WriteLine($"Start read Header");

        ReadEdid(edidSpan);
    }
}

void ReadEdid(Span<byte> span)
{
    const int minLength = 128;

    // Header
    var edidHeader = span[..8];
    if (edidHeader is not [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00])
    {
        // 这不是一份有效的 edid 文件
        throw new ArgumentException("这不是一份有效的 edid 文件，校验 Header 失败");
    }

    Console.WriteLine($"Start read checksum");

    // checksum
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

    LogMemoryAllocated();

    Console.WriteLine($"Start read name");

    // 3.4 Vendor/Product ID: 10 bytes
    // 看起来有些离谱
    // ID Manufacturer Name
    // EISA manufacturer IDs are issued by Microsoft. Contact by: E-mail: pnpid@microsoft.com
    var nameShort = (int) MemoryMarshal.Cast<byte, short>(span.Slice(0x08, 2))[0];
    nameShort = ((nameShort & 0xff00) >> 8) | ((nameShort & 0x00ff) << 8);
    // 这里面是包含三个字符也是诡异的设计
    var nameChar2 = (char) ('A' + ((nameShort >> 0) & 0x1f) - 1);
    var nameChar1 = (char) ('A' + ((nameShort >> 5) & 0x1f) - 1);
    var nameChar0 = (char) ('A' + ((nameShort >> 10) & 0x1f) - 1);
    //// 转换一下大概32个长度
    string manufacturerName = new string([nameChar0, nameChar1, nameChar2]);
    Console.WriteLine($"Name={manufacturerName}");
    //LogMemoryAllocated();


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

    LogMemoryAllocated();
    Console.WriteLine($"屏幕尺寸 {monitorPhysicalWidth} x {monitorPhysicalHeight}");

    var displayTransferCharacteristicGamma = span[0x17];
    var featureSupport = span[0x18];

    int[] n = [1, 2, 3];

    var value = n.AsSpan();
    if (value is [2, 2, 3])
    {
        // 底层
        /*
       IL_0021: ldloca.s     'value'
       IL_0023: call         instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
       IL_0028: ldc.i4.3
       IL_0029: bne.un.s     IL_0051
       IL_002b: ldloca.s     'value'
       IL_002d: ldc.i4.0
       IL_002e: call         instance !0/*int32* /& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
       IL_0033: ldind.i4
       IL_0034: ldc.i4.2
       IL_0035: bne.un.s     IL_0051
       IL_0037: ldloca.s     'value'
       IL_0039: ldc.i4.1
       IL_003a: call         instance !0/*int32* /& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
       IL_003f: ldind.i4
       IL_0040: ldc.i4.2
       IL_0041: bne.un.s     IL_0051
       IL_0043: ldloca.s     'value'
       IL_0045: ldc.i4.2
       IL_0046: call         instance !0/*int32* /& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
       IL_004b: ldind.i4
       IL_004c: ldc.i4.3
       IL_004d: ceq
       IL_004f: br.s         IL_0052
       IL_0051: ldc.i4.0
       IL_0052: stloc.s      V_5
     */
        // 重新转换为低级 C# 代码
        /*
         if (value.Length != 3 || value[0] != 2 || value[1] != 2 || value[2] != 3)
     */
    }

    void LogMemoryAllocated()
    {
#if DebugAllocated
        l = GC.GetAllocatedBytesForCurrentThread();
        deltaAllocatedBytes1 = l - lastAllocatedBytes1;
        Console.WriteLine($"内存申请量 {deltaAllocatedBytes1}");
        lastAllocatedBytes1 = GC.GetAllocatedBytesForCurrentThread();
#endif // DebugAllocated
    }
}

readonly record struct Cm(uint Value)
{
    public override string ToString() => $"{Value} cm";
}