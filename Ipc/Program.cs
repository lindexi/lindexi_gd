using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipc
{
    class Program
    {
        static void Main(string[] args)
        {
            var jalejekemNereyararli = new List<Task>
            {
                //Task.Run(DiwerlowuKahecallweeler),
                //Task.Run(DiwerlowuKahecallweeler),
                //Task.Run(HalrowemfeareeHeabemnikeci),
                //Task.Run(LibearlafeCilinoballnelnall),
                //Task.Run(LibearlafeCilinoballnelnall),
                //Task.Run(LibearlafeCilinoballnelnall),
                //Task.Run(WhejeewukawBalbejelnewearfe),
                Task.Run(WheehakawlucearHalwahewurlaiwhair),
                Task.Run(BaiqealawbawKeqakeyawaw),
            };

            Task.WaitAll(jalejekemNereyararli.ToArray());
            Console.Read();
        }

        private static Task? BaiqealawbawKeqakeyawaw()
        {
            var ipcProvider = new IpcProvider();
            return ipcProvider.ConnectServer(IpcContext.PipeName);
        }

        private static Task? WheehakawlucearHalwahewurlaiwhair()
        {
            var ipcProvider = new IpcProvider(IpcContext.PipeName);
            return ipcProvider.StartServer();
        }

        private static async Task WhejeewukawBalbejelnewearfe()
        {
            var ipcClientService = new IpcClientService();
            await ipcClientService.Start();

            while (true)
            {
                var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
                await ipcClientService.WriteMessageAsync(buffer, 0, buffer.Length);
                await Task.Delay(1000);
            }
        }

        private static void HalrowemfeareeHeabemnikeci()
        {
            var ipcServerService = new IpcServerService();
            ipcServerService.Start();
        }

        private static async void LibearlafeCilinoballnelnall()
        {
            try
            {
                int beebeniharHijocerene;

                lock (NamedPipeClientStreamList)
                {
                    _loyawfanawKererocarwho++;
                    beebeniharHijocerene = _loyawfanawKererocarwho;
                }

                var neachearjarcaiYahofairwufu = new NamedPipeClientStream(".", IpcContext.PipeName,
                    PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
                //neachearjarcaiYahofairwufu=new NamedPipeClientStream(IpcContext.PipeName);
                NamedPipeClientStreamList.Add(neachearjarcaiYahofairwufu);
                neachearjarcaiYahofairwufu.Connect();

                //WebecucecefawJajaywurrere(new StreamReader(neachearjarcaiYahofairwufu), "Client" + beebeniharHijocerene);

                while (true)
                {
                    var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
                    await neachearjarcaiYahofairwufu.WriteAsync(buffer, 0, buffer.Length);
                    await neachearjarcaiYahofairwufu.FlushAsync();
                    await Task.Delay(1000);
                }


                //while (true)
                //{
                //    var buffer = new byte[64];
                //    var n = await neachearjarcaiYahofairwufu.ReadAsync(buffer, 0, 64);
                //    var text = Encoding.UTF8.GetString(buffer, 0, n);
                //    Console.WriteLine($"Client {beebeniharHijocerene} {text}");
                //}

                // 下面方法发送失败
                int n = 0;
                while (true)
                {
                    var gachajurkakaiyiFewalkurbe = new StreamWriter(neachearjarcaiYahofairwufu);
                    await gachajurkakaiyiFewalkurbe.WriteLineAsync($"Client {beebeniharHijocerene} {n}");
                    await neachearjarcaiYahofairwufu.FlushAsync();
                    n++;
                    await Task.Delay(1000);
                }
            }
            catch (Exception e)
            {
            }
        }

        private static async void WebecucecefawJajaywurrere(StreamReader streamReader,
            string raywheawaljalciChewhaiballlo)
        {
            while (true)
            {
                Console.WriteLine($"{raywheawaljalciChewhaiballlo} {await streamReader.ReadLineAsync()}");
            }
        }

        private static List<NamedPipeClientStream> NamedPipeClientStreamList { get; } =
            new List<NamedPipeClientStream>();

        private static int _loyawfanawKererocarwho;

        private static NamedPipeServerStream NamedPipeServerStream { set; get; }

        //private static async Task DiwerlowuKahecallweeler()
        //{
        //    var namedPipeServerStream = new NamedPipeServerStream(IpcContext.PipeName, PipeDirection.InOut, 10);
        //    NamedPipeServerStream = namedPipeServerStream;

        //    var streamWriter = new StreamWriter(namedPipeServerStream);
        //    var streamReader = new StreamReader(namedPipeServerStream);
        //    Console.WriteLine("WaitForConnectionAsync");
        //    await namedPipeServerStream.WaitForConnectionAsync();
        //    //WebecucecefawJajaywurrere(streamReader, "Server");

        //    namedPipeServerStream.ReadByte();

        //    while (true)
        //    {
        //        //await streamWriter.WriteLineAsync(DateTime.Now.ToString());
        //        var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
        //        await namedPipeServerStream.WriteAsync(buffer, 0, buffer.Length);
        //        await namedPipeServerStream.FlushAsync();
        //        await Task.Delay(1000);
        //    }

        //    //var ipcServerService = new IpcServerService();
        //    //ipcServerService.Start();
        //}
    }

    class PipeServerMessage
    {
        public PipeServerMessage(string pipeName)
        {
            PipeName = pipeName;
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        public async Task Start()
        {
            var namedPipeServerStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 250);
            NamedPipeServerStream = namedPipeServerStream;

            await namedPipeServerStream.WaitForConnectionAsync();

            var streamMessageConverter = new StreamMessageConverter(namedPipeServerStream,
                IpcConfiguration.MessageHeader, IpcConfiguration.SharedArrayPool);
            streamMessageConverter.MessageReceived += OnClientConnectReceived;
            StreamMessageConverter = streamMessageConverter;
            streamMessageConverter.Start();
        }

        private StreamMessageConverter StreamMessageConverter { set; get; } = null!;

        private void OnClientConnectReceived(object? sender, ByteListMessageStream stream)
        {
            var streamReader = new StreamReader(stream);
            var clientName = streamReader.ReadToEnd();
            ClientName = clientName;

            OnClientConnected(new ClientConnectedArgs(clientName, NamedPipeServerStream));

            StreamMessageConverter.MessageReceived -= OnClientConnectReceived;
            StreamMessageConverter.MessageReceived += StreamMessageConverter_MessageReceived;
        }

        private void StreamMessageConverter_MessageReceived(object? sender, ByteListMessageStream e)
        {
            OnMessageReceived(new ClientMessageArgs(ClientName, e));
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        private string ClientName { set; get; } = null!;

        public string PipeName { get; }

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        protected virtual void OnClientConnected(ClientConnectedArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnMessageReceived(ClientMessageArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }

    public class ClientConnectedArgs : EventArgs
    {
        public ClientConnectedArgs(string clientName, NamedPipeServerStream namedPipeServerStream)
        {
            ClientName = clientName;
            NamedPipeServerStream = namedPipeServerStream;
        }

        public string ClientName { get; }

        public NamedPipeServerStream NamedPipeServerStream { get; }
    }

    public class ClientMessageArgs : EventArgs
    {
        public ClientMessageArgs(string clientName, Stream stream)
        {
            Stream = stream;
            ClientName = clientName;
        }

        public Stream Stream { get; }

        public string ClientName { get; }
    }

    class NamedPipeServerStreamPool
    {
        public async Task Start()
        {
            while (true)
            {
                var pipeServerMessage = new PipeServerMessage(PipeName);

                pipeServerMessage.ClientConnected += PipeServerMessage_ClientConnected;
                pipeServerMessage.MessageReceived += PipeServerMessage_MessageReceived;

                await pipeServerMessage.Start();
            }
        }

        public NamedPipeServerStreamPool(string pipeName)
        {
            PipeName = pipeName;
        }

        public string PipeName { get; }

        private void PipeServerMessage_MessageReceived(object? sender, ClientMessageArgs e)
        {
            MessageReceived?.Invoke(sender, e);
        }

        private void PipeServerMessage_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            if (NamedPipeServerStreamList.TryAdd(e.ClientName, e.NamedPipeServerStream))
            {

            }
            else
            {
                // 有客户端重复连接，或这是服务器端连接
            }

            ClientConnected?.Invoke(sender, e);
        }

        public event EventHandler<ClientMessageArgs>? MessageReceived;

        public event EventHandler<ClientConnectedArgs>? ClientConnected;

        private ConcurrentDictionary<string, NamedPipeServerStream> NamedPipeServerStreamList { get; } = new ConcurrentDictionary<string, NamedPipeServerStream>();

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }

    public static class IpcContext
    {
        public const string PipeName = "1231";
    }

    //class IpcClientStream:Stream
    //{
    //    public override void Flush()
    //    {

    //    }

    //    public override Task FlushAsync(CancellationToken cancellationToken)
    //    {
    //        return base.FlushAsync(cancellationToken);
    //    }

    //    public override void Write(ReadOnlySpan<byte> buffer)
    //    {
    //        base.Write(buffer);
    //    }

    //    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    //    {
    //        return base.WriteAsync(buffer, offset, count, cancellationToken);
    //    }

    //    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    //    {
    //        return base.WriteAsync(buffer, cancellationToken);
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void SetLength(long value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public override bool CanRead { get; }
    //    public override bool CanSeek { get; }
    //    public override bool CanWrite { get; }
    //    public override long Length { get; }
    //    public override long Position { get; set; }
    //}

    /// <summary>
    /// 对等通讯，每个都是服务器端，每个都是客户端
    /// </summary>
    public class IpcProvider
    {
        public IpcProvider() : this(Guid.NewGuid().ToString("N"))
        {

        }

        public IpcProvider(string clientName)
        {
            ClientName = clientName;
        }

        public async Task StartServer()
        {
            if (IpcServerService != null)
            {
                return;
            }

            var ipcServerService = new IpcServerService(ClientName);
            IpcServerService = ipcServerService;

            ipcServerService.NamedPipeServerStreamPool.ClientConnected += NamedPipeServerStreamPool_ClientConnected;

            await ipcServerService.Start();
        }

        private async void NamedPipeServerStreamPool_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            // 也许是服务器连接
            if (ConnectedServerManagerList.Any(temp => temp.ServerName == e.ClientName))
            {

            }
            else
            {
                // 其他客户端连接
                await ConnectServer(e.ClientName);
            }
        }

        public IpcServerService IpcServerService { set; get; } = null!;

        public async Task ConnectServer(string serverName)
        {
            var task = StartServer();

            var connectedServerManager = new ConnectedServerManager(serverName, ClientName);

            ConnectedServerManagerList.Add(connectedServerManager);

            await connectedServerManager.ConnectServer();

            await task;
        }

        public string ClientName { get; }

        public ConcurrentBag<ConnectedServerManager> ConnectedServerManagerList { get; } = new ConcurrentBag<ConnectedServerManager>();
    }

    public class ConnectedServerManager
    {
        public ConnectedServerManager(string serverName, string clientName)
        {
            ServerName = serverName;
            ClientName = clientName;
        }

        public async Task ConnectServer()
        {
            var ipcClientService = new IpcClientService(ServerName);
            IpcClientService = ipcClientService;
            await ipcClientService.Start();
            await ipcClientService.WriteStringAsync(ClientName);
        }

        public string ServerName { get; }
        public string ClientName { get; }

        public IpcClientService IpcClientService { get; set; } = null!;

    }

    public class IpcClientService
    {
        public IpcClientService(string serverName = IpcContext.PipeName)
        {
            ServerName = serverName;
        }

        public async Task Start()
        {
            var namedPipeClientStream = new NamedPipeClientStream(".", ServerName, PipeDirection.InOut,
                PipeOptions.None, TokenImpersonationLevel.Impersonation);
            await namedPipeClientStream.ConnectAsync();

            NamedPipeClientStream = namedPipeClientStream;
        }

        public void Stop()
        {
            // 告诉服务器端不连接
        }

        private NamedPipeClientStream NamedPipeClientStream { set; get; } = null!;

        public Task WriteStringAsync(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            return WriteMessageAsync(buffer, 0, buffer.Length);
        }

        public async Task WriteMessageAsync(byte[] buffer, int offset, int count)
        {
            await NamedPipeClientStream.WriteAsync(IpcConfiguration.MessageHeader);
            var byteList = BitConverter.GetBytes(count);
            await NamedPipeClientStream.WriteAsync(byteList, 0, byteList.Length);
            await NamedPipeClientStream.WriteAsync(buffer, offset, count);
            await NamedPipeClientStream.FlushAsync();
        }

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        public string ServerName { get; }
    }

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

        private void StreamMessageConverter_MessageReceived(object? sender, ByteListMessageStream e)
        {
            Console.WriteLine(string.Join(",", e));
        }

        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;
        private StreamMessageConverter StreamMessageConverter { set; get; } = null!;

        // 后续需要优化支持传入
        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }

    public class IpcConfiguration
    {
        public ISharedArrayPool SharedArrayPool { get; set; } = new SharedArrayPool();

        // dotnet campus 0x64, 0x6F, 0x74, 0x6E, 0x65, 0x74, 0x20, 0x63, 0x61, 0x6D, 0x70, 0x75, 0x73 
        public byte[] MessageHeader { set; get; } =
            {0x64, 0x6F, 0x74, 0x6E, 0x65, 0x74, 0x20, 0x63, 0x61, 0x6D, 0x70, 0x75, 0x73};

        /*
         * Message:
         * Header
         * Length
         * Content
         *
         */
    }
}