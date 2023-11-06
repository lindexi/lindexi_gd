// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Threading.Channels;

var channel = Channel.CreateUnbounded<Foo>(new UnboundedChannelOptions()
{
    SingleReader = true,
    SingleWriter = true,
});

Task.Run(async () =>
{
    while (true)
    {
        var result = await channel.Reader.WaitToReadAsync();
        if (result)
        {
            var foo = await channel.Reader.ReadAsync();
            Console.WriteLine($"读取到{foo.Count}");
        }
        else
        {
            Console.WriteLine($"返回 false 值");
            return;
        }
    }
});

Task.Run(async () =>
{
    for (int i = 0; i < 10; i++)
    {
        var result = await channel.Writer.WaitToWriteAsync();

        if (result is false)
        {
            Debugger.Break();
        }

        var foo = new Foo(i);
        await channel.Writer.WriteAsync(foo);
        await Task.Delay(100);
    }

    channel.Writer.Complete();
});

Console.Read();

record Foo(int Count);