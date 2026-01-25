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
    private int _currentUsagePage = -1;
    private readonly StringBuilder _output = new();

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
            bool IsHexChar(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        return byteList;
    }

    public string Parse(IList<byte> report)
    {
        _output.Clear();
        _currentUsagePage = -1;
        
        _output.AppendLine("Report Item Parse:");
        
        // 用于控制缩进
        int space = 0;

        for (var index = 0; index < report.Count; )
        {
            var itemTag = report[index] & TagMask;
            var itemSize = ToItemSize(report[index] & SizeMask);

            if (index + itemSize >= report.Count)
            {
                _output.AppendLine("out of buffer.");
                break;
            }

            var itemData = GetItemDataLength(report, index + 1, itemSize);
            switch (itemTag & TypeMask)
            {
                case MainItem:
                    ToMainItem(itemTag, itemData, ref space);
                    break;
                case GlobalItem:
                    ToGlobalItem(itemTag, itemData, space);
                    break;
                case LocalItem:
                    ToLocalItem(itemTag, itemData, space);
                    break;
                default:
                    _output.AppendLine($"Unknown Type: {itemTag:X2}, index: {index}");
                    break;
            }
            index += (itemSize + 1);
        }

        return _output.ToString();
    }

    void ToMainItem(int itemTag, int itemData, ref int space)
    {
        var str = new StringBuilder();

        for (int i = 0; i < space; i++)
        {
            str.Append(' ');
        }

        if ((itemTag & TagMask) == Input(0))
        {
            str.Append($"  Input ({ConvertDataType(Input(0), itemData)})");
        }
        else if ((itemTag & TagMask) == Output(0))
        {
            str.Append($"  Output ({ConvertDataType(Output(0), itemData)})");
        }
        else if ((itemTag & TagMask) == Feature(0))
        {
            str.Append($"  Feature ({ConvertDataType(Feature(0), itemData)})");
        }
        else if ((itemTag & TagMask) == Collection(0))
        {
            str.Append($"Collection ({ConvertCollectionType((byte)itemData)})");
            if ((itemData & 0xFFFFFF00) != 0)
            {
                str.Append(" ???");
            }
            space += 2;
        }
        else if ((itemTag & TagMask) == EndCollection(0))
        {
            space -= 2;
            // 减少缩进后重新格式化
            str.Clear();
            for (int i = 0; i < space; i++)
            {
                str.Append(' ');
            }
            str.Append("End Collection");
        }
        else
        {
            str.Append($"Unknown Item: {itemTag:X2}");
        }

        _output.AppendLine(str.ToString());
    }

    string ConvertDataType(int itemTag, int itemData)
    {
        var str = new StringBuilder(128);

        str.Append((itemData & Constant) != 0 ? "Cnst" : "Data");
        str.Append((itemData & Variable) != 0 ? ", Var" : ", Array");
        str.Append((itemData & Relative) != 0 ? ", Rel" : ", Abs");
        
        // 总是显示 Wrap 状态
        str.Append((itemData & Wrap) != 0 ? ", Wrap" : ", No Wrap");

        // 总是显示 Linear 状态
        str.Append((itemData & NonLinear) != 0 ? ", NonLinear" : ", Linear");

        // 总是显示 Preferred 状态
        str.Append((itemData & NoPreferred) != 0 ? ", No Preferred" : ", Preferred State");

        // 总是显示 Null State 状态
        str.Append((itemData & NullState) != 0 ? ", Null State" : ", No Null Position");

        // Input Items Data bit 7 is undefined and is RFU.
        if ((itemTag & TagMask) != Input(0))
        {
            str.Append((itemData & Volatile) != 0 ? ", Volatile" : ", Non-volatile");
        }

        // Data byte 1~3 is RFU.
        if ((itemData & 0xFFFFFF00) != 0)
        {
            str.Append(", ???");
        }

        return str.ToString();
    }

    string ConvertCollectionType(byte itemData)
    {
        return itemData switch
        {
            ColPhysical => "Physical",
            ColApplication => "App",
            ColLogical => "Logical",
            ColReport => "Report",
            ColNamedArray => "Named Array",
            ColUsageSwitch => "Usage Switch",
            ColUsageModifier => "Usage Modifier",
            _ => "Unknown"
        };
    }

    void ToGlobalItem(int itemTag, int itemData, int space)
    {
        var str = new StringBuilder(256);

        for (int i = 0; i < space; i++)
        {
            str.Append(' ');
        }

        if ((itemTag & TagMask) == UsagePage(0))
        {
            // 将有符号值转换为无符号存储（处理 0xFFAB 这样的 Vendor Defined 值）
            _currentUsagePage = (int)(ushort)itemData;
            str.Append($"Usage Page ({GetUsagePageName(itemData)})");
        }
        else if ((itemTag & TagMask) == LogicalMinimum(0))
        {
            str.Append($"Logical Min ({itemData})");
        }
        else if ((itemTag & TagMask) == LogicalMaximum(0))
        {
            str.Append($"Logical Max ({itemData})");
        }
        else if ((itemTag & TagMask) == PhysicalMinimum(0))
        {
            str.Append($"Physical Min ({itemData})");
        }
        else if ((itemTag & TagMask) == PhysicalMaximum(0))
        {
            str.Append($"Physical Max ({itemData})");
        }
        else if ((itemTag & TagMask) == UnitExponent(0))
        {
            str.Append($"Unit Exponent ({GetExponent(itemData)})");
        }
        else if ((itemTag & TagMask) == Unit(0))
        {
            str.Append($"Unit ({GetUnit((uint)itemData)})");
        }
        else if ((itemTag & TagMask) == ReportSize(0))
        {
            str.Append($"Report Size ({itemData})");
        }
        else if ((itemTag & TagMask) == ReportID(0))
        {
            str.Append($"Report ID ({itemData})");
        }
        else if ((itemTag & TagMask) == ReportCount(0))
        {
            str.Append($"Report Count ({itemData})");
        }
        else if ((itemTag & TagMask) == Push(0))
        {
            str.Append("Push");
        }
        else if ((itemTag & TagMask) == Pop(0))
        {
            str.Append("Pop");
        }
        else
        {
            str.Append($"Unknown Item: {itemTag:X2}");
        }

        _output.AppendLine(str.ToString());
    }

    void ToLocalItem(int itemTag, int itemData, int space)
    {
        var str = new StringBuilder(256);

        for (int i = 0; i < space; i++)
        {
            str.Append(' ');
        }

        if ((itemTag & TagMask) == Usage(0))
        {
            var usageStr = ToUsage((uint)_currentUsagePage, (uint)itemData);
            // 对于 Vendor Defined Usage Page 或者返回 Unknown 的情况，显示十六进制值
            if (usageStr == "Unknown" || (uint)_currentUsagePage >= 0xFF00)
            {
                str.Append($"Usage (0x{itemData:X2})");
            }
            else
            {
                str.Append($"Usage ({usageStr})");
            }
        }
        else if ((itemTag & TagMask) == UsageMinimum(0))
        {
            str.Append($"Usage Min ({itemData})");
        }
        else if ((itemTag & TagMask) == UsageMaximum(0))
        {
            str.Append($"Usage Max ({itemData})");
        }
        else if ((itemTag & TagMask) == DesignatorIndex(0))
        {
            str.Append($"Designator Index ({itemData})");
        }
        else if ((itemTag & TagMask) == DesignatorMinimum(0))
        {
            str.Append($"Designator Min ({itemData})");
        }
        else if ((itemTag & TagMask) == DesignatorMaximum(0))
        {
            str.Append($"Designator Max ({itemData})");
        }
        else if ((itemTag & TagMask) == StringIndex(0))
        {
            str.Append($"String Index ({itemData})");
        }
        else if ((itemTag & TagMask) == StringMinimum(0))
        {
            str.Append($"String Min ({itemData})");
        }
        else if ((itemTag & TagMask) == StringMaximum(0))
        {
            str.Append($"String Max ({itemData})");
        }
        else if ((itemTag & TagMask) == Delimiter(0))
        {
            var delimiterStr = itemData == 1 ? "Open Set" : (itemData == 0 ? "Close Set" : "Unknown Setting");
            str.Append($"Delimiter ({delimiterStr})");
        }
        else
        {
            str.Append($"Unknown Item: {itemTag:X2}");
        }

        _output.AppendLine(str.ToString());
    }

    string GetUsagePageName(int usagePage)
    {
        // 将有符号的 int 转换为无符号处理（处理 0xFFAB 这样的值）
        var pageValue = (uint)(ushort)usagePage;
        
        var result = pageValue switch
        {
            UP_Generic_Desktop => "Generic Desktop",
            UP_Simulation_Controls => "Simulation",
            UP_VR_Controls => "VR Controls",
            UP_Sport_Controls => "Sport Controls",
            UP_Game_Controls => "Game Controls",
            UP_Generic_Device_Controls => "Generic Device",
            UP_Keyboard_or_Keypad => "Keyboard",
            UP_LEDs => "LEDs",
            UP_Button => "Buttons",
            UP_Ordinal => "Ordinal",
            UP_Telephony => "Telephony",
            UP_Consumer => "Consumer",
            UP_Digitizer => "Digitizer",
            UP_PID_Page => "PID Page",
            UP_Unicode => "Unicode",
            UP_Alphanumeric_Display => "Alphanumeric Display",
            UP_Medical_Instruments => "Medical Instruments",
            _ => null
        };

        if (result != null)
            return result;

        // Vendor Defined (0xFF00-0xFFFF)
        if (pageValue >= 0xFF00)
            return $"Vendor Defined 0x{pageValue:X4}";

        return "Unknown";
    }

    string GetExponent(int itemData)
    {
        byte code = (byte)itemData;
        if (code >= 0x05 && code <= 0x0F)
        {
            string[] str = { "5", "6", "7", "-8", "-7", "-6", "-5", "-4", "-3", "-2", "-1" };
            return str[code - 5];
        }
        return "Unknown";
    }

    sbyte NibbleToByte(sbyte nibble) => (sbyte)((nibble & 0x08) != 0 ? (nibble | 0xF0) : nibble);

    string GetUnit(uint itemData)
    {
        string[][] strUnits = [
            [], // 占位符，索引 0 不使用
            ["SI Linear", "cm", "Gram", "Seconds", "Kelvin", "Ampere", "Candela"],
            ["SI Rotation", "rad", "Gram", "Seconds", "Kelvin", "Ampere", "Candela"],
            ["English Linear", "Inch", "Slug", "Seconds", "Fahrenheit", "Ampere", "Candela"],
            ["English Rotation", "Degrees", "Slug", "Seconds", "Fahrenheit", "Ampere", "Candela"]
        ];

        sbyte nibble = (sbyte)(itemData & 0xF);
        
        if (nibble == 0)
            return "None";
        
        if (nibble < 1 || nibble > 4)
            return "Unknown";

        var str = new StringBuilder();
        var selectedUnit = strUnits[nibble]; // 保存选中的单位系统
        str.Append(selectedUnit[0]);
        str.Append(':');

        itemData >>= 4;
        int nibbleNo = 1;

        while (itemData != 0 && nibbleNo < 7)
        {
            nibble = (sbyte)(itemData & 0xF);
            if (nibble != 0)
            {
                str.Append($" {selectedUnit[nibbleNo]}[{NibbleToByte(nibble)}]");
            }
            itemData >>= 4;
            nibbleNo++;
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

    // Input, Output and Feature Items Data bit constants
    private const int Data = 0;
    private const int Constant = 1;
    private const int Array = 0;
    private const int Variable = (1 << 1);
    private const int Absolute = 0;
    private const int Relative = (1 << 2);
    private const int NoWrap = 0;
    private const int Wrap = (1 << 3);
    private const int Linear = 0;
    private const int NonLinear = (1 << 4);
    private const int PreferredState = 0;
    private const int NoPreferred = (1 << 5);
    private const int NoNullPosition = 0;
    private const int NullState = (1 << 6);
    private const int Nonvolatile = 0;
    private const int Volatile = (1 << 7);

    // Collection types
    private const byte ColPhysical = 0x00;
    private const byte ColApplication = 0x01;
    private const byte ColLogical = 0x02;
    private const byte ColReport = 0x03;
    private const byte ColNamedArray = 0x04;
    private const byte ColUsageSwitch = 0x05;
    private const byte ColUsageModifier = 0x06;

    // Global Item Tag methods
    private int UsagePage(int size) => (GlobalItem | (0x00 << TagOffset) | (size & SizeMask));
    private int LogicalMinimum(int size) => (GlobalItem | (0x01 << TagOffset) | (size & SizeMask));
    private int LogicalMaximum(int size) => (GlobalItem | (0x02 << TagOffset) | (size & SizeMask));
    private int PhysicalMinimum(int size) => (GlobalItem | (0x03 << TagOffset) | (size & SizeMask));
    private int PhysicalMaximum(int size) => (GlobalItem | (0x04 << TagOffset) | (size & SizeMask));
    private int UnitExponent(int size) => (GlobalItem | (0x05 << TagOffset) | (size & SizeMask));
    private int Unit(int size) => (GlobalItem | (0x06 << TagOffset) | (size & SizeMask));
    private int ReportSize(int size) => (GlobalItem | (0x07 << TagOffset) | (size & SizeMask));
    private int ReportID(int size) => (GlobalItem | (0x08 << TagOffset) | (size & SizeMask));
    private int ReportCount(int size) => (GlobalItem | (0x09 << TagOffset) | (size & SizeMask));
    private int Push(int size) => (GlobalItem | (0x0A << TagOffset) | (size & SizeMask));
    private int Pop(int size) => (GlobalItem | (0x0B << TagOffset) | (size & SizeMask));

    // Local Item Tag methods
    private int Usage(int size) => (LocalItem | (0x00 << TagOffset) | (size & SizeMask));
    private int UsageMinimum(int size) => (LocalItem | (0x01 << TagOffset) | (size & SizeMask));
    private int UsageMaximum(int size) => (LocalItem | (0x02 << TagOffset) | (size & SizeMask));
    private int DesignatorIndex(int size) => (LocalItem | (0x03 << TagOffset) | (size & SizeMask));
    private int DesignatorMinimum(int size) => (LocalItem | (0x04 << TagOffset) | (size & SizeMask));
    private int DesignatorMaximum(int size) => (LocalItem | (0x05 << TagOffset) | (size & SizeMask));
    private int StringIndex(int size) => (LocalItem | (0x07 << TagOffset) | (size & SizeMask));
    private int StringMinimum(int size) => (LocalItem | (0x08 << TagOffset) | (size & SizeMask));
    private int StringMaximum(int size) => (LocalItem | (0x09 << TagOffset) | (size & SizeMask));
    private int Delimiter(int size) => (LocalItem | (0x0A << TagOffset) | (size & SizeMask));
}