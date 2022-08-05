using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using TtfReader;

var file = @"C:\windows\fonts\simhei.ttf";

using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

var ttfInfo = TtfInfo.Read(fileStream);

Console.WriteLine($"读取 {file} 字体名");

foreach (var nameRecord in ttfInfo.NameTable.NameRecords)
{
    if (nameRecord.NameId == NameIdentifier.FontFamily)
    {
        Console.WriteLine(nameRecord.Value);
    }
}

