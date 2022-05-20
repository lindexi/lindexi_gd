// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Pipes;

const string PipeName = "lindexi";

StartServer();

File.Exists(@"\\.\pipe\" + PipeName);

StartClient();

Console.Read();

void StartClient()
{
    var localServer = ".";
    var pipeDirection = PipeDirection.InOut;
    var client = new NamedPipeClientStream(localServer,
        PipeName, pipeDirection, PipeOptions.Asynchronous);

    var timeout = 1000 * 5;
    client.Connect(timeout);
}

void StartServer()
{
    var server = new NamedPipeServerStream(PipeName,
        PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 1024, 1024);

    server.BeginWaitForConnection(OnWaitForConnection, server);
}

void OnWaitForConnection(IAsyncResult ar)
{
    Console.WriteLine($"Connect");
}