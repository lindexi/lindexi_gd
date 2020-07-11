using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace FileDownloader.Tests
{
    [TestClass]
    public class RandomFileWriterTest
    {
        [ContractTestCase]
        public void WriteFile()
        {
            "随机分段写入文件，可以读取到按照顺序的文件".Test(async () =>
            {
                var file = new FileInfo("File.txt");

                if (file.Exists)
                {
                    file.Delete();
                }

                var str = new StringBuilder();

                await using (var fileStream = file.Create())
                await using (var randomFileWriter = new RandomFileWriter(fileStream))
                {
                    const int count = 'z' - 'a' + 1;
                    fileStream.SetLength(count * 100);

                    var list = new List<(int startPoint, byte[] data)>();
                    for (int i = 0; i < 100; i++)
                    {
                        var data = new byte[count];
                        for (int j = 0; j < count; j++)
                        {
                            data[j] = (byte)('a' + j);
                        }

                        list.Add((count * i, data));
                    }

                    foreach (var (startPoint, data) in list)
                    {
                        str.Append(string.Join("", data.Select(temp => (char)temp)));
                    }

                    // 打乱顺序
                    var random = new Random();

                    for (int i = 0; i < 100; i++)
                    {
                        var a = random.Next(0, list.Count);
                        var b = random.Next(0, list.Count);

                        var t = list[a];
                        list[a] = list[b];
                        list[b] = t;
                    }

                    foreach (var (startPoint, data) in list)
                    {
                        randomFileWriter.WriteAsync(startPoint, data, count);
                    }
                }

                Thread.Sleep(1000);

                var text = await File.ReadAllTextAsync(file.FullName);

                Assert.AreEqual(str.ToString(), text);
            });


            "连续的文件写入，可以写入连续的文件".Test(async () =>
            {
                var file = new FileInfo("File.txt");

                if (file.Exists)
                {
                    file.Delete();
                }

                var str = new StringBuilder();

                await using (var fileStream = file.Create())
                await using (var randomFileWriter = new RandomFileWriter(fileStream))
                {
                    const int count = 'z' - 'a' + 1;
                    fileStream.SetLength(count * 100);

                    var list = new List<(int startPoint, byte[] data)>();
                    for (int i = 0; i < 100; i++)
                    {
                        var data = new byte[count];
                        for (int j = 0; j < count; j++)
                        {
                            data[j] = (byte)('a' + j);
                        }

                        list.Add((count * i, data));
                    }

                    foreach (var (startPoint, data) in list)
                    {
                        randomFileWriter.WriteAsync(startPoint, data, count);
                        str.Append(string.Join("", data.Select(temp => (char)temp)));
                    }
                }

                Thread.Sleep(1000);

                var text = await File.ReadAllTextAsync(file.FullName);

                Assert.AreEqual(str.ToString(), text);
            });
        }
    }
}