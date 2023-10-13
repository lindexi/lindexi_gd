// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;

var task1Channel = Channel.CreateBounded<Foo>(new BoundedChannelOptions(5)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
    SingleReader = true
});

var task2Channel = Channel.CreateBounded<Foo>(new BoundedChannelOptions(3)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
    SingleReader = true
});

bool foo = true;

// 让第一个创建的速度最快
Task.Run(async () =>
{
    while (true)
    {
        await task1Channel.Writer.WriteAsync(new Foo());
    }
});

// 让第二个等待空闲再写入
Task.Run(async () =>
{
    while (true)
    {
        var waitToWrite = await task2Channel.Writer.WaitToWriteAsync();
        if (!waitToWrite)
        {
            return;
        }

        var task = await task1Channel.Reader.ReadAsync();
        // 模拟执行任务
        await Task.Delay(100);
        await task2Channel.Writer.WriteAsync(task);
    }
});

Task.Run(async () =>
{
    while (true)
    {
        while (foo)
        {
            await Task.Delay(100);
        }

        await task2Channel.Reader.ReadAsync();
    }
});

Console.WriteLine("Hello, World!");
Console.Read();

class Foo
{
    public Foo()
    {
        N = _count++;
    }

    public int N { get; }

    private static int _count;
}