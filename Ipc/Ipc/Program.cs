using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
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
                Task.Run(HalrowemfeareeHeabemnikeci),
                Task.Run(LibearlafeCilinoballnelnall),
                Task.Run(LibearlafeCilinoballnelnall),
                Task.Run(LibearlafeCilinoballnelnall),
            };

            Task.WaitAll(jalejekemNereyararli.ToArray());
            Console.Read();
        }

        private static void HalrowemfeareeHeabemnikeci()
        {
            var ipcServerService = new IpcServerService();
            ipcServerService.Start();

        }

        private static async void LibearlafeCilinoballnelnall()
        {
            int beebeniharHijocerene;

            lock (NamedPipeClientStreamList)
            {
                _loyawfanawKererocarwho++;
                beebeniharHijocerene = _loyawfanawKererocarwho;
            }

            var neachearjarcaiYahofairwufu = new NamedPipeClientStream(IpcContext.PipeName);
            NamedPipeClientStreamList.Add(neachearjarcaiYahofairwufu);
            neachearjarcaiYahofairwufu.Connect();

            while (true)
            {
                var buffer = new byte[64];
                var n = await neachearjarcaiYahofairwufu.ReadAsync(buffer, 0, 64);
                var text = Encoding.UTF8.GetString(buffer, 0, n);
                Console.WriteLine($"Client {beebeniharHijocerene} {text}");
            }

            //WebecucecefawJajaywurrere(new StreamReader(neachearjarcaiYahofairwufu), "Client" + beebeniharHijocerene);

            //int n = 0;
            //while (true)
            //{
            //    var gachajurkakaiyiFewalkurbe = new StreamWriter(neachearjarcaiYahofairwufu);
            //    await gachajurkakaiyiFewalkurbe.WriteLineAsync($"Client {beebeniharHijocerene} {n}");
            //    await neachearjarcaiYahofairwufu.FlushAsync();
            //    n++;
            //    await Task.Delay(1000);
            //}
        }

        private static List<NamedPipeClientStream> NamedPipeClientStreamList { get; } = new List<NamedPipeClientStream>();

        private static int _loyawfanawKererocarwho;

        private static NamedPipeServerStream NamedPipeServerStream { set; get; }

        private static async Task DiwerlowuKahecallweeler()
        {
            var namedPipeServerStream = new NamedPipeServerStream(IpcContext.PipeName, PipeDirection.InOut, 10);
            NamedPipeServerStream = namedPipeServerStream;

            var streamWriter = new StreamWriter(namedPipeServerStream);
            var streamReader = new StreamReader(namedPipeServerStream);
            Console.WriteLine("WaitForConnectionAsync");
            await namedPipeServerStream.WaitForConnectionAsync();
            //WebecucecefawJajaywurrere(streamReader, "Server");

            while (true)
            {
                //await streamWriter.WriteLineAsync(DateTime.Now.ToString());
                var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
                await namedPipeServerStream.WriteAsync(buffer, 0, buffer.Length);
                await namedPipeServerStream.FlushAsync();
                await Task.Delay(1000);
            }

            //var ipcServerService = new IpcServerService();
            //ipcServerService.Start();
        }

        private static async void WebecucecefawJajaywurrere(StreamReader streamReader, string raywheawaljalciChewhaiballlo)
        {
            while (true)
            {
                Console.WriteLine($"{raywheawaljalciChewhaiballlo} {await streamReader.ReadLineAsync()}");
            }
        }
    }

    class PipeServerMessage
    {
        private NamedPipeServerStream NamedPipeServerStream { set; get; } = null!;

        public async Task Start()
        {
            var namedPipeServerStream = new NamedPipeServerStream(PipeName);
            NamedPipeServerStream = namedPipeServerStream;

            var streamMessageConverter = new StreamMessageConverter(namedPipeServerStream, IpcConfiguration.MessageHeader, IpcConfiguration.SharedArrayPool);
            streamMessageConverter.MessageReceived += OnClientConnectReceived;
            StreamMessageConverter = streamMessageConverter;
            await namedPipeServerStream.WaitForConnectionAsync();
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

        public string PipeName { get; } = IpcContext.PipeName;

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

        public async void Start()
        {
            while (true)
            {
                var pipeServerMessage = new PipeServerMessage();

                pipeServerMessage.ClientConnected += PipeServerMessage_ClientConnected;
                pipeServerMessage.MessageReceived += PipeServerMessage_MessageReceived;

                await pipeServerMessage.Start();
            }
        }

        private void PipeServerMessage_MessageReceived(object? sender, ClientMessageArgs e)
        {
        }

        private void PipeServerMessage_ClientConnected(object? sender, ClientConnectedArgs e)
        {
            if (NamedPipeServerStreamList.TryAdd(e.ClientName,e.NamedPipeServerStream))
            {
                
            }
            else
            {
                // 有客户端重复连接
            }
        }

        private ConcurrentDictionary<string, NamedPipeServerStream> NamedPipeServerStreamList { get; } = new ConcurrentDictionary<string, NamedPipeServerStream>();

        private IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }

    public static class IpcContext
    {
        public const string PipeName = "1231";
    }

    public class IpcServerService
    {
        public void Start()
        {
            //var namedPipeServerStreamPool = new NamedPipeServerStreamPool();
            //namedPipeServerStreamPool.Start();

        }

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
        public byte[] MessageHeader { set; get; } = { 0x64, 0x6F, 0x74, 0x6E, 0x65, 0x74, 0x20, 0x63, 0x61, 0x6D, 0x70, 0x75, 0x73 };
        /*
         * Message:
         * Header
         * Length
         * Content
         *
         */
    }
}