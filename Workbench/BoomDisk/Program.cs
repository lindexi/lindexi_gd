// See https://aka.ms/new-console-template for more information

using System.Buffers;

var folder = @"D:\lindexi\laji";
var currentFolder = folder;

for (int i = 0; i < int.MaxValue / 2; i++)
{
    try
    {
        if (Random.Shared.Next(10) > 7)
        {
            var next = Random.Shared.Next(3);
            var folderName = GetRandomName();

            if (next == 0)
            {
                currentFolder = Path.Join(currentFolder, folderName);
                Directory.CreateDirectory(currentFolder);
            }
            else if (next == 1)
            {
                if (currentFolder.Length <= folder.Length + 1)
                {
                    currentFolder = folder;
                    continue;
                }

                currentFolder = Path.Join(Path.GetDirectoryName(currentFolder), folderName);
            }
            else
            {
                currentFolder = Path.GetDirectoryName(currentFolder) ?? folder;
                if (currentFolder.Length <= folder.Length + 1)
                {
                    currentFolder = folder;
                }
            }
        }
        else
        {
            var fileName = GetRandomName();
            Directory.CreateDirectory(currentFolder);
            var file = Path.Join(currentFolder, fileName);
            WriteRandomFile(file);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

string GetRandomName() => Path.GetRandomFileName();

void WriteRandomFile(string file)
{
    var length = Random.Shared.Next(1024 * 1024 * 10);
    var buffer = ArrayPool<byte>.Shared.Rent(length);
    Span<byte> span = buffer.AsSpan(0, length);
    try
    {
        Random.Shared.NextBytes(span);
        using var fileStream = File.OpenWrite(file);
        fileStream.Write(span);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

Console.WriteLine("Hello, World!");
