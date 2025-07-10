// See https://aka.ms/new-console-template for more information

using System.Buffers;

var buffer = new byte[1024];
ArrayPool<byte>.Shared.Return(buffer);

for (int i = 0; i < 100; i++)
{
    var t = ArrayPool<byte>.Shared.Rent(1024);
    if (ReferenceEquals(t, buffer))
    {
        Console.WriteLine($"归还的自己申请的非租用的 Buffer 数组，可以重新被借用出来");
    }
}
Console.WriteLine("Hello, World!");