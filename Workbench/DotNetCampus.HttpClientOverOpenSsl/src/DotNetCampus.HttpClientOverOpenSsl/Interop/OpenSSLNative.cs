using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace DotNetCampus.HttpClientOverOpenSsl.Interop;

/// <summary>
/// OpenSSL P/Invoke declarations and SafeHandle wrappers.
/// Uses <see cref="NativeLibrary.SetDllImportResolver"/> to dynamically resolve
/// architecture-specific DLL names (x86: libssl-3.dll, x64: libssl-3-x64.dll, arm64: libssl-3-arm64.dll).
/// </summary>
internal static class OpenSSLNative
{
    // DllImport 必须用编译时常量，这里使用 x86 名称作为标识符，
    // 实际加载通过下面的 resolver 按架构映射到正确文件名。
    private const string LibSslConst = "libssl-3.dll";
    private const string LibCryptoConst = "libcrypto-3.dll";

    /// <summary>
    /// 业务方可设置的回退查找文件夹路径。当默认搜索路径和 NuGet runtimes 目录
    /// 都找不到原生 DLL 时，会在此路径下查找。支持插件式加载场景。
    /// </summary>
    public static string? FallbackLibraryPath { get; set; }

    static OpenSSLNative()
    {
        var archSuffix = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "",
            Architecture.X64 => "-x64",
            Architecture.Arm64 => "-arm64",
            _ => throw new PlatformNotSupportedException(
                $"不支持的 CPU 架构: {RuntimeInformation.ProcessArchitecture}")
        };

        var actualSsl = string.IsNullOrEmpty(archSuffix)
            ? "libssl-3.dll"
            : $"libssl-3{archSuffix}.dll";
        var actualCrypto = string.IsNullOrEmpty(archSuffix)
            ? "libcrypto-3.dll"
            : $"libcrypto-3{archSuffix}.dll";

        var rid = RuntimeInformation.RuntimeIdentifier;

        IntPtr cachedSslHandle = IntPtr.Zero;
        IntPtr cachedCryptoHandle = IntPtr.Zero;

        NativeLibrary.SetDllImportResolver(typeof(OpenSSLNative).Assembly, MapAndLoad);

        IntPtr MapAndLoad(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // 提前计算归属，避免多处重复字符串比较
            var isSsl = string.Equals(libraryName, LibSslConst, StringComparison.OrdinalIgnoreCase);
            var isCrypto = string.Equals(libraryName, LibCryptoConst, StringComparison.OrdinalIgnoreCase);

            // 只处理已知的两个库，其他一律返回 0
            if (!isSsl && !isCrypto)
            {
                return IntPtr.Zero;
            }

            // 检查缓存
            ref var cachedHandle = ref isSsl
                ? ref cachedSslHandle
                : ref cachedCryptoHandle;

            if (cachedHandle != IntPtr.Zero)
            {
                return cachedHandle;
            }

            var mappedName = isSsl ? actualSsl : isCrypto ? actualCrypto : libraryName;

            // 1. AppContext.BaseDirectory 下带架构后缀的名称（如 libssl-3-x64.dll）
            var baseDirPath = Path.Join(AppContext.BaseDirectory, mappedName);
            if (File.Exists(baseDirPath) && NativeLibrary.TryLoad(baseDirPath, out var handle))
            {
                cachedHandle = handle;
                return handle;
            }

            // 2. AppContext.BaseDirectory 下不带架构后缀的原始名称（如 libssl-3.dll）
            if (!string.Equals(mappedName, libraryName, StringComparison.OrdinalIgnoreCase))
            {
                var baseDirOriginalPath = Path.Join(AppContext.BaseDirectory, libraryName);
                if (File.Exists(baseDirOriginalPath) && NativeLibrary.TryLoad(baseDirOriginalPath, out handle))
                {
                    cachedHandle = handle;
                    return handle;
                }
            }

            // 3. NuGet runtimes 目录（openssl-native 包放置 DLL 的位置）
            var runtimesPath = Path.Join(AppContext.BaseDirectory, "runtimes", rid, "native", mappedName);
            if (File.Exists(runtimesPath) && NativeLibrary.TryLoad(runtimesPath, out handle))
            {
                cachedHandle = handle;
                return handle;
            }

            if (rid.Contains("win10-", StringComparison.OrdinalIgnoreCase))
            {
                var winRid = rid.Replace("win10-", "win-");
                var winRuntimesPath = Path.Join(AppContext.BaseDirectory, "runtimes", winRid, "native", mappedName);
                if (File.Exists(winRuntimesPath) && NativeLibrary.TryLoad(winRuntimesPath, out handle))
                {
                    cachedHandle = handle;
                    return handle;
                }
            }

            // 4. 回退文件夹（业务方通过 FallbackLibraryPath 配置的插件路径）
            var fallbackPath = FallbackLibraryPath;
            if (!string.IsNullOrWhiteSpace(fallbackPath))
            {
                // 4a. 带架构后缀的名称
                var fallbackFilePath = Path.Join(fallbackPath, mappedName);
                if (File.Exists(fallbackFilePath) && NativeLibrary.TryLoad(fallbackFilePath, out handle))
                {
                    cachedHandle = handle;
                    return handle;
                }

                // 4b. 不带架构后缀的原始名称
                if (!string.Equals(mappedName, libraryName, StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackOriginalPath = Path.Join(fallbackPath, libraryName);
                    if (File.Exists(fallbackOriginalPath) && NativeLibrary.TryLoad(fallbackOriginalPath, out handle))
                    {
                        cachedHandle = handle;
                        return handle;
                    }
                }
            }

            return IntPtr.Zero;
        }
    }

