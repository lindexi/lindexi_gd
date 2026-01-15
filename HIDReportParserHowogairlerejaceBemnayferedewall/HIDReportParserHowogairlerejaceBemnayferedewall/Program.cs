using System.Text;

namespace HIDReportParserHowogairlerejaceBemnayferedewall;

internal class Program
{
    static void Main(string[] args)
    {
        Parse(@"  06 AB FF 0A 00 20 A1 01 85 01 09 00 15 00 25 FF
  35 00 45 00 65 00 55 00 75 08 95 08 B2 02 01 25
  01 45 01 75 01 96 A0 01 B1 03 85 02 09 00 25 FF
  45 00 75 08 95 3C B2 02 01 C1 00");

        Console.WriteLine();

        Parse(@" 06 AB FF 0A 00 02 A1 01 09 01 15 00 25 FF 35 00
  45 00 65 00 55 00 75 08 95 40 81 02 09 02 91 02
  C1 00");

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