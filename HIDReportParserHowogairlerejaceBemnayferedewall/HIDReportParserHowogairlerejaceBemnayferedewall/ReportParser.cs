using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

using static HIDReportParserHowogairlerejaceBemnayferedewall.ReportUsage;

namespace HIDReportParserHowogairlerejaceBemnayferedewall;

class ReportParser
{
    public IList<byte> ParseHexByteText(string hexByteText)
    {
        List<byte> byteList = new List<byte>();
        for (var i = 0; i < hexByteText.Length; i++)
        {
            if (IsHexCharIndex(i))
            {
                if (i + 1 < hexByteText.Length && IsHexCharIndex(i + 1))
                {
                    byteList.Add(byte.Parse(hexByteText.AsSpan(i, 2), NumberStyles.HexNumber));
                    i++;
                }
                else
                {
                    byteList.Add(byte.Parse(hexByteText.AsSpan(i, 1), NumberStyles.HexNumber));
                }
            }

            bool IsHexCharIndex(int index) => IsHexChar(hexByteText[index]);
            bool IsHexChar(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
        }

        return byteList;
    }

    public string Parse(IList<byte> report)
    {
        // 用于控制缩进
        int space = 0;

        for (var index = 0; index < report.Count; index++)
        {

            var itemTag = report[index] & TagMask;
            var itemSize = ToItemSize(report[index] & SizeMask);

            if (index + itemSize >= report.Count)
            {
                throw new Exception("Report is too short");
            }

            var itemData = GetItemDataLength(report, index + 1, itemSize);
            switch (itemTag & TypeMask)
            {
                case MainItem:
                    ToMainItem(itemTag, itemData, ref space);
                    break;
                case GlobalItem:
                    //ToGlobalItem(itemTag, itemData, space, ref space);
                    break;
                case LocalItem:
                    //ToLocalItem(itemTag, itemData, space, ref space);
                    break;
                default:
                    break;
            }
            index += (itemSize + 1);
        }

        return null;
    }

    void ToMainItem(int itemTag, int itemData, ref int space)
    {
        var stringBuilder = new StringBuilder();

        for (int i = 0; i < space; i++)
        {
            stringBuilder.Append("  ");
        }

        if (itemTag == Input(0))
        {
            stringBuilder.Append($"  Input ({ConvertDataType(Input(0), itemData)})");
        }
        else if (itemTag == Output(0))
        {
            stringBuilder.Append($"  Output ({ConvertDataType(Output(0), itemData)})");
        }
        else if (itemTag == Feature(0))
        {
            stringBuilder.Append($"  Feature ({ConvertDataType(Feature(0), itemData)})");
        }
        else if (itemTag == Collection(0))
        {
            //space += 2;
            //stringBuilder.Append($"  Collection ({ConvertColletionType(itemData)})");
            ////if (itemData & 0xFFFFFF00U)
            //stringBuilder.Append($"  ???");

        }
        else if (itemTag == EndCollection(0))
        {
            space -= 2;
            stringBuilder.Append($"  End Collection ({ConvertDataType(EndCollection(0), itemData)})");
        }
        else
        {
            stringBuilder.Append($"Unknown Item: {itemTag:X2}X");
        }

        Console.WriteLine(stringBuilder.ToString());
    }

    string ConvertDataType(int itemTag, int itemData)
    {
        var str = new StringBuilder(128);

        str.Append((itemData & Constant) == 0 ? "Cnst" : "Data");
        str.Append((itemData & Variable) == 0 ? ", Var" : ", Array");
        str.Append((itemData & Relative) == 0 ? ", Rel" : ", Abs");
        if ((itemData & Wrap) == 1)
        {
            str.Append(", Wrap");
        }

        if ((itemData & NonLinear) == 1)
        {
            str.Append(", NonLinear");
        }

        if ((itemData & NoPreferred) == 1)
        {
            str.Append(", No Preferred");
        }

        if ((itemData & NullState) == 1)
        {
            str.Append(", Null State");
        }

        if ((itemTag & TagMask) != Input(0) && (itemData & Volatile) == 1)
        {
            str.Append(", Volatile");
        }

        if ((itemData & 0xFFFFFF00) == 1)
        {
            str.Append(", ???");
        }

        return str.ToString();
    }

    private int GetItemDataLength(IList<byte> report, int startIndex, int itemSize)
    {
        if (itemSize == 1)
        {
            return report[startIndex];
        }
        else if (itemSize == 2)
        {
            Span<byte> buffer = stackalloc byte[2] { report[startIndex], report[startIndex + 1] };
            return BitConverter.ToInt16(buffer);
        }
        else if (itemSize == 4)
        {
            Span<byte> buffer = stackalloc byte[4] { report[startIndex], report[startIndex + 1], report[startIndex + 2], report[startIndex + 3] };
            return BitConverter.ToInt32(buffer);
        }
        else
        {
            return 0;
        }
    }


    private static int ToItemSize(int sizeMask) => sizeMask == Size_4B ? 4 : sizeMask;

    private const int TagOffset = 4;
    private const int TypeOffset = 2;
    private const int SizeOffset = 0;

    private const int TypeMask = 0x03 << TypeOffset;
    private const int TagMask = (0x0F << TagOffset) | TypeMask;
    private const int SizeMask = 0x03 << SizeOffset;

    private const int Size_0B = 0;
    private const int Size_1B = 1;
    private const int Size_2B = 2;
    private const int Size_4B = 3;

    private const int MainItem = 0x00; // (0x00 << TypeOffset);
    private const int GlobalItem = (0x01 << TypeOffset);
    private const int LocalItem = (0x02 << TypeOffset);

    private int Input(int size) => (MainItem | (0x08 << TagOffset) | (size & SizeMask));
    private int Output(int size) => (MainItem | (0x09 << TagOffset) | (size & SizeMask));
    private int Feature(int size) => (MainItem | (0x0B << TagOffset) | (size & SizeMask));
    private int Collection(int size) => (MainItem | (0x0A << TagOffset) | (size & SizeMask));
    private int EndCollection(int size) => (MainItem | (0x0C << TagOffset) | (size & SizeMask));

    private const int Data = (0);
    private const int Constant = (1);
    private const int Array = (0);
    private const int Variable = (1 << 1);
    private const int Absolute = (0);
    private const int Relative = (1 << 2);
    private const int NoWrap = (0);
    private const int Wrap = (1 << 3);
    private const int Linear = (0);
    private const int NonLinear = (1 << 4);
    private const int PreferredState = (0);
    private const int NoPreferred = (1 << 5);
    private const int NoNullPosition = (0);
    private const int NullState = (1 << 6);
    private const int Nonvolatile = (0);
    private const int Volatile = (1 << 7);
}