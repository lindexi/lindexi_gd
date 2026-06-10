using System.Net.Sockets;
using JucojocuNeficawhurholee.Interop;

namespace JucojocuNeficawhurholee;

/// <summary>
/// 基于非阻塞 Socket 的 OpenSSL TLS 加密流，通过 P/Invoke 调用 libssl-3 完成 TLS 握手和数据传输。
/// 与 <see cref="OpenSslStream"/> 的区别：不使用 <c>Task.Run</c> 占用线程池线程，
/// 而是将 Socket 设为非阻塞模式，当 OpenSSL 返回 <c>SSL_ERROR_WANT_READ</c>/<c>SSL_ERROR_WANT_WRITE</c> 时，
/// 通过 .NET 原生异步 IO（零长度 <see cref="Socket.ReceiveAsync(Memory{byte}, SocketFlags, CancellationToken)"/>/
/// <see cref="Socket.SendAsync(ReadOnlyMemory{byte}, SocketFlags, CancellationToken)"/>）等待 Socket 就绪后重试。
/// </summary>
internal sealed class OpenSslAsyncStream : Stream
{
    private static readonly bool s_initialized;

    private readonly Socket _socket;
    private readonly bool _ownsSocket;
    private SafeSslContextHandle? _sslContext;
    private SafeSslHandle? _ssl;
    private bool _isAuthenticated;
    private bool _disposed;

    static OpenSslAsyncStream()
    {
        s_initialized = OpenSSLNative.OPENSSL_init_ssl(
            OpenSSLNative.OPENSSL_INIT_LOAD_SSL_STRINGS | OpenSSLNative.OPENSSL_INIT_LOAD_CRYPTO_STRINGS,
            IntPtr.Zero) == 1;
    }

    /// <summary>
    /// 使用指定的 Socket 创建 <see cref="OpenSslAsyncStream"/> 实例。
    /// </summary>
    /// <param name="socket">已连接的 TCP Socket。</param>
    /// <param name="ownsSocket">是否在释放流时同时释放 Socket。</param>
    public OpenSslAsyncStream(Socket socket, bool ownsSocket = false)
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
    /// 执行 TLS 客户端握手。使用非阻塞 Socket + .NET 异步等待，不占用线程池线程。
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

        // 启用部分写入和移动写缓冲区模式，避免写操作需要连续重试同一缓冲区。
        OpenSSLNative.SSL_set_mode(_ssl,
            OpenSSLNative.SSL_MODE_ENABLE_PARTIAL_WRITE |
            OpenSSLNative.SSL_MODE_ACCEPT_MOVING_WRITE_BUFFER);

        // OpenSSL BIO_new_socket 需要原生 SOCKET（IntPtr），在 Windows 64 位上是 8 字节。
        // close_flag=0 表示 BIO 不接管 Socket 的生命周期，由 OpenSslAsyncStream 自行管理。
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

        // 将 Socket 设为非阻塞模式，后续 SSL_connect 遇到 WANT_READ/WANT_WRITE 时
        // 通过 .NET 异步 IO 等待 Socket 就绪，而非占用线程池线程阻塞等待。
        _socket.Blocking = false;

        // TLS 握手涉及多次网络往返，通过非阻塞循环 + 异步 Socket 等待完成。
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connectResult = OpenSSLNative.SSL_connect(_ssl);
            if (connectResult == 1)
            {
                _isAuthenticated = true;
                return;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl, connectResult);

            switch (error)
            {
                case OpenSSLNative.SSL_ERROR_WANT_READ:
                    await WaitForSocketReadAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case OpenSSLNative.SSL_ERROR_WANT_WRITE:
                    await WaitForSocketWriteAsync(cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    var errCode = OpenSSLNative.ERR_get_error();
                    var errStr = errCode != 0 ? OpenSSLNative.GetErrorString(errCode) : $"SSL 错误码: {error}";
                    throw new OpenSslException($"TLS 握手失败: {errStr}", error, errCode);
            }
        }
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

        // 同步 Read 临时恢复阻塞模式，确保 SSL_read 能阻塞等待数据。
        _socket.Blocking = true;
        try
        {
            var tempBuffer = new byte[count];
            var bytesRead = OpenSSLNative.SSL_read(_ssl!, tempBuffer, count);

            if (bytesRead > 0)
            {
                Buffer.BlockCopy(tempBuffer, 0, buffer, offset, bytesRead);
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
        finally
        {
            _socket.Blocking = false;
        }
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

        var tempBuffer = new byte[count];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = OpenSSLNative.SSL_read(_ssl!, tempBuffer, count);

            if (bytesRead > 0)
            {
                Buffer.BlockCopy(tempBuffer, 0, buffer, offset, bytesRead);
                return bytesRead;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl!, bytesRead);

            switch (error)
            {
                case OpenSSLNative.SSL_ERROR_ZERO_RETURN:
                    return 0;

                case OpenSSLNative.SSL_ERROR_SYSCALL when bytesRead == 0:
                    return 0;

                case OpenSSLNative.SSL_ERROR_WANT_READ:
                    await WaitForSocketReadAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case OpenSSLNative.SSL_ERROR_WANT_WRITE:
                    await WaitForSocketWriteAsync(cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new OpenSslException($"SSL_read 失败，错误码: {error}", error);
            }
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

        // 同步 Write 临时恢复阻塞模式，确保 SSL_write 能阻塞等待发送完成。
        _socket.Blocking = true;
        try
        {
            var tempBuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, tempBuffer, 0, count);

            var written = OpenSSLNative.SSL_write(_ssl!, tempBuffer, count);
            if (written <= 0)
            {
                var error = OpenSSLNative.SSL_get_error(_ssl!, written);
                throw new OpenSslException($"SSL_write 失败，错误码: {error}", error);
            }
        }
        finally
        {
            _socket.Blocking = false;
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

        var tempBuffer = new byte[count];
        Buffer.BlockCopy(buffer, offset, tempBuffer, 0, count);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var written = OpenSSLNative.SSL_write(_ssl!, tempBuffer, count);

            if (written > 0)
            {
                return;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl!, written);

            switch (error)
            {
                case OpenSSLNative.SSL_ERROR_WANT_READ:
                    await WaitForSocketReadAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case OpenSSLNative.SSL_ERROR_WANT_WRITE:
                    await WaitForSocketWriteAsync(cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new OpenSslException($"SSL_write 失败，错误码: {error}", error);
            }
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
                // SSL_shutdown 前临时恢复阻塞模式，确保关闭握手能完成。
                _socket.Blocking = true;
                OpenSSLNative.SSL_shutdown(_ssl);
                _socket.Blocking = false;

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

    /// <summary>
    /// 异步等待 Socket 变为可读状态。通过零长度 <see cref="Socket.ReceiveAsync(Memory{byte}, SocketFlags, CancellationToken)"/>
    /// 实现，不会占用线程池线程。
    /// </summary>
    private ValueTask<int> WaitForSocketReadAsync(CancellationToken cancellationToken)
    {
        return _socket.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None, cancellationToken);
    }

    /// <summary>
    /// 异步等待 Socket 变为可写状态。通过零长度 <see cref="Socket.SendAsync(ReadOnlyMemory{byte}, SocketFlags, CancellationToken)"/>
    /// 实现，不会占用线程池线程。
    /// </summary>
    private ValueTask<int> WaitForSocketWriteAsync(CancellationToken cancellationToken)
    {
        return _socket.SendAsync(ReadOnlyMemory<byte>.Empty, SocketFlags.None, cancellationToken);
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
