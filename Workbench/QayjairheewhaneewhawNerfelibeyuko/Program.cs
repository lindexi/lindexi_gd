// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

HashAlgorithm hashAlgorithm = MD5.Create();

var text = "Hello, World!";

var list = new List<string>();
var count = 1024 * 10240;

for (int i = 0; i < count; i++)
{
    var hash = CalculateHash(text);
    list.Add(hash);

    if (list.Count == count - 1)
    {
        var gcCount = 
            //GC.CollectionCount(0) +
                      GC.CollectionCount(1) 
                      //+ GC.CollectionCount(2)
            ;
        return;
    }
}

Console.WriteLine("Hello, World!");

string CalculateHash(string input)
{
    var byteCount = Encoding.UTF8.GetByteCount(input);
    var buffer = ArrayPool<byte>.Shared.Rent(byteCount);

    var hashBuffer = ArrayPool<byte>.Shared.Rent(MD5.HashSizeInBytes);

    try
    {
        Encoding.UTF8.GetBytes(input, buffer);
        if (hashAlgorithm.TryComputeHash(buffer.AsSpan(0, byteCount), hashBuffer.AsSpan(0, MD5.HashSizeInBytes), out var bytesWritten))
        {
            var hash = string.Join("", hashBuffer.Take(bytesWritten).Select(b => b.ToString("X2")));
            return hash;
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
        ArrayPool<byte>.Shared.Return(hashBuffer);
    }

    return string.Empty;
}