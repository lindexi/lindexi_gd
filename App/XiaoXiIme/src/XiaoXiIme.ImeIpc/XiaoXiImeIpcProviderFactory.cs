using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;
using dotnetCampus.Ipc.Pipes;

namespace XiaoXiIme.ImeIpc;

public static class XiaoXiImeIpcProviderFactory
{
    public static JsonIpcDirectRoutedProvider CreateServerProvider(XiaoXiImeIpcOptions? options = null)
    {
        options ??= XiaoXiImeIpcOptions.Default;
        return new JsonIpcDirectRoutedProvider(options.ServerName, CreateConfiguration());
    }

    public static JsonIpcDirectRoutedProvider CreateClientProvider()
    {
        var pipeName = $"XiaoXiIme_IpcClient_{Environment.ProcessId}_{Guid.NewGuid():N}";
        return new JsonIpcDirectRoutedProvider(pipeName, CreateConfiguration());
    }

    public static IpcConfiguration CreateConfiguration()
    {
        return new IpcConfiguration
        {
            IpcObjectSerializer = new XiaoXiImeAotJsonIpcObjectSerializer(XiaoXiImeIpcJsonSerializerContext.Default),
        };
    }
}
