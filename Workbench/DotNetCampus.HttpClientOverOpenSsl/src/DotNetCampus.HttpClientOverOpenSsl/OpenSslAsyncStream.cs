using System.Net.Sockets;
using DotNetCampus.HttpClientOverOpenSsl.Interop;

namespace DotNetCampus.HttpClientOverOpenSsl;

/// <summary>
/// 基于非阻塞 Socket 的 OpenSSL TLS 加密流，通过 P/Invoke 调用 libssl-3 完成 TLS 握手和数据传输。
/// </summary>
/// <remarks>
/// <para><b>与 <see cref="OpenSslStream"/>（Task.Run 方案）的区别：</b></para>
/// <para><see cref="OpenSslStream"/> 将 <c>SSL_read</c>/<c>SSL_write</c>/<c>SSL_connect</c> 通过
/// <c>Task.Run</c> 卸载到线程池执行，Socket 保持默认阻塞模式。优点是实现简单，缺点是线程池
/// 线程被长时间阻塞在 I/O 上，高并发下可能导致线程池饥饿。</para>
///
/// <para><b>本类的实现方案（非阻塞 Socket + 原生异步 I/O 等待）：</b></para>
///
/// <para><b>1. 核心思路</b></para>
/// <para>构造时将 <c>_socket.Blocking = false</c> 设为非阻塞模式。之后所有
/// <c>SSL_read</c>/<c>SSL_write</c>/<c>SSL_connect</c> 调用都在当前线程同步执行：</para>
/// <list type="bullet">
///   <item>若操作立刻完成（返回 &gt; 0），直接返回结果，零开销。</item>
///   <item>若内核缓冲区未就绪，OpenSSL 返回 &lt;= 0，此时调用 <c>SSL_get_error</c> 获取：
///     <list type="bullet">
///       <item><c>SSL_ERROR_WANT_READ</c> — SSL 需要更多入站数据（Socket 可读）</item>
///       <item><c>SSL_ERROR_WANT_WRITE</c> — SSL 需要发送出站数据（Socket 可写）</item>
///     </list>
///   </item>
/// </list>
///
/// <para><b>2. 零长度异步等待（避免忙等）</b></para>
/// <para>当收到 <c>WANT_READ</c> 或 <c>WANT_WRITE</c> 时，不调用 <c>Task.Delay</c> 忙等，而是：</para>
/// <list type="bullet">
///   <item><c>WANT_READ</c> → <c>await _socket.ReceiveAsync(Memory&lt;byte&gt;.Empty, ...)</c></item>
///   <item><c>WANT_WRITE</c> → <c>await _socket.SendAsync(ReadOnlyMemory&lt;byte&gt;.Empty, ...)</c></item>
/// </list>
/// <para>零长度的 <c>ReceiveAsync</c>/<c>SendAsync</c> 不会读写任何数据，只等待 Socket 变为
/// 可读/可写状态。底层使用 IOCP（Windows）或 epoll（Linux）的异步完成端口，等待期间
/// <b>不占用任何线程</b>。Signal 后回到原调用方线程继续循环。</para>
///
/// <para><b>3. SSL_MODE 设置</b></para>
/// <para>握手前调用 <c>SSL_set_mode</c> 设置两个标志位：</para>
/// <list type="bullet">
///   <item><c>SSL_MODE_ENABLE_PARTIAL_WRITE</c> — 允许 <c>SSL_write</c> 仅发送一个 TLS
///   记录即返回（而非等全部数据发送完毕），避免非阻塞模式下写操作被误判为失败。</item>
///   <item><c>SSL_MODE_ACCEPT_MOVING_WRITE_BUFFER</c> — 允许写缓冲区在重试时移动位置，
///   这样每次重试可以传入不同的 <c>tempBuffer</c> 实例，无需固定 pin 住内存。</item>
/// </list>
///
/// <para><b>4. 同步 Read/Write 的兼容处理</b></para>
/// <para>同步的 <see cref="Read"/> 和 <see cref="Write"/> 方法内临时设置
/// <c>_socket.Blocking = true</c>，使 <c>SSL_read</c>/<c>SSL_write</c> 能正常阻塞等待，
/// 完成后在 <c>finally</c> 中恢复非阻塞模式。这保证了 Stream 基类的同步使用者不依赖
/// 调用方线程模型。</para>
///
/// <para><b>5. 与 Task.Run 方案的对比</b></para>
/// <list type="table">
///   <listheader>
///     <term>维度</term>
///     <description>Task.Run（OpenSslStream）</description>
///     <description>本类（OpenSslAsyncStream）</description>
///   </listheader>
///   <item>
///     <term>线程占用</term>
///     <description>每个 I/O 操作占用 1 个线程池线程阻塞等待</description>
///     <description>I/O 等待期间零线程占用（IOCP/epoll）</description>
///   </item>
///   <item>
///     <term>高并发表现</term>
///     <description>线程池可能饥饿，吞吐下降</description>
///     <description>仅受内存和 CPU 限制</description>
///   </item>
///   <item>
///     <term>实现复杂度</term>
///     <description>简单</description>
///     <description>中等（需处理非阻塞重试循环）</description>
///   </item>
///   <item>
///     <term>同步 Read/Write</term>
///     <description>天然支持（Socket 阻塞模式）</description>
///     <description>临时切回阻塞模式（有轻微开销）</description>
///   </item>
/// </list>
/// </remarks>
internal sealed class OpenSslAsyncStream : Stream
{
    private readonly Socket _socket;
    private readonly bool _ownsSocket;
    private SafeSslContextHandle? _sslContext;
    private SafeSslHandle? _ssl;
    private bool _isAuthenticated;
    private bool _disposed;

