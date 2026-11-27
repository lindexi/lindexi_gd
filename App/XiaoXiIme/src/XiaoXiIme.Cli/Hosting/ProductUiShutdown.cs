using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.Cli;

internal static class ProductUiShutdown
{
    internal static bool Stop(TextWriter error, Action<string>? log = null)
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

            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                client.Connect(3000);
                client.WriteByte(2);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
            {
                if (WaitForExit(mutex, 0))
                {
                    return true;
                }

                throw;
            }

            if (WaitForExit(mutex, 5000))
            {
                return true;
            }

            ReportError($"[ProductUi][Stop] ServerName={serverName}; Error=The input service did not exit.");
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException)
        {
            ReportError($"[ProductUi][Stop] ServerName={serverName}; Error={exception}");
            return false;
        }

        void ReportError(string message)
        {
            log?.Invoke(message);
            error.WriteLine(message);
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
