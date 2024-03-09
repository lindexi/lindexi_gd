using dotnetCampus.Ipc.Pipes.PipeConnectors;

namespace UnoSpySnoopDebugger.Communications;

class UnoSpySnoopDebuggerIpcClientPipeConnector : IIpcClientPipeConnector
{
    public async Task<IpcClientNamedPipeConnectResult> ConnectNamedPipeAsync(IpcClientPipeConnectionContext ipcClientPipeConnectionContext)
    {
        try
        {
            // With special timeout
            await ipcClientPipeConnectionContext.NamedPipeClientStream.ConnectAsync(TimeSpan.FromSeconds(1),
                ipcClientPipeConnectionContext.CancellationToken);
        }
        catch (TimeoutException e)
        {
            return new IpcClientNamedPipeConnectResult(false, e.Message);
        }

        return new IpcClientNamedPipeConnectResult(true);
    }
}