    /// <summary>
    /// 使用指定的 Socket 创建 <see cref="OpenSslAsyncStream"/> 实例。
    /// </summary>
    /// <param name="socket">已连接的 TCP Socket。</param>
    /// <param name="ownsSocket">是否在释放流时同时释放 Socket。</param>
    public OpenSslAsyncStream(Socket socket, bool ownsSocket = false)
    {
        ArgumentNullException.ThrowIfNull(socket);

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
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(options);

        var host = options.TargetHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("目标主机名不能为空。", nameof(options));
        }

        // 延迟初始化 OpenSSL，避免静态构造函数过早加载原生 DLL。
        // OPENSSL_init_ssl 幂等，可多次调用。
        if (OpenSSLNative.OPENSSL_init_ssl(
                OpenSSLNative.OPENSSL_INIT_LOAD_SSL_STRINGS | OpenSSLNative.OPENSSL_INIT_LOAD_CRYPTO_STRINGS,
                IntPtr.Zero) != 1)
        {
            throw new InvalidOperationException("OpenSSL 初始化失败。");
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

                        if (await TryHandleSslWantAsync(error, cancellationToken).ConfigureAwait(false))
                        {
                            continue;
                        }

                        var errCode = OpenSSLNative.ERR_get_error();
                        var errStr = errCode != 0 ? OpenSSLNative.GetErrorString(errCode) : $"SSL 错误码: {error}";
                        throw new OpenSslException($"TLS 握手失败: {errStr}", error, errCode);
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
        ThrowIfDisposed();
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
        finally
        {
            _socket.Blocking = false;
        }
    }

    /// <inheritdoc />
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
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

                        switch (error)
                        {
                            case OpenSSLNative.SSL_ERROR_ZERO_RETURN:
                                return 0;

                            case OpenSSLNative.SSL_ERROR_SYSCALL when bytesRead == 0:
                                return 0;
                        }

                        if (await TryHandleSslWantAsync(error, cancellationToken).ConfigureAwait(false))
                        {
                            continue;
                        }

                        throw new OpenSslException($"SSL_read 失败，错误码: {error}", error);
        }
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
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
        finally
        {
            _socket.Blocking = false;
        }
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
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
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    written = OpenSSLNative.SSL_write(_ssl!, ptr + offset, count);
                }
            }

            if (written > 0)
            {
                return;
            }

            var error = OpenSSLNative.SSL_get_error(_ssl!, written);

                        if (await TryHandleSslWantAsync(error, cancellationToken).ConfigureAwait(false))
                        {
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
    /// 处理 SSL 操作的 WANT_READ/WANT_WRITE 状态。若错误码为 WANT_READ 或 WANT_WRITE，
    /// 则异步等待 Socket 就绪并返回 <c>true</c>（调用方应重试 SSL 操作）；否则返回 <c>false</c>。
    /// </summary>
    /// <param name="sslError"><see cref="OpenSSLNative.SSL_get_error"/> 返回的错误码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>若已处理等待并应重试，返回 <c>true</c>；否则返回 <c>false</c>，调用方自行处理错误。</returns>
    private async ValueTask<bool> TryHandleSslWantAsync(int sslError, CancellationToken cancellationToken)
    {
        switch (sslError)
        {
            case OpenSSLNative.SSL_ERROR_WANT_READ:
                await WaitForSocketReadAsync(cancellationToken).ConfigureAwait(false);
                return true;

            case OpenSSLNative.SSL_ERROR_WANT_WRITE:
                await WaitForSocketWriteAsync(cancellationToken).ConfigureAwait(false);
                return true;

            default:
                return false;
        }
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

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
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
