// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Pipes;

const string PipeName = "lindexi_doubi";

await StartServer();

var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    if (Directory.GetFiles(@"\\.\pipe\", PipeName).Length > 0)
    {
    }
}
stopwatch.Stop();
Console.WriteLine(stopwatch.ElapsedMilliseconds);


//File.Exists(@"\\.\pipe\" + PipeName);

await StartClient();

Console.Read();

async Task StartClient()
{
    var localServer = ".";
    var pipeDirection = PipeDirection.InOut;
    var client = new NamedPipeClientStream(localServer,
        PipeName, pipeDirection, PipeOptions.Asynchronous);

    var timeout = 1000 * 5;
    client.Connect(timeout);
}

async Task StartServer()
{
    var server = new NamedPipeServerStream(PipeName,
        PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024);

    server.BeginWaitForConnection(OnWaitForConnection, server);
}

void OnWaitForConnection(IAsyncResult ar)
{
    Console.WriteLine($"连接");
}