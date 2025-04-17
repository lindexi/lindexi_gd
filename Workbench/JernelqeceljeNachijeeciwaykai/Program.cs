// See https://aka.ms/new-console-template for more information
using System.Text;

// 创建测试的文件
var textFile = Path.Join(AppContext.BaseDirectory, "Text.txt");
WriteTestContent();

void WriteTestContent()
{
    var stringBuilder = new StringBuilder();
    for (int i = 0; i < 10; i++)
    {
        stringBuilder.AppendLine(string.Join("", Enumerable.Repeat(i.ToString(), 200)));
    }

    var testText = stringBuilder.ToString();
    File.WriteAllText(textFile, testText);
}

// 构建测试代码
using var fileStream = File.OpenRead(textFile);
using var streamReader = new StreamReader(fileStream);

// 读取两行
var line1 = streamReader.ReadLine(); // 0000000...
var line2 = streamReader.ReadLine(); // 1111111...

// 进行错误的设置
streamReader.BaseStream.Seek(0, SeekOrigin.Begin);

// 此时继续读取两行，能够继续读取下去，但是读取到某个时刻，将会从头开始读取
var line3 = streamReader.ReadLine(); // 2222222...
var line4 = streamReader.ReadLine(); // 3333333...
var line5 = streamReader.ReadLine(); // 4444444...
var line6 = streamReader.ReadLine(); // 5555555...0000 ?!
var line7 = streamReader.ReadLine(); // 1111111 ???

// 这是因为 StreamReader 带一个内部的缓存，设置 BaseStream 到 0 位置后，StreamReader 还会继续使用自己内部的缓存，直到缓存用完，再从 BaseStream 读取。这就是为什么 line3 还是 2222222...，而 line6 读取到 5555555...0000000... 的原因。在读取 line3 的时候，内部的缓存还有值，于是继续使用内部的缓存，直到读取到 line6 一半时，才读取完了内部的缓存，需要从 BaseStream 读取，由于 BaseStream 被设置到开始位置，于是读取到 0000000... 的内容了
Console.WriteLine("Hello, World!");