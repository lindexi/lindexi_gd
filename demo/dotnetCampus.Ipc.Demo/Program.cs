using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //Console.WriteLine(string.Join(",", Encoding.UTF8.GetBytes("ACK").Select(temp => "0x" + temp.ToString("X2"))));
            //var byteList = BitConverter.GetBytes((ulong) 100);
            //Console.WriteLine(sizeof(ulong)); 

            //var peerRegisterProviderTests = new PeerRegisterProviderTests();
            //peerRegisterProviderTests.Run();

            //var ipcMessageConverterTest = new IpcMessageConverterTest();
            //ipcMessageConverterTest.Run();

            //var ackManagerTest = new AckManagerTest();
            //ackManagerTest.Run();

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
                Task.Run(BaiqealawbawKeqakeyawaw)
            };

            Task.WaitAll(jalejekemNereyararli.ToArray());
            Console.Read();
        }

        private static Task? BaiqealawbawKeqakeyawaw()
        {
            var ipcProvider = new IpcProvider();
            return ipcProvider.ConnectPeer(IpcContext.DefaultPipeName);
        }

        private static Task? WheehakawlucearHalwahewurlaiwhair()
        {
            var ipcProvider = new IpcProvider(IpcContext.DefaultPipeName);
            return ipcProvider.StartServer();
        }

        //private static async Task WhejeewukawBalbejelnewearfe()
        //{
        //    var ipcClientService = new IpcClientService();
        //    await ipcClientService.Start();

        //    while (true)
        //    {
        //        var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
        //        await ipcClientService.WriteMessageAsync(buffer, 0, buffer.Length);
        //        await Task.Delay(1000);
        //    }
        //}

        //private static void HalrowemfeareeHeabemnikeci()
        //{
        //    var ipcServerService = new IpcServerService();
        //    ipcServerService.Start();
        //}

        //private static async void LibearlafeCilinoballnelnall()
        //{
        //    try
        //    {
        //        int beebeniharHijocerene;

        //        lock (NamedPipeClientStreamList)
        //        {
        //            _loyawfanawKererocarwho++;
        //            beebeniharHijocerene = _loyawfanawKererocarwho;
        //        }

        //        var neachearjarcaiYahofairwufu = new NamedPipeClientStream(".", IpcContext.PipeName,
        //            PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
        //        //neachearjarcaiYahofairwufu=new NamedPipeClientStream(IpcContext.PipeName);
        //        NamedPipeClientStreamList.Add(neachearjarcaiYahofairwufu);
        //        neachearjarcaiYahofairwufu.Connect();

        //        //WebecucecefawJajaywurrere(new StreamReader(neachearjarcaiYahofairwufu), "Client" + beebeniharHijocerene);

        //        while (true)
        //        {
        //            var buffer = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
        //            await neachearjarcaiYahofairwufu.WriteAsync(buffer, 0, buffer.Length);
        //            await neachearjarcaiYahofairwufu.FlushAsync();
        //            await Task.Delay(1000);
        //        }


        //        //while (true)
        //        //{
        //        //    var buffer = new byte[64];
        //        //    var n = await neachearjarcaiYahofairwufu.ReadAsync(buffer, 0, 64);
        //        //    var text = Encoding.UTF8.GetString(buffer, 0, n);
        //        //    Console.WriteLine($"Client {beebeniharHijocerene} {text}");
        //        //}

        //        // 下面方法发送失败
        //        int n = 0;
        //        while (true)
        //        {
        //            var gachajurkakaiyiFewalkurbe = new StreamWriter(neachearjarcaiYahofairwufu);
        //            await gachajurkakaiyiFewalkurbe.WriteLineAsync($"Client {beebeniharHijocerene} {n}");
        //            await neachearjarcaiYahofairwufu.FlushAsync();
        //            n++;
        //            await Task.Delay(1000);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //    }
        //}

        //private static async void WebecucecefawJajaywurrere(StreamReader streamReader,
        //    string raywheawaljalciChewhaiballlo)
        //{
        //    while (true)
        //    {
        //        Console.WriteLine($"{raywheawaljalciChewhaiballlo} {await streamReader.ReadLineAsync()}");
        //    }
        //}

        //private static List<NamedPipeClientStream> NamedPipeClientStreamList { get; } =
        //    new List<NamedPipeClientStream>();

        //private static int _loyawfanawKererocarwho;

        //private static NamedPipeServerStream NamedPipeServerStream { set; get; }

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

    //class IpcClientStream:Message
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
}
