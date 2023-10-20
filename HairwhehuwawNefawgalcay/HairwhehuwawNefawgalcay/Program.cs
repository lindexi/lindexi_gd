// See https://aka.ms/new-console-template for more information

using System.IO.Pipes;

var pipeName = Guid.NewGuid().ToString();

Task.Run(async () =>
{
    var namedPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);

    await namedPipeServerStream.WaitForConnectionAsync();

    var stream = namedPipeServerStream;
    Task.Run(async () =>
    {
        while (true)
        {
            var foo = new byte[1024];
            await stream.ReadAsync(foo.AsMemory());
        }
    });

    Task.Run(async () =>
    {
        for (int i = 0; i < int.MaxValue; i++)
        {
            stream.WriteByte((byte) i);
            await stream.FlushAsync();
        }
    });
});

Task.Run(async () =>
{
    var namedPipeClientStream = new NamedPipeClientStream(".",pipeName,PipeDirection.InOut);
    await namedPipeClientStream.ConnectAsync();

    var stream = namedPipeClientStream;

    Task.Run(async () =>
    {
        while (true)
        {
            //var readByte = stream.ReadByte();
            var foo =new byte[1024];
            await stream.ReadAsync(foo.AsMemory());
        }
    });

    //Task.Run(async () =>
    //{
    //    for (int i = 0; i < int.MaxValue; i++)
    //    {
    //        stream.WriteByte((byte) i);
    //    }
    //});
});

Console.WriteLine("Hello, World!");
Console.Read();

