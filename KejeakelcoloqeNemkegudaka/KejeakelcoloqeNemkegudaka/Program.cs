// See https://aka.ms/new-console-template for more information

using System.IO.Pipes;
using System.Security.Principal;

for (int i = 0; i < 1000; i++)
{
    var namedPipeClientStream = new NamedPipeClientStream(".", "NotExists_" + i, PipeDirection.Out,
        PipeOptions.None, TokenImpersonationLevel.Impersonation);
    //Task.Factory.StartNew(namedPipeClientStream.Connect, TaskCreationOptions.LongRunning);
   _ = namedPipeClientStream.ConnectAsync();
}

Console.Read();
