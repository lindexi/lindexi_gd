using System.Security.Cryptography;
using System.Text;

namespace BairrercebiCeenocaneaharhar;

internal class Program
{
    static void Main(string[] args)
    {
        var text = "Hello, World!";

        var memoryStream = new MemoryStream();
        var buffer = Encoding.UTF8.GetBytes(text);
        memoryStream.Write(buffer, 0, buffer.Length);
        var binaryWriter = new BinaryWriter(memoryStream);

        using var md5 = MD5.Create();

        var count = 3;
        var currentLength = memoryStream.Length;
        var n = 0;

        while (true)
        {
            memoryStream.Position = currentLength;
            binaryWriter.Write(n);
            n++;

            memoryStream.Position = 0;

            var hash = md5.ComputeHash(memoryStream);

            bool end = true;
            for (int i = 0; i < count; i++)
            {
                if (hash[i] != 0)
                {
                    end = false;
                    break;
                }
            }

            if (end)
            {
                break;
            }
        }

        Console.WriteLine();
    }
}