    #region Constants

    public const int SSL_VERIFY_NONE = 0x00;
    public const int SSL_VERIFY_PEER = 0x01;

    public const int SSL_ERROR_NONE = 0;
    public const int SSL_ERROR_SSL = 1;
    public const int SSL_ERROR_WANT_READ = 2;
    public const int SSL_ERROR_WANT_WRITE = 3;
    public const int SSL_ERROR_SYSCALL = 5;
    public const int SSL_ERROR_ZERO_RETURN = 6;
    public const int SSL_ERROR_WANT_CONNECT = 7;
    public const int SSL_ERROR_WANT_ACCEPT = 8;

    public const ulong OPENSSL_INIT_LOAD_SSL_STRINGS = 0x00200000UL;
    public const ulong OPENSSL_INIT_LOAD_CRYPTO_STRINGS = 0x00000002UL;

    public const int SSL_CTRL_SET_TLSEXT_HOSTNAME = 55;
    public const int TLSEXT_NAMETYPE_host_name = 0;

    public const int SSL_CTRL_MODE = 33;
    public const uint SSL_MODE_ENABLE_PARTIAL_WRITE = 0x00000001U;
    public const uint SSL_MODE_ACCEPT_MOVING_WRITE_BUFFER = 0x00000002U;

    #endregion

    #region Initialization

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int OPENSSL_init_ssl(ulong opts, IntPtr settings);

    #endregion

    #region SSL Context

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TLS_client_method();

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern SafeSslContextHandle SSL_CTX_new(IntPtr method);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SSL_CTX_free(IntPtr ctx);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_CTX_set_default_verify_paths(SafeSslContextHandle ctx);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_CTX_load_verify_file(SafeSslContextHandle ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string CAfile);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_CTX_load_verify_locations(SafeSslContextHandle ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string CAfile, [MarshalAs(UnmanagedType.LPUTF8Str)] string? CApath);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SSL_CTX_set_verify(SafeSslContextHandle ctx, int mode, IntPtr callback);

    #endregion

    #region SSL Object

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern SafeSslHandle SSL_new(SafeSslContextHandle ctx);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SSL_free(IntPtr ssl);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_connect(SafeSslHandle ssl);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int SSL_write(SafeSslHandle ssl, byte* buf, int num);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int SSL_read(SafeSslHandle ssl, byte* buf, int num);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_shutdown(SafeSslHandle ssl);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_get_error(SafeSslHandle ssl, int ret);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SSL_set_bio(SafeSslHandle ssl, SafeBioHandle rbio, SafeBioHandle wbio);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SSL_set1_host(SafeSslHandle ssl, [MarshalAs(UnmanagedType.LPUTF8Str)] string host);

    [DllImport(LibSslConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern long SSL_ctrl(SafeSslHandle ssl, int cmd, long larg, IntPtr parg);

    public static int SSL_set_tlsext_host_name(SafeSslHandle ssl, string name)
    {
        var namePtr = Marshal.StringToHGlobalAnsi(name);
        try
        {
            return (int) SSL_ctrl(ssl, SSL_CTRL_SET_TLSEXT_HOSTNAME, TLSEXT_NAMETYPE_host_name, namePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    /// <summary>
    /// 设置 SSL 模式标志位（对应 SSL_set_mode 宏）。
    /// </summary>
    public static long SSL_set_mode(SafeSslHandle ssl, uint mode)
    {
        return SSL_ctrl(ssl, SSL_CTRL_MODE, mode, IntPtr.Zero);
    }

    #endregion

    #region BIO

    [DllImport(LibCryptoConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern SafeBioHandle BIO_new_socket(IntPtr sock, int close_flag);

    [DllImport(LibCryptoConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void BIO_free_all(IntPtr bio);

    #endregion

    #region Error Handling

    [DllImport(LibCryptoConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ERR_get_error();

    [DllImport(LibCryptoConst, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ERR_error_string_n(ulong e, byte[] buf, int len);

    public static string GetErrorString(ulong error)
    {
        var buffer = new byte[256];
        ERR_error_string_n(error, buffer, buffer.Length);
        var nullIndex = Array.IndexOf(buffer, (byte) 0);
        return nullIndex >= 0 ? System.Text.Encoding.ASCII.GetString(buffer, 0, nullIndex) : System.Text.Encoding.ASCII.GetString(buffer);
    }

    #endregion
}

#region SafeHandle Wrappers

internal sealed class SafeSslContextHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeSslContextHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
        OpenSSLNative.SSL_CTX_free(handle);
        return true;
    }
}

internal sealed class SafeSslHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeSslHandle() : base(true) { }

    protected override bool ReleaseHandle()
    {
        OpenSSLNative.SSL_free(handle);
        return true;
    }
}

internal sealed class SafeBioHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeBioHandle() : base(true) { }

    /// <summary>
    /// 将句柄标记为无效，防止 ReleaseHandle 时重复释放。
    /// 用于 SSL_set_bio 已将 BIO 所有权转移给 SSL 对象的场景。
    /// </summary>
    public void MarkAsInvalid()
    {
        SetHandleAsInvalid();
    }

    protected override bool ReleaseHandle()
    {
        OpenSSLNative.BIO_free_all(handle);
        return true;
    }
}

#endregion
