using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0130
namespace EncodingUtf8AndGBKDifferentiater
{
    /// <summary>
    /// 区分文件编码
    /// </summary>
    /// Copy from: https://github.com/dotnet-campus/EncodingNormalior/blob/1c7cee71b1626e340a40f783882148aa2ac29958/EncodingUtf8AndGBKDifferentiater/EncodingDifferentiater.cs
    public class EncodingDifferentiater : IDisposable
    {
        public EncodingDifferentiater(Stream stream)
        {
            _stream = stream;
            if (!stream.CanSeek)
            {
                throw new ArgumentException();
            }
        }

        private byte[]? CountBuffer
        {
            set; get;
        }

        private readonly Stream _stream;

        public async ValueTask<InspectFileEncodingResult> InspectFileEncodingAsync()
        {
            double confidenceCount = 1;
            var stream = _stream;

            const int headAmount = 4;
            if (stream.Length < headAmount)
            {
                // 太短了，无法识别
                return new InspectFileEncodingResult(Encoding.ASCII, 0);
            }

            var headByte = ReadFileHeadByte(stream, headAmount);
            stream.Position = 0;

            //从文件获取编码
            var encoding = AutoEncoding(headByte);

            //Encoding.UTF8
            // uft8无签名
            if (encoding.Equals(Encoding.ASCII)) //GBK utf8
            {
                //如果都是ASCII，那么无法知道编码
                //如果属于 Utf8的byte数大于 GBK byte数，那么编码是 utf8，否则是GBK
                //如果两个数相同，那么不知道是哪个

                var countUtf8 = await CountUtf8Async();
                if (countUtf8 == 0)
                {
                    encoding = Encoding.ASCII;
                }
                else
                {
                    var countGbk = await CountGbkAsync();
                    if (countUtf8 > countGbk)
                    {
                        encoding = Encoding.UTF8;
                        confidenceCount = (double) countUtf8 / (countUtf8 + countGbk);
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding("GBK");
                        confidenceCount = (double) countGbk / (countUtf8 + countGbk);
                    }
                }
            }
            else
            {
                //EncodingScrutatorFile.Encoding = encoding;//不需要
                confidenceCount = 1;
            }

            return new(encoding, confidenceCount);
        }

