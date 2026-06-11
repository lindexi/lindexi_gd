using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DotNetCampus.HttpClientOverOpenSsl;
using DotNetCampus.HttpClientOverOpenSsl.Interop;

/// <summary>
/// 基于 OpenSSL 原生 DLL 实现的 HttpMessageHandler，通过 P/Invoke 调用 libssl-3.dll 完成 TLS 握手和数据传输。
/// </summary>
internal sealed class OpenSSLHttpMessageHandler : HttpMessageHandler
{
    private static readonly bool s_initialized;

    /// <summary>
    /// 缓存已导出的 Windows 根证书 PEM 文件路径列表，避免每次请求重复导出。
    /// 使用记录类型确保多线程重入时赋值操作的原子性。
    /// </summary>
    private WindowsRootCertsCache? _rootCertsCache;

    private sealed record WindowsRootCertsCache(IReadOnlyList<string> TempFilePaths);

    static OpenSSLHttpMessageHandler()
    {
        s_initialized = OpenSSLNative.OPENSSL_init_ssl(
            OpenSSLNative.OPENSSL_INIT_LOAD_SSL_STRINGS | OpenSSLNative.OPENSSL_INIT_LOAD_CRYPTO_STRINGS,
            IntPtr.Zero) == 1;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!s_initialized)
        {
            throw new InvalidOperationException("OpenSSL 初始化失败。");
        }

