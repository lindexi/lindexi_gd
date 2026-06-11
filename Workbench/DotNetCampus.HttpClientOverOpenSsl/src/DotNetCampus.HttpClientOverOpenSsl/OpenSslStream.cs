using System.Net.Sockets;
using System.Runtime.InteropServices;
using DotNetCampus.HttpClientOverOpenSsl.Interop;

namespace DotNetCampus.HttpClientOverOpenSsl;

/// <summary>
/// 基于 OpenSSL 原生 DLL 实现的 TLS 加密流，通过 P/Invoke 调用 libssl-3 完成 TLS 握手和数据传输。
/// 可替代 <see cref="System.Net.Security.SslStream"/> 用于 <c>SocketsHttpHandler.ConnectCallback</c> 场景。
/// </summary>
internal sealed class OpenSslStream : Stream
{
    private static readonly bool s_initialized;

    private readonly Socket _socket;
    private readonly bool _ownsSocket;
    private SafeSslContextHandle? _sslContext;
    private SafeSslHandle? _ssl;
    private bool _isAuthenticated;
    private bool _disposed;

    static OpenSslStream()
    {
        s_initialized = OpenSSLNative.OPENSSL_init_ssl(
            OpenSSLNative.OPENSSL_INIT_LOAD_SSL_STRINGS | OpenSSLNative.OPENSSL_INIT_LOAD_CRYPTO_STRINGS,
            IntPtr.Zero) == 1;
    }

    /// <summary>
    /// 使用指定的 Socket 创建 <see cref="OpenSslStream"/> 实例。
    /// </summary>
    /// <param name="socket">已连接的 TCP Socket。</param>
    /// <param name="ownsSocket">是否在释放流时同时释放 Socket。</param>
    public OpenSslStream(Socket socket, bool ownsSocket = false)
    {
        ArgumentNullException.ThrowIfNull(socket);

        if (!s_initialized)
        {
            throw new InvalidOperationException("OpenSSL 初始化失败。");
        }

        _socket = socket;
        _ownsSocket = ownsSocket;
    }

    /// <summary>
    /// 执行 TLS 客户端握手。
    /// </summary>
    /// <param name="options">认证配置选项。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task AuthenticateAsClientAsync(OpenSslClientAuthenticationOptions options, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        ArgumentNullException.ThrowIfNull(options);

