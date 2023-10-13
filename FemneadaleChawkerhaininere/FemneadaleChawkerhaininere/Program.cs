// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;

var bounded = Channel.CreateBounded<Foo>(new BoundedChannelOptions(5)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
    SingleReader = true
});

bool foo = true;

Task.Run(async () =>
{
    while (true)
    {
        while (foo)
        {
            await Task.Delay(100);
        }

        await bounded.Reader.ReadAsync();
    }
});

for (int i = 0; i < 10; i++)
{
    await bounded.Writer.WriteAsync(new Foo());
}

Console.WriteLine("Hello, World!");

record Foo();