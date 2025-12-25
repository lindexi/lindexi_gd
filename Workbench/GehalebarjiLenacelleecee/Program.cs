// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Text;

// Signed = 947712
// App = 947712

var appFile = Path.Join(AppContext.BaseDirectory, "SignedApp.exe");

// 如果已经存在则直接读取 PE 头信息，获取 Overlays 信息
if (File.Exists(appFile))
{
    using var fs = File.OpenRead(appFile);
    using var peReader = new PEReader(fs);
    var parts = PeOverlayHelper.GetOverlayAndCertificate(fs, peReader);
    Console.WriteLine($"Certificate: offset={parts.certificateOffset}, length={parts.certificateLength}");
    Console.WriteLine($"Overlay before certificate: offset={parts.overlayBeforeCertOffset}, length={parts.overlayBeforeCertLength}");
    // 示例读取前后两个 Overlay 的部分内容
    if (parts.overlayBeforeCertLength > 0)
    {
        fs.Seek(parts.overlayBeforeCertOffset, SeekOrigin.Begin);
        var sampleLen = (int)Math.Min(parts.overlayBeforeCertLength, 64);
        var sample = new byte[sampleLen];
        _ = fs.Read(sample, 0, sampleLen);
        Console.WriteLine($"Overlay(before) sample (first {sampleLen} bytes): {BitConverter.ToString(sample)}");
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
    var parts2 = PeOverlayHelper.GetOverlayAndCertificate(verifyStream, peReader2);
    Console.WriteLine($"Certificate: offset={parts2.certificateOffset}, length={parts2.certificateLength}");
    Console.WriteLine($"Overlay before certificate: offset={parts2.overlayBeforeCertOffset}, length={parts2.overlayBeforeCertLength}");
}

// 计算并读取 PE Overlays 的帮助类
static class PeOverlayHelper
{
    // 返回证书区间、证书前的 Overlay、证书后的 Overlay
    public static (
        long certificateOffset,
        long certificateLength,
        long overlayBeforeCertOffset,
        long overlayBeforeCertLength
    ) GetOverlayAndCertificate(FileStream fs, PEReader peReader)
    {
        var headers = peReader.PEHeaders;
        long fileLen = fs.Length;

        // 计算最后一个节结束位置
        long lastSectionEnd = 0;
        foreach (var s in headers.SectionHeaders)
        {
            long end = (long)s.PointerToRawData + s.SizeOfRawData;
            if (end > lastSectionEnd)
                lastSectionEnd = end;
        }
        if (headers.SectionHeaders.Length == 0)
        {
            lastSectionEnd = headers.PEHeader.SizeOfHeaders;
        }

        // 读取 Security 目录（证书位置与大小）。注意：Security 目录的 RVA 字段是文件偏移而非 RVA。
        var securityDir = headers.PEHeader!.CertificateTableDirectory;
        long certOffset = securityDir.Size == 0 ? 0 : securityDir.RelativeVirtualAddress; // 实际是文件偏移
        long certLength = securityDir.Size;

        // 规范：AuthentiCode 签名位于文件靠后位置，校验覆盖证书区间之外的所有数据。
        // 我们将 Overlay 分为两段：
        // 1) 证书前的 Overlay：从 lastSectionEnd 到 certOffset（不含证书本身）。
        // 2) 证书后的 Overlay：从 certOffset+certLength 到文件末尾。

        long beforeOffset = 0, beforeLength = 0;
        long afterOffset = 0, afterLength = 0;

        if (fileLen > lastSectionEnd)
        {
            if (certLength > 0 && certOffset > lastSectionEnd)
            {
                beforeOffset = lastSectionEnd;
                beforeLength = Math.Max(0, certOffset - lastSectionEnd);
            }
            else if (certLength == 0)
            {
                // 无证书则整个 lastSectionEnd 之后都是 Overlay
                beforeOffset = lastSectionEnd;
                beforeLength = fileLen - lastSectionEnd;
            }
        }

        if (certLength > 0)
        {
            long certEnd = certOffset + certLength;
            if (fileLen > certEnd)
            {
                afterOffset = certEnd;
                afterLength = fileLen - certEnd;

                // 不会存在此情况
            }
        }

        return (certOffset, certLength, beforeOffset, beforeLength);
    }
}