        var host = options.TargetHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("目标主机名不能为空。", nameof(options));
        }

        _sslContext = OpenSSLNative.SSL_CTX_new(OpenSSLNative.TLS_client_method());
        if (_sslContext.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 SSL 上下文。");
        }

        OpenSSLNative.SSL_CTX_set_default_verify_paths(_sslContext);
        OpenSSLNative.SSL_CTX_set_verify(_sslContext, OpenSSLNative.SSL_VERIFY_PEER, IntPtr.Zero);

        await OpenSslCertificateLoader.LoadWindowsRootCertsAsync(_sslContext).ConfigureAwait(false);

        _ssl = OpenSSLNative.SSL_new(_sslContext);
        if (_ssl.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 SSL 对象。");
        }

        OpenSSLNative.SSL_set_tlsext_host_name(_ssl, host);
        OpenSSLNative.SSL_set1_host(_ssl, host);

        // OpenSSL BIO_new_socket 需要原生 SOCKET（IntPtr），在 Windows 64 位上是 8 字节。
        // close_flag=0 表示 BIO 不接管 Socket 的生命周期，由 OpenSslStream 自行管理。
        var rawSocketHandle = _socket.Handle;
        using var bio = OpenSSLNative.BIO_new_socket(rawSocketHandle, 0);
        if (bio.IsInvalid)
        {
            throw new InvalidOperationException("无法创建 BIO 对象。");
        }

        // SSL_set_bio 会将 BIO 所有权转移给 SSL 对象，SSL_free 时会同时释放 BIO。
        // 因此 SafeBioHandle 不应再次释放 BIO。
        OpenSSLNative.SSL_set_bio(_ssl, bio, bio);
        bio.MarkAsInvalid();

        // TLS 握手涉及多次网络往返，使用 Task.Run 卸载到线程池避免阻塞调用方线程。
        var connectResult = await Task.Run(() => OpenSSLNative.SSL_connect(_ssl), cancellationToken).ConfigureAwait(false);
        if (connectResult != 1)
        {
            var error = OpenSSLNative.SSL_get_error(_ssl, connectResult);
            var errCode = OpenSSLNative.ERR_get_error();
            var errStr = errCode != 0 ? OpenSSLNative.GetErrorString(errCode) : $"SSL 错误码: {error}";
            throw new OpenSslException($"TLS 握手失败: {errStr}", error, errCode);
        }

        _isAuthenticated = true;
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        ThrowIfNotAuthenticated();
        ValidateBufferArgs(buffer, offset, count);

        if (count == 0)
        {
            return 0;
        }

        int bytesRead;
        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                bytesRead = OpenSSLNative.SSL_read(_ssl!, ptr + offset, count);
            }
        }

        if (bytesRead > 0)
        {
            return bytesRead;
        }

        var error = OpenSSLNative.SSL_get_error(_ssl!, bytesRead);

        if (error == OpenSSLNative.SSL_ERROR_ZERO_RETURN)
        {
            return 0;
        }

        if (error == OpenSSLNative.SSL_ERROR_SYSCALL && bytesRead == 0)
        {
            return 0;
        }

        throw new OpenSslException($"SSL_read 失败，错误码: {error}", error);
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        ThrowIfNotAuthenticated();
        ValidateBufferArgs(buffer, offset, count);

        if (count == 0)
        {
            return 0;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int bytesRead;
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject() + offset;
                bytesRead = await Task.Run(
                    () =>
                    {
                        unsafe
                        {
                            return OpenSSLNative.SSL_read(_ssl!, (byte*)ptr, count);
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                handle.Free();
            }

            if (bytesRead > 0)
            {
                return bytesRead;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl!, bytesRead);

            if (error == OpenSSLNative.SSL_ERROR_ZERO_RETURN)
            {
                return 0;
            }

            if (error == OpenSSLNative.SSL_ERROR_SYSCALL && bytesRead == 0)
            {
                return 0;
            }

            if (error == OpenSSLNative.SSL_ERROR_WANT_READ || error == OpenSSLNative.SSL_ERROR_WANT_WRITE)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            throw new OpenSslException($"SSL_read 失败，错误码: {error}", error);
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        ThrowIfNotAuthenticated();
        ValidateBufferArgs(buffer, offset, count);

        if (count == 0)
        {
            return;
        }

        int written;
        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                written = OpenSSLNative.SSL_write(_ssl!, ptr + offset, count);
            }
        }

        if (written <= 0)
        {
            var error = OpenSSLNative.SSL_get_error(_ssl!, written);
            throw new OpenSslException($"SSL_write 失败，错误码: {error}", error);
        }
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        ThrowIfNotAuthenticated();
        ValidateBufferArgs(buffer, offset, count);

        if (count == 0)
        {
            return;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int written;
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                var ptr = handle.AddrOfPinnedObject() + offset;
                written = await Task.Run(
                    () =>
                    {
                        unsafe
                        {
                            return OpenSSLNative.SSL_write(_ssl!, (byte*)ptr, count);
                        }
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                handle.Free();
            }

            if (written > 0)
            {
                return;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl!, written);

            if (error == OpenSSLNative.SSL_ERROR_WANT_READ || error == OpenSSLNative.SSL_ERROR_WANT_WRITE)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            throw new OpenSslException($"SSL_write 失败，错误码: {error}", error);
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // TLS 层无缓冲，无需额外操作。
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            if (_ssl is not null)
            {
                OpenSSLNative.SSL_shutdown(_ssl);
                _ssl.Dispose();
                _ssl = null;
            }

            _sslContext?.Dispose();
            _sslContext = null;

            if (_ownsSocket)
            {
                _socket.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ThrowIfNotAuthenticated()
    {
        if (!_isAuthenticated)
        {
            throw new InvalidOperationException("TLS 握手尚未完成，请先调用 AuthenticateAsClientAsync。");
        }
    }

    private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (offset + count > buffer.Length)
        {
            throw new ArgumentException("偏移量和计数超出缓冲区范围。");
        }
    }
}
