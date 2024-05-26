using System.Text;

namespace HIDReportParserHowogairlerejaceBemnayferedewall;

internal class Program
{
    static void Main(string[] args)
    {
        var t = @"06 C9 FF 09 04 A1 5C 09 75 15 00 25 FF 35 00 45
  00 65 00 55 00 75 08 95 40 81 02 09 76 95 20 91
  02 09 76 95 04 B1 02 C1 00";

        var parser = new ReportParser();

        var bytes = parser.ParseHexByteText(t);
        var report = parser.Parse(bytes);
        Console.WriteLine(report);
    }
}