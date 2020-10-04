using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Ipc
{
    public class IpcServerService
    {
        public IpcServerService(string pipeName = IpcContext.PipeName)
        {
            PipeName = pipeName;
            var namedPipeServerStreamPool = new NamedPipeServerStreamPool(PipeName);
            NamedPipeServerStreamPool = namedPipeServerStreamPool;
        }

        public async Task Start()
        {
            await NamedPipeServerStreamPool.Start();
        }

        internal NamedPipeServerStreamPool NamedPipeServerStreamPool { set; get; } = null!;

        public string PipeName { get; }

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