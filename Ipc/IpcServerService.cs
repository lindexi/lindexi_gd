using System.Threading.Tasks;

namespace Ipc
{
    public class IpcServerService
    {
        public IpcServerService(IpcContext ipcContext, string pipeName = IpcContext.DefaultPipeName)
        {
            PipeName = pipeName;
            IpcContext = ipcContext;
            var namedPipeServerStreamPool = new NamedPipeServerStreamPool(PipeName, IpcContext);
            NamedPipeServerStreamPool = namedPipeServerStreamPool;
        }

        internal NamedPipeServerStreamPool NamedPipeServerStreamPool { set; get; } = null!;

        public string PipeName { get; }
        public IpcContext IpcContext { get; }

        public async Task Start()
        {
            await NamedPipeServerStreamPool.Start();
        }

        //private void StreamMessageConverter_MessageReceived(object? sender, ByteListMessageStream e)
        //{
        //    Console.WriteLine(string.Join(",", e));
        //}

        //private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;
        //private StreamMessageConverter StreamMessageConverter { set; get; } = null!;

        //// 后续需要优化支持传入
        //private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }
}