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

        // PInvoke declaration for OpenSSL functions
        [DllImport("libcrypto-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void ERR_load_CRYPTO_strings();

        [DllImport("libssl-3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void EVP_cleanup();

        [DllImport("libssl-3.dll", EntryPoint = "OPENSSL_init_ssl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int OPENSSL_init_ssl(ulong opts, IntPtr settings); // 对应 SSL_library_init

        [DllImport("libssl-3.dll",EntryPoint = "TLS_client_method", CallingConvention = CallingConvention.Cdecl)]
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
                ERR_load_CRYPTO_strings();
                OPENSSL_init_ssl(0, IntPtr.Zero);

                // Create SSL context
                var ssLv23ClientMethod = SSLv23_client_method();
                IntPtr ctx = SSL_CTX_new(ssLv23ClientMethod);
                if (ctx == IntPtr.Zero)
                {
                    Console.WriteLine("创建 SSL 上下文失败");
                    return;
                }

                // Create SSL connection
                IntPtr bio = BIO_new_ssl_connect(ctx);
                if (bio == IntPtr.Zero)
                {
                    Console.WriteLine("创建 SSL 连接失败");
                    return;
                }

                // Set hostname and port
                BIO_puts(bio, "www.baidu.com:443");

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

                // Send HTTP GET request
                string request = "GET / HTTP/1.1\r\nHost: www.baidu.com\r\n\r\n";
                BIO_puts(bio, request);

                // Read response
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = BIO_read(bio, buffer, buffer.Length)) > 0)
                {
                    Console.Write(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }

                // Clean up resources
                BIO_free_all(bio);
                SSL_CTX_free(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误：{ex.Message}");
            }
        }
    }
}