        /// <summary>
        ///     统计文件属于 GBK 的 byte数
        /// </summary>
        /// <returns></returns>
        private async ValueTask<int> CountGbkAsync()
        {
            var count = 0; //存在GBK的byte

            CountBuffer ??= ArrayPool<byte>.Shared.Rent(1024);

            var stream = _stream;
            stream.Position = 0;

            int readCount;
            while ((readCount = await stream.ReadAsync(CountBuffer, 0, CountBuffer.Length)) > 0)
            {
                var length = readCount;
                var buffer = CountBuffer;

                const char head = (char) 0x80; //小于127 通过 &head==0

                for (var i = 0; i < length; i++)
                {
                    var firstByte = buffer[i]; //第一个byte，GBK有两个
                    if ((firstByte & head) == 0) //如果是127以下，那么就是英文等字符，不确定是不是GBK
                    {
                        continue; //文件全部都是127以下字符，可能是Utf-8 或ASCII
                    }
                    if (i + 1 >= length) //如果是大于127，需要两字符，如果只有一个，那么文件错了，但是我也没法做什么
                    {
                        break;
                    }
                    var secondByte = buffer[i + 1]; //如果是GBK，那么添加GBK byte 2
                    if (firstByte >= 161 && firstByte <= 247 &&
                        secondByte >= 161 && secondByte <= 254)
                    {
                        count += 2;
                        i++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        ///     属于 UTF8 的 byte 数
        /// </summary>
        /// <returns></returns>
        private async ValueTask<int> CountUtf8Async()
        {
            var count = 0;

            CountBuffer ??= ArrayPool<byte>.Shared.Rent(1024);

            var stream = _stream;
            stream.Position = 0;

            int readCount;
            const char head = (char) 0x80;
            while ((readCount = await stream.ReadAsync(CountBuffer, 0, CountBuffer.Length)) > 0)
            {
                var length = readCount;
                var buffer = CountBuffer;
                for (var i = 0; i < length; i++)
                {
                    var temp = buffer[i];
                    if (temp < 128) //  !(temp&head)
                    {
                        //utf8 一开始如果byte大小在 0-127 表示英文等，使用一byte
                        //length++; 我们记录的是和CountGBK比较
                        continue;
                    }
                    var tempHead = head;
                    var wordLength = 0; //单词长度，一个字使用多少个byte

                    while ((temp & tempHead) != 0) //存在多少个byte
                    {
                        wordLength++;
                        tempHead >>= 1;
                    }

                    if (wordLength <= 1)
                    {
                        //utf8最小长度为2
                        continue;
                    }

                    wordLength--; //去掉最后一个，可以让后面的 point大于wordLength
                    if (wordLength + i >= length)
                    {
                        break;
                    }
                    var point = 1; //utf8的这个word 是多少 byte
                    //utf8在两字节和三字节的编码，除了最后一个 byte 
                    //其他byte 大于127 
                    //所以 除了最后一个byte，其他的byte &head >0
                    for (; point <= wordLength; point++)
                    {
                        var secondChar = buffer[i + point];
                        if ((secondChar & head) == 0)
                        {
                            break;
                        }
                    }

                    if (point > wordLength)
                    {
                        count += wordLength + 1;
                        i += wordLength;
                    }
                }
            }

            return count;
        }

        //[MemberNotNull(nameof(CountBuffer))]
        //private async ValueTask ReadStreamAsync()
        //{
        //    var stream = _stream;
        //    stream.Position = 0;
        //    var length = (int) stream.Length;

        //    // 先跳过 Ascii 方面

        //    // 不用全读取，读取一些就可以了
        //    length = Math.Max(length, 1024);
        //    CountBuffer = new byte[length];
        //    await stream.ReadExactlyAsync(CountBuffer, 0, length);
        //}

        /// <summary>
        ///     读取文件的头4个byte
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="headAmount">读取长度</param>
        /// <returns>文件头4个byte</returns>
        private byte[] ReadFileHeadByte(Stream stream, int headAmount = 4)
        {
            //var headAmount = 4;
            var buffer = new byte[headAmount];
            int n = stream.Read(buffer, 0, headAmount);
            if (n < headAmount)
            {
                throw new ArgumentException("读取到的文件长度太小，实际读取长度" + n + "，需要的长度" + headAmount);
            }
            stream.Position = 0;
            return buffer;
        }


        private static Encoding AutoEncoding(byte[] bom)
        {
            if (bom.Length != 4)
            {
                throw new ArgumentException("EncodingScrutator.AutoEncoding 参数大小不等于4");
            }

            // Analyze the BOM

            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
#pragma warning disable SYSLIB0001
                return Encoding.UTF7; //85 116 102 55    //utf7 aa 97 97 0 0
            //utf7 编码 = 43 102 120 90

            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                return Encoding.UTF8; //无签名 117 116 102 56
            // 130 151 160 231
            if (bom[0] == 0xff && bom[1] == 0xfe)
                return Encoding.Unicode; //UTF-16LE

            if (bom[0] == 0xfe && bom[1] == 0xff)
                return Encoding.BigEndianUnicode; //UTF-16BE

            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
                return Encoding.UTF32;

            return Encoding.ASCII; //如果返回ASCII可能是GBK 无签名utf8
        }

        public void Dispose()
        {
            if (CountBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(CountBuffer);
            }
        }
    }

    /// <summary>
    /// 判断一个文件的编码的结果
    /// </summary>
    /// <param name="Encoding">文件的编码</param>
    /// <param name="ConfidenceCount">文件的编码可信度，注意 ASCII 文件的可信度为 0 在可信度为 1 的时候就是确定</param>
    public readonly record struct InspectFileEncodingResult(Encoding Encoding, double ConfidenceCount);
}
