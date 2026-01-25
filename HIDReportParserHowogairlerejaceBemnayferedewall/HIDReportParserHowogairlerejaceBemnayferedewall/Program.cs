using System.Text;

namespace HIDReportParserHowogairlerejaceBemnayferedewall;

internal class Program
{
    static void Main(string[] args)
    {
        Parse(@"06 AB FF 
0A 00 02  
A1 01        
75 08        
15 00        
26 FF 00  
95 40        
09 01        
81 02       
95 40        
09 02        
91 02        
C0              ");

        // 预期以上输出是：
/*
   0x06, 0xAB, 0xFF,  // Usage Page (Vendor Defined 0xFFAB)
   0x0A, 0x00, 0x02,  // Usage (0x0200)
   0xA1, 0x01,        // Collection (Application)
   0x75, 0x08,        //   Report Size (8)
   0x15, 0x00,        //   Logical Minimum (0)
   0x26, 0xFF, 0x00,  //   Logical Maximum (255)
   0x95, 0x40,        //   Report Count (64)
   0x09, 0x01,        //   Usage (0x01)
   0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
   0x95, 0x40,        //   Report Count (64)
   0x09, 0x02,        //   Usage (0x02)
   0x91, 0x02,        //   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
   0xC0,              // End Collection
 */
        Console.WriteLine();

        var keyboardReport = @"
          05 01
          09 06
          A1 01
          85 01
          05 07
          19 E0
          29 E7
          15 00
          25 01
          95 08
          75 01
          81 02
          95 01
          75 08
          81 03
          05 07
          19 00
          29 68
          15 00
          25 68
          95 06
          75 08
          81 00
          05 08
          19 01
          29 05
          95 05
          75 01
          91 02
          95 01
          75 03
          91 01
          C0
        ";

        var parser = new ReportParser();

        var bytes = parser.ParseHexByteText(keyboardReport);
        var report = parser.Parse(bytes);
        Console.WriteLine(report);

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // Test with the custom report
        var customReport = @"06 C9 FF 09 04 A1 5C 09 75 15 00 25 FF 35 00 45
  00 65 00 55 00 75 08 95 40 81 02 09 76 95 20 91
  02 09 76 95 04 B1 02 C1 00";

        bytes = parser.ParseHexByteText(customReport);
        report = parser.Parse(bytes);
        Console.WriteLine(report);
    }

    private static void Parse(string byteText)
    {
        var parser = new ReportParser();
        var bytes = parser.ParseHexByteText(byteText);
        var report = parser.Parse(bytes);
        Console.WriteLine(report);
    }
}