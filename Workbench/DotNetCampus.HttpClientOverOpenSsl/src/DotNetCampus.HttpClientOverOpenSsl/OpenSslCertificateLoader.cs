using System.Security.Cryptography.X509Certificates;
using System.Text;
using DotNetCampus.HttpClientOverOpenSsl.Interop;

namespace DotNetCampus.HttpClientOverOpenSsl;

/// <summary>
/// Windows 根证书加载器，从 Windows 证书存储导出受信任的根证书到临时 PEM 文件，并加载到 OpenSSL 上下文。
/// </summary>
internal static class OpenSslCertificateLoader
{
    /// <summary>
    /// 缓存已导出的 Windows 根证书 PEM 文件路径列表，避免重复导出。
    /// </summary>
    private static WindowsRootCertsCache? s_rootCertsCache;

    private sealed record WindowsRootCertsCache(IReadOnlyList<string> TempFilePaths);

    /// <summary>
    /// 从 Windows 证书存储导出受信任的根证书到临时 PEM 文件，并加载到 OpenSSL 上下文。
    /// 首次调用时导出并缓存，后续调用复用缓存。
    /// </summary>
    public static async Task LoadWindowsRootCertsAsync(SafeSslContextHandle sslContext)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (s_rootCertsCache is not null)
        {
            LoadCertsFromCache(sslContext, s_rootCertsCache);
            return;
        }

        var tempFiles = await ExportWindowsRootCertsAsync().ConfigureAwait(false);
        if (tempFiles is null || tempFiles.Count == 0)
        {
            return;
        }

        // 多线程重入时，多个实例可能同时导出，但最终只有一个缓存被保留。
        // 这不会导致业务问题，因为所有导出的证书内容相同。
        var cache = new WindowsRootCertsCache(tempFiles);
        s_rootCertsCache = cache;
        LoadCertsFromCache(sslContext, cache);
    }

    private static void LoadCertsFromCache(SafeSslContextHandle sslContext, WindowsRootCertsCache cache)
    {
        foreach (var tempFile in cache.TempFilePaths)
        {
            OpenSSLNative.SSL_CTX_load_verify_locations(sslContext, tempFile, null);
        }
    }

    /// <summary>
    /// 从 Windows 证书存储导出受信任的根证书到临时 PEM 文件。
    /// </summary>
    private static async Task<IReadOnlyList<string>> ExportWindowsRootCertsAsync()
    {
        var tempFiles = new List<string>(128);

        X509Store? store = null;
        try
        {
            store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
        }
        catch
        {
            store?.Dispose();
            store = null;
        }

        if (store is null)
        {
            try
            {
                store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
            }
            catch
            {
                store?.Dispose();
                return tempFiles;
            }
        }

        using (store)
        {
            foreach (var cert in store.Certificates)
            {
                var tempFile = Path.GetTempFileName();
                try
                {
                    var pem = ExportCertificateAsPem(cert);
                    // 确保使用 \n 换行符（PEM 标准），而非 Windows 的 \r\n
                    await File.WriteAllTextAsync(tempFile, pem.Replace("\r\n", "\n"), Encoding.ASCII).ConfigureAwait(false);
                    tempFiles.Add(tempFile);
                }
                catch
                {
                    try { File.Delete(tempFile); } catch { /* ignore */ }
                }
            }
        }

        return tempFiles;
    }

    /// <summary>
    /// 将 X509Certificate2 证书导出为 PEM 格式字符串（.NET 6 兼容实现，替代 .NET 9+ 的 ExportCertificatePem）。
    /// </summary>
    internal static string ExportCertificateAsPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder(2048);
        builder.AppendLine("-----BEGIN CERTIFICATE-----");
        builder.AppendLine(Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END CERTIFICATE-----");
        return builder.ToString();
    }
}
