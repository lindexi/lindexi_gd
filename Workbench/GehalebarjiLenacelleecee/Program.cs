// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Reflection.PortableExecutable;

var appFile = Path.Join(AppContext.BaseDirectory, "App.exe");

// 如果已经存在则直接读取 PE 头信息，获取 Overlays 信息
if (File.Exists(appFile))
{
    using var peReader = new PEReader(File.OpenRead(appFile));
    PEHeaders peHeaders = peReader.PEHeaders;

    return;
}

var file = @"C:\lindexi\Work\App.exe";
var fileStream = File.Open(file, FileMode.Open);

// 从模版应用拷贝内容
await using var appFileStream = File.Create(appFile);
await fileStream.CopyToAsync(appFileStream);

// 尝试添加 Overlays 内容
var buffer = ArrayPool<byte>.Shared.Rent(102400);

for (int i = 0; i < 10000; i++)
{
    Random.Shared.Shuffle(buffer);
    await appFileStream.WriteAsync(buffer.AsMemory());
}

ArrayPool<byte>.Shared.Return(buffer);

Console.WriteLine("Hello, World!");
Console.Read();