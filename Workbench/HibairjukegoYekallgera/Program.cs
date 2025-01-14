// See https://aka.ms/new-console-template for more information

var filePath = @"F:\temp\FelernihearBechanalwhi";

await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite,
    FileShare.Delete | FileShare.ReadWrite, bufferSize: 10, FileOptions.WriteThrough);

await using var readStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite, bufferSize: 10, FileOptions.WriteThrough);

for (var i = 0; i < int.MaxValue; i++)
{
    fileStream.Write([1, 2, 3]);
    await fileStream.FlushAsync();
    Console.ReadLine();

    if (i == 3)
    {
        File.Delete(filePath);
    }

    readStream.Position = 0;
    var buffer = new byte[1024];
    var readCount = readStream.Read(buffer, 0, buffer.Length);
    Console.WriteLine($"文件存在： {File.Exists(filePath)} 读取内容： {readCount}");
}

Console.WriteLine("Hello, World!");
