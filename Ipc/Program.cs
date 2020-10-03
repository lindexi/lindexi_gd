using System;
using System.Collections;
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
                Task.Run(DiwerlowuKahecallweeler),
                Task.Run(LibearlafeCilinoballnelnall),
                Task.Run(LibearlafeCilinoballnelnall),
                Task.Run(LibearlafeCilinoballnelnall),
            };

            Task.WaitAll(jalejekemNereyararli.ToArray());
            Console.Read();
        }

        private static async void LibearlafeCilinoballnelnall()
        {
            int beebeniharHijocerene;

            lock (NamedPipeClientStreamList)
            {
                _loyawfanawKererocarwho++;
                beebeniharHijocerene = _loyawfanawKererocarwho;
            }

            var neachearjarcaiYahofairwufu = new NamedPipeClientStream("123");
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
            var namedPipeServerStream = new NamedPipeServerStream("123", PipeDirection.InOut, 10);
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

    public class IpcServerService
    {
        public void Start()
        {
            var namedPipeServerStream = new NamedPipeServerStream("123");
            NamedPipeServerStream = namedPipeServerStream;

            var streamMessageConverter = new StreamMessageConverter(namedPipeServerStream, IpcConfiguration.MessageHeader, IpcConfiguration.SharedArrayPool);
            StreamMessageConverter = streamMessageConverter;

            streamMessageConverter.MessageReceived += StreamMessageConverter_MessageReceived;
            streamMessageConverter.Start();
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