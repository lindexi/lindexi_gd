using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace BingAccess
{
    class Program
    {
        // 下载 https://kb.firedaemon.com/support/solutions/articles/4000121705

        // #   define OpenSSL_add_all_algorithms() OPENSSL_add_all_algorithms_conf()
        //#  define OPENSSL_add_all_algorithms_conf() \
        // OPENSSL_init_crypto(OPENSSL_INIT_ADD_ALL_CIPHERS \
        //                     | OPENSSL_INIT_ADD_ALL_DIGESTS \
        //                     | OPENSSL_INIT_LOAD_CONFIG, NULL)
        // int OPENSSL_init_crypto(uint64_t opts, const OPENSSL_INIT_SETTINGS *settings);
        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OPENSSL_init_crypto(long opts, IntPtr settings);

        // # define OPENSSL_INIT_LOAD_CONFIG            0x00000040L
        // # define OPENSSL_INIT_ADD_ALL_DIGESTS        0x00000008L
        // # define OPENSSL_INIT_ADD_ALL_CIPHERS        0x00000004L

        public static void OpenSSL_add_all_algorithms()
        {
            OPENSSL_add_all_algorithms_conf();
        }

        public static void OPENSSL_add_all_algorithms_conf()
        {
            OPENSSL_init_crypto(0x00000040L | 0x00000008L | 0x00000004L, IntPtr.Zero);
        }


        //#  define BIO_set_conn_hostname(b,name) BIO_ctrl(b,BIO_C_SET_CONNECT,0, \
        //                                         (char *)(name))
        //# define BIO_C_SET_CONNECT                       100
        public static void BIO_set_conn_hostname(IntPtr bio, string name)
        {
            const int BIO_C_SET_CONNECT = 100;
            BIO_ctrl(bio, BIO_C_SET_CONNECT, 0, name);
        }

        // PInvoke declaration for OpenSSL functions
        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ERR_load_CRYPTO_strings();

        [DllImport("libssl-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void EVP_cleanup();

        [DllImport("libssl-3.dll", EntryPoint = "OPENSSL_init_ssl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int OPENSSL_init_ssl(ulong opts, IntPtr settings); // 对应 SSL_library_init

        [DllImport("libssl-3.dll", EntryPoint = "TLS_client_method", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SSLv23_client_method();

        [DllImport("libssl-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SSL_CTX_new(IntPtr method);

        [DllImport("libssl-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SSL_CTX_free(IntPtr ctx);

        [DllImport("libssl-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr BIO_new_ssl_connect(IntPtr ctx);

        //[DllImport("libssl-3.dll", EntryPoint = "BIO_do_handshake", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int BIO_do_connect(IntPtr bio);
        // long BIO_ctrl(BIO *bp, int cmd, long larg, void *parg);
        // # define BIO_do_handshake(b)     BIO_ctrl(b,BIO_C_DO_STATE_MACHINE,0,NULL)
        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BIO_ctrl(IntPtr bio, int cmd, long larg, IntPtr parg);

        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BIO_ctrl(IntPtr bio, int cmd, long larg, string parg);

        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern nint BIO_new_connect(string host_port);

        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BIO_puts(IntPtr bio, string data);

        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BIO_read(IntPtr bio, byte[] buffer, int len);

        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BIO_free_all(IntPtr bio);

        static void Main(string[] args)
        {
            try
            {
                // Initialize OpenSSL
                //#  define SSL_load_error_strings() \
                //  OPENSSL_init_ssl(OPENSSL_INIT_LOAD_SSL_STRINGS \
                //                 | OPENSSL_INIT_LOAD_CRYPTO_STRINGS, NULL)
                // # define OPENSSL_INIT_LOAD_SSL_STRINGS       0x00200000L
                // # define OPENSSL_INIT_LOAD_CRYPTO_STRINGS    0x00000002L
                const ulong OPENSSL_INIT_LOAD_SSL_STRINGS = 0x00200000L;
                const ulong OPENSSL_INIT_LOAD_CRYPTO_STRINGS = 0x00000002L;
                OPENSSL_init_ssl(OPENSSL_INIT_LOAD_SSL_STRINGS | OPENSSL_INIT_LOAD_CRYPTO_STRINGS, IntPtr.Zero);
                //ERR_load_BIO_strings();
                ERR_load_CRYPTO_strings();
                OpenSSL_add_all_algorithms();

                var bio = BIO_new_connect("www.baidu.com:443");
                // 找不到 BIO_do_connect 方法
                // Perform SSL handshake
                //var bioDoConnect = BIO_do_connect(bio);
                // # define BIO_do_handshake(b)     BIO_ctrl(b,BIO_C_DO_STATE_MACHINE,0,NULL)
                //  # define BIO_C_DO_STATE_MACHINE                  101
                var bioDoConnect = BIO_ctrl(bio, 101, 0, IntPtr.Zero);
                if (bioDoConnect <= 0)
                {
                    Console.WriteLine("建立 SSL 连接失败");
                    return;
                }
                byte[] buffer = new byte[1024];
                int x = BIO_read(bio, buffer, buffer.Length);

                //// Create SSL context
                //var ssLv23ClientMethod = SSLv23_client_method();
                //IntPtr ctx = SSL_CTX_new(ssLv23ClientMethod);
                //if (ctx == IntPtr.Zero)
                //{
                //    Console.WriteLine("创建 SSL 上下文失败");
                //    return;
                //}

                //// Create SSL connection
                //IntPtr bio = BIO_new_ssl_connect(ctx);
                //if (bio == IntPtr.Zero)
                //{
                //    Console.WriteLine("创建 SSL 连接失败");
                //    SSL_CTX_free(ctx);
                //    return;
                //}

                //// Set the target host and port
                //BIO_set_conn_hostname(bio, "www.bing.com:443");

                //// 找不到 BIO_do_connect 方法
                //// Perform SSL handshake
                ////var bioDoConnect = BIO_do_connect(bio);
                //// # define BIO_do_handshake(b)     BIO_ctrl(b,BIO_C_DO_STATE_MACHINE,0,NULL)
                ////  # define BIO_C_DO_STATE_MACHINE                  101
                //var bioDoConnect = BIO_ctrl(bio, 101, 0, IntPtr.Zero);
                //if (bioDoConnect <= 0)
                //{
                //    Console.WriteLine("建立 SSL 连接失败");
                //    return;
                //}

                //// Send HTTP GET request
                //string request = "GET / HTTP/1.1\r\nHost: www.baidu.com\r\n\r\n";
                //BIO_puts(bio, request);

                //// Read response
                //byte[] buffer = new byte[1024];
                //int bytesRead;
                //while ((bytesRead = BIO_read(bio, buffer, buffer.Length)) > 0)
                //{
                //    Console.Write(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                //}

                //// Clean up resources
                //BIO_free_all(bio);
                ////SSL_CTX_free(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误：{ex.Message}");
            }
        }
    }
}
