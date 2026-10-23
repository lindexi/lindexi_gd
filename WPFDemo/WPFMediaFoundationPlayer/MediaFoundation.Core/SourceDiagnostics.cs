using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MediaFoundation.Core;

/// <summary>生成进程内可关联、不包含媒体位置的源诊断，供核心和宿主共用。</summary>
public static class SourceDiagnostics
{
    private static readonly byte[] salt = RandomNumberGenerator.GetBytes(32);

    /// <summary>描述 URI 与最终原生参数的类型、转义特征及本地访问结果，不返回路径或 URI。</summary>
    public static string Describe(Uri source, string? nativeSource = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Get("AbsoluteUriRequired"), nameof(source));
        string id = Convert.ToHexString(HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(source.AbsoluteUri)))[..16];
        string details = $"sourceId={id} sourceType={(source.IsFile ? "file" : "network")} original=[{Features(source.OriginalString)}] uri=[{Features(source.AbsoluteUri)}]";
        if (nativeSource != null) details += $" native=[{Features(nativeSource)}]";
        if (!source.IsFile) return details;
        try
        {
            string path = source.LocalPath;
            string extension = Path.GetExtension(path);
            extension = extension.Length <= 16 && extension.All(c => char.IsAsciiLetterOrDigit(c) || c == '.') ? extension : "other";
            details += $" exists={File.Exists(path)} extension={extension} localLength={path.Length}";
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            details += $" readAccess=True bytes={RandomAccess.GetLength(handle)} accessHRESULT=0x00000000";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            details += $" readAccess=False accessException={error.GetType().Name} accessHRESULT=0x{error.HResult:X8}";
        }
        return details;
    }

    private static string Features(string text) =>
        $"type={(text.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? "file-url" : Uri.TryCreate(text, UriKind.Absolute, out var uri) && !uri.IsFile ? "url" : "path")} length={text.Length} nonAscii={text.Any(c => c > 127)} space={text.Contains(' ')} hash={text.Contains('#')} percent={text.Contains('%')} ampersand={text.Contains('&')} escapedPercent={text.Contains("%25", StringComparison.OrdinalIgnoreCase)} escapedHash={text.Contains("%23", StringComparison.OrdinalIgnoreCase)} escapedSpace={text.Contains("%20", StringComparison.OrdinalIgnoreCase)} extendedPath={text.StartsWith(@"\\?\", StringComparison.Ordinal)}";
}