        var uri = request.RequestUri ?? throw new ArgumentException("请求 URI 不能为空。", nameof(request));

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"仅支持 HTTPS 协议，当前协议: {uri.Scheme}");
        }

        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 443;
        var path = uri.PathAndQuery;

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        cancellationToken.ThrowIfCancellationRequested();
        await socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        using var sslContext = OpenSSLNative.SSL_CTX_new(OpenSSLNative.TLS_client_method());
        if (sslContext.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 SSL 上下文。");
        }

        OpenSSLNative.SSL_CTX_set_default_verify_paths(sslContext);
        OpenSSLNative.SSL_CTX_set_verify(sslContext, OpenSSLNative.SSL_VERIFY_PEER, IntPtr.Zero);

        // Windows 上 OpenSSL 无法自动找到系统 CA 证书，需要从 Windows 证书存储导出并加载
        await LoadWindowsRootCertsAsync(sslContext).ConfigureAwait(false);

        using var ssl = OpenSSLNative.SSL_new(sslContext);
        if (ssl.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 SSL 对象。");
        }

        OpenSSLNative.SSL_set_tlsext_host_name(ssl, host);
        OpenSSLNative.SSL_set1_host(ssl, host);

        // OpenSSL BIO_new_socket 需要原生 SOCKET（IntPtr），在 Windows 64 位上是 8 字节。
        var rawSocketHandle = socket.Handle;
        using var bio = OpenSSLNative.BIO_new_socket(rawSocketHandle, 0);
        if (bio.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 BIO 对象。");
        }

        // SSL_set_bio 会将 BIO 所有权转移给 SSL 对象，SSL_free 时会同时释放 BIO。
        // 因此 SafeBioHandle 不应再次释放 BIO。
        OpenSSLNative.SSL_set_bio(ssl, bio, bio);
        bio.MarkAsInvalid();

        var connectResult = OpenSSLNative.SSL_connect(ssl);
        if (connectResult != 1)
        {
            var error = OpenSSLNative.SSL_get_error(ssl, connectResult);
            var errCode = OpenSSLNative.ERR_get_error();
            var errStr = errCode != 0 ? OpenSSLNative.GetErrorString(errCode) : $"SSL 错误码: {error}";
            throw new InvalidOperationException($"TLS 握手失败: {errStr}");
        }

        try
        {
            var requestBytes = await BuildHttpRequestAsync(request, host, port, path).ConfigureAwait(false);

            int written;
                        unsafe
                        {
                            fixed (byte* ptr = requestBytes)
                            {
                                written = OpenSSLNative.SSL_write(ssl, ptr, requestBytes.Length);
                            }
                        }
            if (written <= 0)
            {
                var error = OpenSSLNative.SSL_get_error(ssl, written);
                throw new InvalidOperationException($"发送 HTTP 请求失败，SSL 错误码: {error}");
            }

            var responseBytes = await ReadResponseAsync(ssl, cancellationToken).ConfigureAwait(false);

            return ParseHttpResponse(responseBytes);
        }
        finally
        {
            OpenSSLNative.SSL_shutdown(ssl);
        }
    }

    private static async Task<byte[]> BuildHttpRequestAsync(HttpRequestMessage request, string host, int port, string path)
    {
        byte[]? bodyBytes = null;
        if (request.Content is not null)
        {
            bodyBytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        await using var ms = new MemoryStream(256 + (bodyBytes?.Length ?? 0));
        await using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 256, leaveOpen: true))
        {
            writer.Write(request.Method.Method);
            writer.Write(' ');
            writer.Write(path);
            writer.Write(" HTTP/1.1\r\n");

            writer.Write("Host: ");
            writer.Write(host);
            if (port != 443)
            {
                writer.Write(':');
                writer.Write(port);
            }
            writer.Write("\r\n");

            writer.Write("Connection: close\r\n");

            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    writer.Write(header.Key);
                    writer.Write(": ");
                    writer.Write(value);
                    writer.Write("\r\n");
                }
            }

            if (bodyBytes is not null)
            {
                if (request.Content!.Headers.ContentType is not null)
                {
                    writer.Write("Content-Type: ");
                    writer.Write(request.Content.Headers.ContentType.ToString());
                    writer.Write("\r\n");
                }

                writer.Write("Content-Length: ");
                writer.Write(bodyBytes.Length);
                writer.Write("\r\n");
            }

            writer.Write("\r\n");
            await writer.FlushAsync().ConfigureAwait(false);
        }

        if (bodyBytes is not null && bodyBytes.Length > 0)
        {
            ms.Write(bodyBytes, 0, bodyBytes.Length);
        }

        var result = ms.ToArray();
        return result;
    }

    private static async Task<byte[]> ReadResponseAsync(SafeSslHandle ssl, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(4096);
        var readBuffer = new byte[4096];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bytesRead;
            unsafe
            {
                fixed (byte* ptr = readBuffer)
                {
                    bytesRead = OpenSSLNative.SSL_read(ssl, ptr, readBuffer.Length);
                }
            }
            var error = OpenSSLNative.SSL_get_error(ssl, bytesRead);

            if (bytesRead > 0)
            {
                buffer.Write(readBuffer, 0, bytesRead);
                continue;
            }

            if (error == OpenSSLNative.SSL_ERROR_ZERO_RETURN)
            {
                break;
            }

            if (error == OpenSSLNative.SSL_ERROR_WANT_READ || error == OpenSSLNative.SSL_ERROR_WANT_WRITE)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (error == OpenSSLNative.SSL_ERROR_SYSCALL && bytesRead == 0)
            {
                break;
            }

            break;
        }

        return buffer.ToArray();
    }

    private static HttpResponseMessage ParseHttpResponse(byte[] rawResponse)
    {
        // 在字节层面查找 \r\n\r\n 分隔 header 和 body，避免全量字符串分配
        var headerEndIndex = FindHeaderEnd(rawResponse);
        if (headerEndIndex < 0)
        {
            throw new InvalidOperationException("无法解析 HTTP 响应：未找到头部结束标记。");
        }

        var headersSection = Encoding.UTF8.GetString(rawResponse, 0, headerEndIndex);
        var bodyBytes = rawResponse.AsSpan(headerEndIndex + 4).ToArray();

        var lines = headersSection.Split("\r\n", StringSplitOptions.None);
        if (lines.Length == 0)
        {
            throw new InvalidOperationException("无法解析 HTTP 响应：状态行缺失。");
        }

        var statusLine = lines[0].Split(' ', 3);
        if (statusLine.Length < 2 || !int.TryParse(statusLine[1], out var statusCode))
        {
            throw new InvalidOperationException($"无法解析 HTTP 状态行: {lines[0]}");
        }

        var response = new HttpResponseMessage((HttpStatusCode)statusCode)
        {
            ReasonPhrase = statusLine.Length > 2 ? statusLine[2] : null
        };

        for (var i = 1; i < lines.Length; i++)
        {
            var colonIndex = lines[i].IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var name = lines[i][..colonIndex].Trim();
            var value = lines[i][(colonIndex + 1)..].Trim();

            if (!response.Headers.TryAddWithoutValidation(name, value))
            {
                response.Content?.Headers.TryAddWithoutValidation(name, value);
            }
        }

        response.Content = new ByteArrayContent(bodyBytes);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

        return response;
    }

    /// <summary>
    /// 在字节数组中查找 "\r\n\r\n" 序列的起始索引。
    /// </summary>
    private static int FindHeaderEnd(byte[] data)
    {
        for (var i = 0; i < data.Length - 3; i++)
        {
            if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 从 Windows 证书存储导出受信任的根证书到临时 PEM 文件，并加载到 OpenSSL 上下文。
    /// 首次调用时导出并缓存，后续调用复用缓存。
    /// </summary>
    private async Task LoadWindowsRootCertsAsync(SafeSslContextHandle sslContext)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (_rootCertsCache is not null)
        {
            LoadCertsFromCache(sslContext);
            return;
        }

        var tempFiles = await ExportWindowsRootCertsAsync().ConfigureAwait(false);
        if (tempFiles is null || tempFiles.Count == 0)
        {
            return;
        }

        // 多线程重入时，多个实例可能同时导出，但最终只有一个缓存被保留。
        // 这不会导致业务问题，因为所有导出的证书内容相同。
        _rootCertsCache = new WindowsRootCertsCache(tempFiles);
        LoadCertsFromCache(sslContext);
    }

    private void LoadCertsFromCache(SafeSslContextHandle sslContext)
    {
        foreach (var tempFile in _rootCertsCache!.TempFilePaths)
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
                    var pem = OpenSslCertificateLoader.ExportCertificateAsPem(cert);
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
}
