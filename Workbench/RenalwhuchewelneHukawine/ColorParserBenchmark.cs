using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace RenalwhuchewelneHukawine;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput)]
[MemoryDiagnoser]
public class ColorParserBenchmark
{
    [Benchmark]
    [ArgumentsSource(nameof(GetArgument))]
    public uint Test(string colorText)
    {
        var (success, a, r, g, b) = ConvertToColor(colorText);

        if (success)
        {
           return (uint)(r | (g << 8) | (b << 16) | (a << 24)); // RGBA layout
        }
        else
        {
            return 0;
        }
    }

    public IEnumerable<string> GetArgument()
    {
        yield return "#FFAABBCC";
        yield return "#FFAABBCC1";
        yield return "#AABBCC1";
        yield return "#AABBCCDD";
        yield return "#ABC";
        yield return "#AABBCC";
        yield return "#123456";
        yield return "#FF123456";
    }

    static (bool success, byte a, byte r, byte g, byte b) ConvertToColor(string input) //ARGB format
    {
        bool startWithPoundSign = input.StartsWith('#');
        var colorStringLength = input.Length;
        if (startWithPoundSign) colorStringLength -= 1;
        int currentOffset = startWithPoundSign ? 1 : 0;
        // Formats:
        // #FFDFD991   8 chars with Alpha
        // #DFD991     6 chars
        // #FD92       4 chars with Alpha
        // #DAC        3 chars
        if (colorStringLength == 8
            || colorStringLength == 6
            || colorStringLength == 4
            || colorStringLength == 3)
        {
            bool success;
            byte result;
            byte a;

            int readCount;
            // #DFD991     6 chars
            // #FFDFD991   8 chars with Alpha
            //if (colorStringLength == 8 || colorStringLength == 6)
            if (colorStringLength > 5)
            {
                readCount = 2;
            }
            else
            {
                readCount = 1;
            }

            bool includeAlphaChannel = colorStringLength == 8 || colorStringLength == 4;

            if (includeAlphaChannel)
            {
                (success, result) = HexCharToNumber(input, currentOffset, readCount);
                if (!success) return default;
                a = result;
                currentOffset += readCount;
            }
            else
            {
                a = 0xFF;
            }

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte r = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte g = result;
            currentOffset += readCount;

            (success, result) = HexCharToNumber(input, currentOffset, readCount);
            if (!success) return default;
            byte b = result;

            return (true, a, r, g, b);
        }

        return default;
    }

    static (bool success, byte result) HexCharToNumber(string input, int offset, int readCount)
    {
        Debug.Assert(readCount == 1 || readCount == 2);

        byte result = 0;

        for (int i = 0; i < readCount; i++, offset++)
        {
            var c = input[offset];
            byte n;
            if (c >= '0' && c <= '9')
            {
                n = (byte) (c - '0');
            }
            else if (c >= 'a' && c <= 'f')
            {
                n = (byte) (c - 'a' + 10);
            }
            else if (c >= 'A' && c <= 'F')
            {
                n = (byte) (c - 'A' + 10);
            }
            else
            {
                return default;
            }

            result *= 16;
            result += n;
        }

        if (readCount == 1)
        {
            result = (byte) (result * 16 + result);
        }

        return (true, result);
    }
}
