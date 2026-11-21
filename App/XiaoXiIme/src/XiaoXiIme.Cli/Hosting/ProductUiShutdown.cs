using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli;

internal static class ProductUiShutdown
{
    internal static bool Stop(TextWriter error)
    {
        var serverName = XiaoXiImeIpcOptions.DefaultServerName;
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.UserName + "\0" + serverName)));
        var pipeName = "XiaoXiIme_ProductUi_" + identity;
        try
        {
            using var mutex = new Mutex(false, "Local\\" + pipeName);
            if (WaitForExit(mutex, 0))
            {
                return true;
            }

            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly))
            {
                client.Connect(3000);
                client.WriteByte(2);
            }

            if (WaitForExit(mutex, 5000))
            {
                return true;
            }

            error.WriteLine($"[ProductUi][Stop] ServerName={serverName}; Error=The input service did not exit.");
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
        {
            error.WriteLine($"[ProductUi][Stop] ServerName={serverName}; Error={exception}");
            return false;
        }
    }

    private static bool WaitForExit(Mutex mutex, int timeout)
    {
        try
        {
            if (!mutex.WaitOne(timeout))
            {
                return false;
            }
        }
        catch (AbandonedMutexException)
        {
        }

        mutex.ReleaseMutex();
        return true;
    }
}
