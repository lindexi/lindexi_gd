// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Text;

var appFile = Path.Join(AppContext.BaseDirectory, "App.exe");

// 如果已经存在则直接读取 PE 头信息，获取 Overlays 信息
if (File.Exists(appFile))
{
    using var fs = File.OpenRead(appFile);
    using var peReader = new PEReader(fs);
    var overlayInfo = PeOverlayHelper.GetOverlay(fs, peReader);
    long overlayOffset = overlayInfo.offset;
    long overlayLength = overlayInfo.length;

    Console.WriteLine($"Found overlay: offset={overlayOffset}, length={overlayLength}");
    if (overlayLength > 0)
    {
        fs.Seek(overlayOffset, SeekOrigin.Begin);
        var sampleLen = (int)Math.Min(overlayLength, 64);
        var sample = new byte[sampleLen];
        _ = fs.Read(sample, 0, sampleLen);
        Console.WriteLine($"Overlay sample (first {sampleLen} bytes): {BitConverter.ToString(sample)}");
    }
    return;
}

var file = @"C:\lindexi\Work\App.exe";
var fileStream = File.Open(file, FileMode.Open);

// 从模版应用拷贝内容
await using var appFileStream = File.Create(appFile);
await fileStream.CopyToAsync(appFileStream);

// 尝试添加 Overlays 内容
var buffer = ArrayPool<byte>.Shared.Rent(102400);

for (byte i = 0; i < byte.MaxValue; i++)
{
    Random.Shared.Shuffle(buffer);
    buffer[i] = i;
    await appFileStream.WriteAsync(buffer.AsMemory());
}

for (int i = 0; i < 10000; i++)
{
    Random.Shared.Shuffle(buffer);
    await appFileStream.WriteAsync(buffer.AsMemory());
}

ArrayPool<byte>.Shared.Return(buffer);

// 确保写入完成
await appFileStream.FlushAsync();

// 重新打开并读取 Overlays 信息进行验证
appFileStream.Position = 0;
var verifyStream = appFileStream;
using (var peReader2 = new PEReader(verifyStream))
{
    var overlayInfo2 = PeOverlayHelper.GetOverlay(verifyStream, peReader2);
    long overlayOffset2 = overlayInfo2.offset;
    long overlayLength2 = overlayInfo2.length;
    var fileStreamLength = fileStream.Length;
    Console.WriteLine($"Added overlay: offset={overlayOffset2}, length={overlayLength2}");
}

// 计算并读取 PE Overlays 的帮助类
static class PeOverlayHelper
{
    public static (long offset, long length) GetOverlay(FileStream fs, PEReader peReader)
    {
        var headers = peReader.PEHeaders;
        // 计算文件中最后一个节的末尾位置：max(Section.PointerToRawData + Section.SizeOfRawData)
        long lastSectionEnd = 0;
        foreach (var s in headers.SectionHeaders)
        {
            long end = (long)s.PointerToRawData + s.SizeOfRawData;
            if (end > lastSectionEnd)
                lastSectionEnd = end;
        }

        // 如果没有节，则使用 SizeOfHeaders 作为镜像文件末尾
        if (headers.SectionHeaders.Length == 0)
        {
            lastSectionEnd = headers.PEHeader.SizeOfHeaders;
        }

        // 覆盖数据从 lastSectionEnd 开始直到文件末尾
        long fileLen = fs.Length;
        if (fileLen <= lastSectionEnd)
        {
            return (0, 0);
        }

        long overlayOffset = lastSectionEnd;
        long overlayLength = fileLen - lastSectionEnd;
        return (overlayOffset, overlayLength);
    }
}
